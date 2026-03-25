namespace PolyEdgeScout.Domain.Services;

using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Simple edge formula: modelProbability − marketPrice with no scaling.
/// </summary>
/// <example>
/// <code>
/// var formula = new BaseEdgeFormula();
/// double edge = formula.CalculateEdge(0.60, 0.45, null, null);
/// // edge = 0.15
/// </code>
/// </example>
public sealed class BaseEdgeFormula : IEdgeFormula
{
    /// <inheritdoc />
    public string Name => "Base";

    /// <inheritdoc />
    public double CalculateEdge(double modelProbability, double marketPrice, double? targetPrice, double? currentAssetPrice)
    {
        return modelProbability - marketPrice;
    }
}
