namespace PolyEdgeScout.Domain.Entities;

using PolyEdgeScout.Domain.Enums;

public sealed record Trade
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string MarketQuestion { get; init; } = "";
    public string ConditionId { get; init; } = "";
    public string TokenId { get; init; } = "";
    public TradeAction Action { get; init; }
    public TradeStatus Status { get; init; } = TradeStatus.Pending;
    public double EntryPrice { get; init; }
    public double Shares { get; init; }
    public double ModelProbability { get; init; }
    public double Edge { get; init; }
    public double Outlay { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsPaper { get; init; } = true;
    public string? TxHash { get; init; }
}
