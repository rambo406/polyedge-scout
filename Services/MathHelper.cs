namespace PolyEdgeScout.Services;

/// <summary>
/// Static math utilities for probability calculations and Kelly criterion bet sizing.
/// </summary>
public static class MathHelper
{
    /// <summary>
    /// Error function approximation using the Abramowitz and Stegun formula 7.1.26.
    /// Maximum error: 1.5×10⁻⁷.
    /// </summary>
    /// <param name="x">Input value.</param>
    /// <returns>Approximation of erf(x).</returns>
    public static double Erf(double x)
    {
        // Constants from Abramowitz and Stegun 7.1.26
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        // Preserve sign
        int sign = x < 0 ? -1 : 1;
        double absX = Math.Abs(x);

        double t = 1.0 / (1.0 + p * absX);
        double t2 = t * t;
        double t3 = t2 * t;
        double t4 = t3 * t;
        double t5 = t4 * t;

        double y = 1.0 - (a1 * t + a2 * t2 + a3 * t3 + a4 * t4 + a5 * t5) * Math.Exp(-absX * absX);

        return sign * y;
    }

    /// <summary>
    /// Standard normal cumulative distribution function using the error function.
    /// </summary>
    /// <param name="x">Input value (z-score).</param>
    /// <returns>P(Z ≤ x) for standard normal Z.</returns>
    public static double NormCdf(double x) => 0.5 * (1.0 + Erf(x / Math.Sqrt(2.0)));

    /// <summary>
    /// Kelly criterion optimal bet fraction: f* = (b·p − q) / b.
    /// <para>
    /// b = net odds received on the bet (decimalOdds − 1).
    /// p = probability of winning.
    /// q = 1 − p (probability of losing).
    /// </para>
    /// </summary>
    /// <param name="probability">Estimated probability of the outcome (0–1).</param>
    /// <param name="decimalOdds">Decimal odds (e.g. 2.0 means even money).</param>
    /// <returns>Fraction of bankroll to bet. Negative means don't bet.</returns>
    public static double KellyFraction(double probability, double decimalOdds)
    {
        double b = decimalOdds - 1.0;
        double q = 1.0 - probability;

        if (b <= 0)
            return 0;

        return (b * probability - q) / b;
    }

    /// <summary>
    /// Calculate bet size using fractional Kelly criterion, capped at a maximum bankroll percentage.
    /// <para>
    /// Decimal odds are derived from the entry price: odds = 1 / entryPrice
    /// (since Polymarket shares pay $1 on success).
    /// </para>
    /// </summary>
    /// <param name="probability">Model's estimated probability (0–1).</param>
    /// <param name="entryPrice">Yes-share price on Polymarket (e.g. 0.55).</param>
    /// <param name="bankroll">Current total bankroll in USD.</param>
    /// <param name="kellyFraction">Fractional Kelly multiplier (e.g. 0.5 for half-Kelly).</param>
    /// <param name="maxBankrollPercent">Maximum fraction of bankroll to risk on a single bet.</param>
    /// <returns>Dollar amount to bet, ≥ 0.</returns>
    public static double KellyBetSize(
        double probability,
        double entryPrice,
        double bankroll,
        double kellyFraction,
        double maxBankrollPercent)
    {
        if (entryPrice <= 0 || bankroll <= 0)
            return 0;

        // Polymarket pays $1 per share, so decimal odds = 1 / entryPrice
        double decimalOdds = 1.0 / entryPrice;

        double fullKelly = KellyFraction(probability, decimalOdds);
        double fractional = fullKelly * kellyFraction;

        double betSize = fractional * bankroll;
        double cap = maxBankrollPercent * bankroll;

        return Math.Max(0, Math.Min(betSize, cap));
    }
}
