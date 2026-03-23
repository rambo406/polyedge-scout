namespace PolyEdgeScout.Domain.Entities;

public sealed record TradeResult
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
