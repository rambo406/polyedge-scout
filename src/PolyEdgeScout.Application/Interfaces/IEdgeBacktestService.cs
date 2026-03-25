namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Runs edge-focused backtests against resolved markets using all registered formulas.
/// </summary>
public interface IEdgeBacktestService
{
    /// <summary>
    /// Runs the edge backtest for the given symbols across two timeframes concurrently.
    /// </summary>
    Task<EdgeBacktestResult> RunEdgeBacktestAsync(
        List<string> symbols, string leftTimeframe, string rightTimeframe, CancellationToken ct = default);

    /// <summary>
    /// Raised after each market is evaluated, providing incremental progress.
    /// </summary>
    event EventHandler<EdgeBacktestProgressEvent>? OnProgress;
}
