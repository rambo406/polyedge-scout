namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Abstraction for the Polymarket CLOB order submission client.
/// </summary>
public interface IClobClient
{
    /// <summary>Posts a signed limit order to the Polymarket CLOB.</summary>
    Task<string?> PostOrderAsync(Trade trade, CancellationToken ct = default);

    /// <summary>
    /// Fetches the midpoint price (average of best bid and best ask) for a given token
    /// from the CLOB orderbook. Returns null if no orderbook data is available.
    /// </summary>
    /// <param name="tokenId">The token ID to look up in the CLOB orderbook.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<double?> GetMidpointPriceAsync(string tokenId, CancellationToken ct = default);
}
