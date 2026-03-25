namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Progress event raised during edge backtest execution for live UI updates.
/// </summary>
public record EdgeBacktestProgressEvent
{
    public EdgeBacktestEntry Entry { get; init; } = new();
    public string FormulaName { get; init; } = "";
    public string Timeframe { get; init; } = "";
    public int EvaluatedCount { get; init; }
    public int TotalCount { get; init; }
    public EdgeBacktestTimeframeResult RunningMetrics { get; init; } = new();
}
