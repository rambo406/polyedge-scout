namespace PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Computes an edge value from market data using a specific formula.
/// </summary>
/// <example>
/// <code>
/// IEdgeFormula formula = new DefaultScaledEdgeFormula();
/// double edge = formula.CalculateEdge(0.60, 0.45, 100_000, 60_000);
/// </code>
/// </example>
public interface IEdgeFormula
{
    /// <summary>The display name of the formula.</summary>
    string Name { get; }

    /// <summary>
    /// Calculates the edge given market parameters.
    /// </summary>
    /// <param name="modelProbability">Model-estimated probability of the outcome.</param>
    /// <param name="marketPrice">Current market yes-price (0–1).</param>
    /// <param name="targetPrice">Optional target asset price from the question.</param>
    /// <param name="currentAssetPrice">Optional current asset price.</param>
    /// <returns>The computed edge value.</returns>
    double CalculateEdge(double modelProbability, double marketPrice, double? targetPrice, double? currentAssetPrice);
}
