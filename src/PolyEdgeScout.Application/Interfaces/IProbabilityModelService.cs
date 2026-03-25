namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Calculates model probability for a market using volatility-scaled normal distribution.
/// </summary>
public interface IProbabilityModelService
{
    /// <summary>
    /// Calculates the model probability for a market.
    /// Returns <c>null</c> when the market question cannot be parsed or price data is unavailable.
    /// </summary>
    Task<ModelEvaluation?> CalculateProbabilityAsync(Market market, CancellationToken ct = default);
}
