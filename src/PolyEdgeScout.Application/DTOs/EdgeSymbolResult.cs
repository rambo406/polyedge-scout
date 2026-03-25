namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Per-symbol breakdown of backtest results.
/// </summary>
public record EdgeSymbolResult
{
    public string Symbol { get; init; } = "";
    public int Markets { get; init; }
    public double WinRate { get; init; }
    public double PnL { get; init; }
    public double Roi { get; init; }
}
