namespace PolyEdgeScout.Models;

public record PnlSnapshot
{
    public double Bankroll { get; init; }
    public int OpenPositions { get; init; }
    public double UnrealizedPnl { get; init; }
    public double RealizedPnl { get; init; }
    public double TotalPnl => UnrealizedPnl + RealizedPnl;
    public List<TradeResult> LastTrades { get; init; } = new();
}

public record TradeResult
{
    public string TradeId { get; init; } = "";
    public string MarketQuestion { get; init; } = "";
    public double EntryPrice { get; init; }
    public double Shares { get; init; }
    public double GrossProfit { get; init; }
    public double Fees { get; init; }
    public double Gas { get; init; }
    public double NetProfit => GrossProfit - Fees - Gas;
    public double Roi { get; init; }
    public DateTime SettledAt { get; init; }
    public bool Won { get; init; }
}
