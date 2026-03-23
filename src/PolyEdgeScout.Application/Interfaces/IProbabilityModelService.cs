namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Calculates model probability for a market using volatility-scaled normal distribution.
/// </summary>
public interface IProbabilityModelService
{
    /// <summary>Calculates the model probability for a market.</summary>
    Task<double> CalculateProbabilityAsync(Market market, CancellationToken ct = default);
}
