namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Complete edge backtest results comparing two timeframes.
/// </summary>
public record EdgeBacktestResult
{
    public EdgeBacktestTimeframeResult LeftTimeframeResult { get; init; } = new();
    public EdgeBacktestTimeframeResult RightTimeframeResult { get; init; } = new();
    public List<string> SelectedSymbols { get; init; } = [];
    public List<string> FormulaNames { get; init; } = [];
}
