namespace PolyEdgeScout.Domain.Services;

using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Default edge formula that scales the base edge by the price ratio
/// when both target and current asset prices are available.
/// </summary>
/// <remarks>
/// Formula: baseEdge = modelProbability − marketPrice.
/// When targetPrice and currentAssetPrice are present (and currentAssetPrice &gt; 0),
/// the edge is scaled: baseEdge × (1 + |targetPrice − currentAssetPrice| / currentAssetPrice).
/// </remarks>
/// <example>
/// <code>
/// var formula = new DefaultScaledEdgeFormula();
/// double edge = formula.CalculateEdge(0.60, 0.45, 100_000, 60_000);
/// // baseEdge = 0.15, priceRatio ≈ 0.6667, edge ≈ 0.25
/// </code>
/// </example>
public sealed class DefaultScaledEdgeFormula : IEdgeFormula
{
    /// <inheritdoc />
    public string Name => "Scaled";

    /// <inheritdoc />
    public double CalculateEdge(double modelProbability, double marketPrice, double? targetPrice, double? currentAssetPrice)
    {
        var baseEdge = modelProbability - marketPrice;

        if (targetPrice.HasValue && currentAssetPrice.HasValue && currentAssetPrice.Value > 0)
        {
            var priceRatio = Math.Abs(targetPrice.Value - currentAssetPrice.Value) / currentAssetPrice.Value;
            return baseEdge * (1 + priceRatio);
        }

        return baseEdge;
    }
}
