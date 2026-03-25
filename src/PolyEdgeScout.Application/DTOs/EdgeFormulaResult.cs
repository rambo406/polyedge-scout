namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Aggregated metrics for a single formula's backtest performance.
/// </summary>
public record EdgeFormulaResult
{
    public string FormulaName { get; init; } = "";
    public double WinRate { get; init; }
    public double EdgeAccuracy { get; init; }
    public double HypotheticalPnl { get; init; }
    public double Roi { get; init; }
    public int TotalMarkets { get; init; }
    public int MarketsWithEdge { get; init; }
    public List<EdgeBacktestEntry> Entries { get; init; } = [];
}
