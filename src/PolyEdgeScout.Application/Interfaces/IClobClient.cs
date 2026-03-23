namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Abstraction for the Polymarket CLOB order submission client.
/// </summary>
public interface IClobClient
{
    /// <summary>Posts a signed limit order to the Polymarket CLOB.</summary>
    Task<string?> PostOrderAsync(Trade trade, CancellationToken ct = default);
}
