namespace PolyEdgeScout.Domain.Services;

using PolyEdgeScout.Domain.Enums;

/// <summary>
/// Discriminated result of parsing a market question.
/// Use pattern matching to determine the market type.
/// </summary>
public abstract record ParseResult
{
    private ParseResult() { }

    /// <summary>
    /// A market with a specific crypto token and target price (e.g., "Will Bitcoin hit $100K?").
    /// </summary>
    public sealed record PriceTarget(string Symbol, double TargetPrice) : ParseResult;

    /// <summary>
    /// A directional market asking whether a token will go up or down in a time window.
    /// </summary>
    public sealed record Directional(
        string Symbol,
        MarketDirection Direction,
        TimeOnly? WindowStart = null,
        TimeOnly? WindowEnd = null,
        string? Timezone = null) : ParseResult;

    /// <summary>
    /// A market question that could not be recognized as any supported type.
    /// </summary>
    public sealed record Unrecognised : ParseResult;
}
