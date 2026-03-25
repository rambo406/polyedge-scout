namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Abstraction for the Polymarket Gamma API client.
/// </summary>
public interface IGammaApiClient
{
    /// <summary>Fetches active markets from the Gamma API with retry logic.</summary>
    Task<List<GammaMarketResponse>> FetchActiveMarketsAsync(CancellationToken ct = default);

    /// <summary>Fetches resolved/closed markets for backtesting.</summary>
    Task<List<GammaMarketResponse>> FetchResolvedMarketsAsync(int limit = 100, CancellationToken ct = default);

    /// <summary>Fetches active crypto events from the Gamma API events endpoint with tag-based filtering.</summary>
    Task<List<GammaEventResponse>> FetchActiveEventsAsync(CancellationToken ct = default);
}
