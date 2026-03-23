namespace PolyEdgeScout.Domain.ValueObjects;

/// <summary>
/// Represents the result of an edge calculation for a market.
/// </summary>
public readonly record struct EdgeCalculation
{
    public double ModelProbability { get; init; }
    public double MarketPrice { get; init; }
    public double Edge => ModelProbability - MarketPrice;
    public bool HasSufficientEdge(double minEdge) => Edge > minEdge;

    public override string ToString() => $"Model={ModelProbability:P1} Market={MarketPrice:P1} Edge={Edge:+0.00;-0.00}";
}
