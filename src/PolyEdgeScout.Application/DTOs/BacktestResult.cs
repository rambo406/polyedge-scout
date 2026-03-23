namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Aggregated results from a backtest run.
/// </summary>
public record BacktestResult
{
    public double BrierScore { get; init; }
    public double WinRate { get; init; }
    public double EdgeAccuracy { get; init; }
    public double HypotheticalPnl { get; init; }
    public int TotalMarkets { get; init; }
    public int MarketsWithEdge { get; init; }
    public List<CalibrationBucket> CalibrationBuckets { get; init; } = [];
    public List<BacktestEntry> Entries { get; init; } = [];
}

/// <summary>
/// A calibration bucket grouping predictions by probability range.
/// </summary>
public record CalibrationBucket
{
    public string Range { get; init; } = "";
    public double AveragePredicted { get; init; }
    public double AverageActual { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// A single backtest result entry pairing model prediction with actual outcome.
/// </summary>
public record BacktestEntry
{
    public string MarketQuestion { get; init; } = "";
    public double ModelProbability { get; init; }
    public double MarketYesPrice { get; init; }
    public double Edge { get; init; }
    public double ActualOutcome { get; init; }
    public bool ModelCorrect { get; init; }
}
