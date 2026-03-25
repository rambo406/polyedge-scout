namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Results for a single timeframe in the edge backtest.
/// </summary>
public record EdgeBacktestTimeframeResult
{
    public string Timeframe { get; init; } = "";
    public List<EdgeFormulaResult> FormulaResults { get; init; } = [];
    public List<EdgeSymbolResult> SymbolResults { get; init; } = [];
    public int TotalMarkets { get; init; }
    public double GrandTotalPnl { get; init; }
    public double GrandTotalRoi { get; init; }
}
