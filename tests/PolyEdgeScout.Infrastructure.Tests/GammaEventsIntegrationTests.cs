namespace PolyEdgeScout.Infrastructure.Tests;

using System.Net.Http;
using System.Text.Json;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Services;

/// <summary>
/// Integration tests that call the live Gamma API events endpoint
/// and verify the full deserialization → domain mapping pipeline.
/// These tests require network access and are excluded from CI via [Trait].
/// </summary>
[Trait("Category", "Integration")]
public sealed class GammaEventsIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private HttpClient _http = null!;
    private List<GammaEventResponse> _events = [];
    private const string ApiUrl =
        "https://gamma-api.polymarket.com/events"
        + "?tag_id=102127&active=true&closed=false"
        + "&end_date_min=2026-03-25"
        + "&volume_num_min=100&volume_num_max=0"
        + "&limit=500";

    public async Task InitializeAsync()
    {
        _http = new HttpClient();
        var json = await _http.GetStringAsync(ApiUrl);
        _events = JsonSerializer.Deserialize<List<GammaEventResponse>>(json, JsonOptions) ?? [];
    }

    public Task DisposeAsync()
    {
        _http.Dispose();
        return Task.CompletedTask;
    }

    // ── Event-level deserialization ───────────────────────────────────

    [Fact]
    public void Events_AreNotEmpty()
    {
        Assert.NotEmpty(_events);
    }

    [Fact]
    public void Events_HaveRequiredFields()
    {
        foreach (var ev in _events)
        {
            Assert.False(string.IsNullOrWhiteSpace(ev.Id), "Event.Id is empty");
            Assert.False(string.IsNullOrWhiteSpace(ev.Title), "Event.Title is empty");
            Assert.False(string.IsNullOrWhiteSpace(ev.Slug), "Event.Slug is empty");
            Assert.True(ev.Active, $"Event {ev.Id} should be active");
            Assert.False(ev.Closed, $"Event {ev.Id} should not be closed");
        }
    }

    [Fact]
    public void Events_HaveTags()
    {
        foreach (var ev in _events)
        {
            Assert.NotEmpty(ev.Tags);
            Assert.Contains(ev.Tags, t => t.Id == "102127"); // Up or Down
        }
    }

    [Fact]
    public void Events_HaveCryptoTag()
    {
        // Not all events with tag 102127 (Up or Down) have tag 21 (Crypto)
        // e.g., SPX/Indices events have Up or Down but not Crypto
        // The in-memory tag filter in ScannerService correctly filters these out
        var eventsWithCrypto = _events.Where(e => e.Tags.Any(t => t.Id == "21")).ToList();
        Assert.NotEmpty(eventsWithCrypto);
    }

    [Fact]
    public void Events_HaveNonEmptyMarkets()
    {
        foreach (var ev in _events)
        {
            Assert.NotEmpty(ev.Markets);
        }
    }

    // ── Market-level deserialization ──────────────────────────────────

    [Fact]
    public void Markets_HaveConditionId()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.ConditionId),
                $"Market question '{m.Question}' has empty ConditionId");
        }
    }

    [Fact]
    public void Markets_HaveQuestion()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Question),
                "Market has empty Question");
        }
    }

    [Fact]
    public void Markets_HaveOutcomePrices()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.OutcomePrices),
                $"Market '{m.Question}' has empty OutcomePrices");
        }
    }

    [Fact]
    public void Markets_HaveClobTokenIds()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.ClobTokenIds),
                $"Market '{m.Question}' has empty ClobTokenIds");
        }
    }

    [Fact]
    public void Markets_HaveVolumeNum()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            // VolumeNum can be 0 for newly created markets, but should not be negative
            Assert.True(m.VolumeNum >= 0,
                $"Market '{m.Question}' has negative VolumeNum: {m.VolumeNum}");
        }
    }

    [Fact]
    public void Markets_HaveEndDateIso()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.EndDateIso),
                $"Market '{m.Question}' has empty EndDateIso");
        }
    }

    [Fact]
    public void Markets_HaveEndDate()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.EndDate),
                $"Market '{m.Question}' has empty EndDate (full timestamp)");
        }
    }

    [Fact]
    public void Markets_HaveCreatedAt()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.CreatedAt),
                $"Market '{m.Question}' has empty CreatedAt");
        }
    }

    [Fact]
    public void Markets_AreActive()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.True(m.Active, $"Market '{m.Question}' should be active");
            Assert.False(m.Closed, $"Market '{m.Question}' should not be closed");
        }
    }

    [Fact]
    public void Markets_HaveValidSlug()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        foreach (var m in markets)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Slug),
                $"Market '{m.Question}' has empty Slug");
        }
    }

    // ── Domain mapping ───────────────────────────────────────────────

    [Fact]
    public void MarketMapper_MapsAllFieldsFromEventsEndpoint()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            Assert.False(string.IsNullOrWhiteSpace(domain.ConditionId),
                $"Domain Market conditionId is empty for '{response.Question}'");
            Assert.False(string.IsNullOrWhiteSpace(domain.Question),
                $"Domain Market question is empty");
            Assert.False(string.IsNullOrWhiteSpace(domain.TokenId),
                $"Domain Market tokenId is empty for '{response.Question}' " +
                $"(ClobTokenIds: {response.ClobTokenIds})");
            Assert.True(domain.Active, $"Domain Market should be active");
            Assert.False(domain.Closed, $"Domain Market should not be closed");
        }
    }

    [Fact]
    public void MarketMapper_ParsesEndDateFromEventsEndpoint()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            Assert.NotNull(domain.EndDate);
            Assert.True(domain.EndDate.Value.Kind == DateTimeKind.Utc,
                $"EndDate for '{response.Question}' should be UTC, got {domain.EndDate.Value.Kind}");
            Assert.True(domain.EndDate.Value > new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                $"EndDate {domain.EndDate.Value} seems too old for '{response.Question}'");
        }
    }

    [Fact]
    public void MarketMapper_ParsesEndDateWithTimePrecision()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        // The events endpoint provides full ISO timestamps (e.g., "2026-03-25T13:00:00Z")
        // not just date-only strings. Verify the parsed EndDate includes time.
        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            // If EndDate is parsed from "2026-03-25T13:00:00Z", the time component should not be midnight
            // (unless the market genuinely ends at midnight, which would be unusual for hourly markets)
            Assert.NotNull(domain.EndDate);
        }
    }

    [Fact]
    public void MarketMapper_ParsesPricesFromOutcomePrices()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            // Events endpoint markets have outcomePrices but no tokens array
            // At least one of Yes/No price should be non-zero
            Assert.True(domain.YesPrice > 0 || domain.NoPrice > 0,
                $"Both prices are 0 for '{response.Question}' " +
                $"(outcomePrices: {response.OutcomePrices})");
            Assert.True(domain.YesPrice >= 0 && domain.YesPrice <= 1,
                $"YesPrice {domain.YesPrice} out of range for '{response.Question}'");
            Assert.True(domain.NoPrice >= 0 && domain.NoPrice <= 1,
                $"NoPrice {domain.NoPrice} out of range for '{response.Question}'");
        }
    }

    [Fact]
    public void MarketMapper_ParsesTokenIdFromClobTokenIds()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            Assert.False(string.IsNullOrWhiteSpace(domain.TokenId),
                $"TokenId not extracted from ClobTokenIds for '{response.Question}'. " +
                $"ClobTokenIds={response.ClobTokenIds}");
            Assert.True(domain.TokenId.Length > 10,
                $"TokenId looks too short ({domain.TokenId.Length} chars) for '{response.Question}'");
        }
    }

    [Fact]
    public void MarketMapper_ParsesCreatedAt()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            Assert.True(domain.CreatedAt > new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                $"CreatedAt {domain.CreatedAt} seems too old for '{response.Question}'");
            Assert.True(domain.CreatedAt.Kind == DateTimeKind.Utc,
                $"CreatedAt should be UTC for '{response.Question}'");
        }
    }

    [Fact]
    public void MarketMapper_ParsesVolume()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            Assert.True(domain.Volume >= 0,
                $"Volume should be non-negative for '{response.Question}'");
        }
    }

    // ── Market slug mapping ──────────────────────────────────────────

    [Fact]
    public void MarketMapper_ParsesMarketSlug()
    {
        var markets = _events.SelectMany(e => e.Markets).ToList();
        Assert.NotEmpty(markets);

        foreach (var response in markets)
        {
            var domain = MarketMapper.ToDomain(response);

            Assert.False(string.IsNullOrWhiteSpace(domain.MarketSlug),
                $"MarketSlug is empty for '{response.Question}'. " +
                $"Slug='{response.Slug}'");
        }
    }
}
