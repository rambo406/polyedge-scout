namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Encapsulates the result of a probability model evaluation,
/// including optional target and current asset prices for edge scaling.
/// </summary>
public record ModelEvaluation(
    double ModelProbability,
    double? TargetPrice,
    double? CurrentAssetPrice);
