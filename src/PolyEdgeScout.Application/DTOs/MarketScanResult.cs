namespace PolyEdgeScout.Application.DTOs;

using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;

/// <summary>
/// Represents a market scan result with model evaluation data.
/// </summary>
public record MarketScanResult
{
    public Market Market { get; init; } = null!;
    public double ModelProbability { get; init; }
    public double Edge { get; init; }
    public TradeAction Action { get; init; } = TradeAction.Hold;

    /// <summary>
    /// The target price extracted from the market question, if available.
    /// </summary>
    public double? TargetPrice { get; init; }

    /// <summary>
    /// The current asset price at the time of evaluation, if available.
    /// </summary>
    public double? CurrentAssetPrice { get; init; }
}
