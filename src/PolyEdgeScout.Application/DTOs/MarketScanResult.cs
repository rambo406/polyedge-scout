namespace PolyEdgeScout.Application.DTOs;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Represents a market scan result with model evaluation data.
/// </summary>
public record MarketScanResult
{
    public Market Market { get; init; } = null!;
    public double ModelProbability { get; init; }
    public double Edge { get; init; }
    public string Action { get; init; } = "HOLD";
}
