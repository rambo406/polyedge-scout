namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Orchestrates the scan-evaluate-trade pipeline.
/// Extracts business logic from the UI layer into a testable, reusable service.
/// </summary>
public interface IScanOrchestrationService
{
    /// <summary>
    /// Scans markets, calculates model probability and edge for each,
    /// and returns evaluated results sorted by edge descending.
    /// </summary>
    Task<IReadOnlyList<MarketScanResult>> ScanAndEvaluateAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs a full scan cycle: scan, evaluate, and auto-trade on qualifying markets.
    /// Returns the evaluated market list for display.
    /// </summary>
    Task<IReadOnlyList<MarketScanResult>> ScanEvaluateAndAutoTradeAsync(CancellationToken ct = default);
}
