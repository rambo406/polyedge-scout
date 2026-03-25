namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Abstraction for the Binance market data API client.
/// </summary>
public interface IBinanceApiClient
{
    /// <summary>Fetches 24h ticker data for a symbol from Binance.</summary>
    Task<BinanceTickerResponse?> FetchTickerAsync(string symbol, CancellationToken ct = default);

    /// <summary>Fetches kline (candlestick) data for a symbol from Binance.</summary>
    Task<IReadOnlyList<KlineData>?> FetchKlinesAsync(string symbol, string interval = "5m", int limit = 12, CancellationToken ct = default);
}
