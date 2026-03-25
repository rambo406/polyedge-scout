namespace PolyEdgeScout.Domain.ValueObjects;

using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Represents the result of an edge calculation for a market.
/// Uses a pluggable <see cref="IEdgeFormula"/> to compute the edge.
/// </summary>
/// <example>
/// <code>
/// var formula = new DefaultScaledEdgeFormula();
/// var calc = EdgeCalculation.Create(formula, 0.60, 0.45, 100_000, 60_000);
/// </code>
/// </example>
public readonly record struct EdgeCalculation
{
    public double ModelProbability { get; init; }
    public double MarketPrice { get; init; }

    /// <summary>
    /// The target price extracted from the market question, if available.
    /// </summary>
    public double? TargetPrice { get; init; }

    /// <summary>
    /// The current asset price at the time of calculation, if available.
    /// </summary>
    public double? CurrentAssetPrice { get; init; }

    /// <summary>
    /// The display name of the formula used to compute <see cref="Edge"/>.
    /// </summary>
    public string FormulaName { get; init; }

    /// <summary>
    /// The pre-computed edge value produced by the <see cref="IEdgeFormula"/>.
    /// </summary>
    public double Edge { get; init; }

    /// <summary>
    /// Creates a new <see cref="EdgeCalculation"/> using the supplied formula.
    /// </summary>
    public static EdgeCalculation Create(
        IEdgeFormula formula,
        double modelProbability,
        double marketPrice,
        double? targetPrice,
        double? currentAssetPrice) => new()
    {
        ModelProbability = modelProbability,
        MarketPrice = marketPrice,
        TargetPrice = targetPrice,
        CurrentAssetPrice = currentAssetPrice,
        FormulaName = formula.Name,
        Edge = formula.CalculateEdge(modelProbability, marketPrice, targetPrice, currentAssetPrice),
    };

    /// <summary>
    /// Returns <see langword="true"/> when the edge exceeds <paramref name="minEdge"/>.
    /// </summary>
    public bool HasSufficientEdge(double minEdge) => Edge > minEdge;

    /// <summary>
    /// Determines the recommended trade action based on the edge and minimum edge threshold.
    /// </summary>
    public TradeAction DetermineAction(double minEdge)
    {
        if (Edge > minEdge)
            return TradeAction.Buy;
        if (Edge < -minEdge)
            return TradeAction.Sell;
        return TradeAction.Hold;
    }

    public override string ToString()
    {
        var baseText = $"Model={ModelProbability:P1} Market={MarketPrice:P1} Edge={Edge:+0.00;-0.00}";

        if (TargetPrice.HasValue && CurrentAssetPrice.HasValue)
            return $"{baseText} Target=${TargetPrice.Value:N0} Current=${CurrentAssetPrice.Value:N0}";

        return baseText;
    }
}
