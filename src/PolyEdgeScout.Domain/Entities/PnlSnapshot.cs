namespace PolyEdgeScout.Domain.Entities;

public sealed record PnlSnapshot
{
    public double Bankroll { get; init; }
    public int OpenPositions { get; init; }
    public double UnrealizedPnl { get; init; }
    public double RealizedPnl { get; init; }
    public double TotalPnl => UnrealizedPnl + RealizedPnl;
    public List<TradeResult> LastTrades { get; init; } = [];
}
