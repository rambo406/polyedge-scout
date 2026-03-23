namespace PolyEdgeScout.Domain.ValueObjects;

using PolyEdgeScout.Domain.Services;

/// <summary>
/// Encapsulates bet sizing logic including flat bet and Kelly criterion.
/// </summary>
public readonly record struct BetSizing
{
    public double FlatBet { get; init; }
    public double KellyBet { get; init; }
    public double RecommendedBet => Math.Max(0, Math.Min(FlatBet, KellyBet));
    public double Shares { get; init; }

    public static BetSizing Calculate(
        double modelProbability,
        double entryPrice,
        double bankroll,
        double defaultBetSize,
        double kellyFraction,
        double maxBankrollPercent)
    {
        var kellyBet = MathHelper.KellyBetSize(
            modelProbability, entryPrice, bankroll, kellyFraction, maxBankrollPercent);
        var flatBet = Math.Min(defaultBetSize, bankroll * maxBankrollPercent);
        var bet = Math.Max(0, Math.Min(flatBet, kellyBet));
        var shares = entryPrice > 0 ? bet / entryPrice : 0;

        return new BetSizing
        {
            FlatBet = flatBet,
            KellyBet = kellyBet,
            Shares = shares
        };
    }
}
