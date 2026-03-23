namespace PolyEdgeScout.Application.Services;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;

/// <summary>
/// Application-level scanner orchestration.
/// Fetches markets from the Gamma API, maps to domain entities, and filters for crypto micro markets.
/// </summary>
public sealed class ScannerService : IScannerService
{
    private readonly AppConfig _config;
    private readonly IGammaApiClient _gammaClient;
    private readonly ILogService _log;

    public ScannerService(AppConfig config, IGammaApiClient gammaClient, ILogService log)
    {
        _config = config;
        _gammaClient = gammaClient;
        _log = log;
    }

    public async Task<List<Market>> ScanMarketsAsync(CancellationToken ct = default)
    {
        _log.Info("Starting market scan...");

        var rawMarkets = await _gammaClient.FetchActiveMarketsAsync(ct);
        _log.Info($"Fetched {rawMarkets.Count} markets from Gamma API");

        var markets = rawMarkets
            .Select(MarketMapper.ToDomain)
            .Where(m => MarketClassifier.IsCryptoMicro(m.Question))
            .Where(m => MarketClassifier.MeetsFilterCriteria(m.CreatedAt, m.Volume, _config.MaxVolume))
            .ToList();

        _log.Info($"Filtered to {markets.Count} crypto micro markets");
        return markets;
    }
}
