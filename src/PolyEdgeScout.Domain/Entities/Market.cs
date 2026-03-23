namespace PolyEdgeScout.Domain.Entities;

public sealed class Market
{
    public string ConditionId { get; init; } = "";
    public string QuestionId { get; init; } = "";
    public string TokenId { get; init; } = "";
    public string Question { get; init; } = "";
    public double YesPrice { get; init; }
    public double NoPrice { get; init; }
    public double Volume { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EndDate { get; init; }
    public string MarketSlug { get; init; } = "";
    public bool Active { get; init; }
    public bool Closed { get; init; }
}
