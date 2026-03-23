namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Runs backtests against resolved markets to measure model accuracy.
/// </summary>
public interface IBacktestService
{
    Task<BacktestResult> RunBacktestAsync(CancellationToken ct = default);
}
