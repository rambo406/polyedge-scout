namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Scans for markets and returns filtered crypto micro markets.
/// </summary>
public interface IScannerService
{
    /// <summary>Scans for markets and returns filtered crypto micro markets.</summary>
    Task<List<Market>> ScanMarketsAsync(CancellationToken ct = default);
}
