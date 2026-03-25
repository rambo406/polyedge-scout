namespace PolyEdgeScout.Application.Services;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;

/// <summary>
/// Application-level scanner orchestration.
/// Fetches events from the Gamma API using server-side tag filtering, applies in-memory tag filtering
/// for required tags (e.g., Crypto), flattens events to markets, and enriches zero-price markets
/// with CLOB orderbook data. Keyword filtering is not needed because tags handle market relevance.
/// </summary>
public sealed class ScannerService : IScannerService
{
    private readonly AppConfig _config;
    private readonly IGammaApiClient _gammaClient;
    private readonly IClobClient _clobClient;
    private readonly ILogService _log;

    public ScannerService(AppConfig config, IGammaApiClient gammaClient, IClobClient clobClient, ILogService log)
    {
        _config = config;
        _gammaClient = gammaClient;
        _clobClient = clobClient;
        _log = log;
    }

    public async Task<List<Market>> ScanMarketsAsync(CancellationToken ct = default)
    {
        _log.Info("Starting market scan...");

        var events = await _gammaClient.FetchActiveEventsAsync(ct);
        _log.Info($"Fetched {events.Count} events from Gamma API (tag-based)");

        // In-memory tag filter: keep only events that contain ALL required tags
        var requiredTagIds = _config.InMemoryEventTagIds
            .Select(id => id.ToString())
            .ToHashSet();

        var tagFilteredEvents = events
            .Where(e => requiredTagIds.All(
                tagId => e.Tags.Any(t => t.Id == tagId)))
            .ToList();

        _log.Info($"Tag-filtered to {tagFilteredEvents.Count} events (required tags: {string.Join(", ", requiredTagIds)})");

        // Flatten events → markets, filter expired, apply volume/freshness criteria
        var now = DateTime.UtcNow;
        var markets = tagFilteredEvents
            .SelectMany(e => e.Markets)
            .Select(MarketMapper.ToDomain)
            .Where(m => !m.EndDate.HasValue || m.EndDate.Value > now)
            .Where(m => MarketClassifier.MeetsFilterCriteria(m.CreatedAt, m.Volume, _config.MaxVolume, _config.MinVolume))
            .ToList();

        // Enrich zero-price markets with CLOB orderbook midpoint prices
        var enrichedMarkets = new List<Market>(markets.Count);
        foreach (var market in markets)
        {
            if (ct.IsCancellationRequested)
                break;

            if (market.YesPrice == 0 && !string.IsNullOrEmpty(market.TokenId))
            {
                var midpoint = await _clobClient.GetMidpointPriceAsync(market.TokenId, ct);
                if (midpoint.HasValue && midpoint.Value > 0)
                {
                    _log.Debug($"Enriched '{market.Question}' YES price from CLOB: {midpoint.Value:F4}");
                    enrichedMarkets.Add(new Market
                    {
                        ConditionId = market.ConditionId,
                        QuestionId = market.QuestionId,
                        TokenId = market.TokenId,
                        Question = market.Question,
                        YesPrice = midpoint.Value,
                        NoPrice = market.NoPrice,
                        Volume = market.Volume,
                        CreatedAt = market.CreatedAt,
                        EndDate = market.EndDate,
                        MarketSlug = market.MarketSlug,
                        Active = market.Active,
                        Closed = market.Closed,
                    });
                    continue;
                }
            }

            enrichedMarkets.Add(market);
        }

        _log.Info($"Filtered to {enrichedMarkets.Count} crypto micro markets");
        return enrichedMarkets;
    }
}
