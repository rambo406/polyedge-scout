namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// A single entry from the edge backtest — one market evaluated by one formula.
/// </summary>
public record EdgeBacktestEntry
{
    public string MarketQuestion { get; init; } = "";
    public double ModelProbability { get; init; }
    public double MarketYesPrice { get; init; }
    public double Edge { get; init; }
    public double ActualOutcome { get; init; }
    public bool ModelCorrect { get; init; }
    public string Timeframe { get; init; } = "";
    public string FormulaName { get; init; } = "";
    public string Symbol { get; init; } = "";
    public double PnL { get; init; }
}
