namespace PolyEdgeScout.Domain.Services;

/// <summary>
/// Classifies markets into niches (e.g., "crypto micro").
/// Consolidates duplicate classification logic from ScannerService and BacktestService.
/// </summary>
public static class MarketClassifier
{
    /// <summary>
    /// Determines if a market qualifies as a "crypto micro" market using configurable keyword lists.
    /// Applies exclude-first evaluation: markets matching any exclude keyword are rejected
    /// regardless of include keyword matches.
    /// </summary>
    /// <param name="question">The market question text.</param>
    /// <param name="includeKeywords">Keywords that indicate relevance. At least one must match.</param>
    /// <param name="excludeKeywords">Keywords that disqualify a market. Any match causes rejection.</param>
    public static bool IsCryptoMicro(string question, IReadOnlyList<string> includeKeywords, IReadOnlyList<string> excludeKeywords)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var lower = question.ToLowerInvariant();

        // Exclude-first: reject if any exclude keyword matches
        if (excludeKeywords.Any(kw => lower.Contains(kw, StringComparison.Ordinal)))
            return false;

        // Include: require at least one include keyword match
        return includeKeywords.Any(kw => lower.Contains(kw, StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if a market meets the freshness/volume criteria for scanning.
    /// Market must have volume &gt;= minVolume to exclude zero-liquidity markets.
    /// When maxVolume is 0 (disabled), only the minVolume check applies.
    /// Otherwise, market must be created in last 24h OR have volume &lt; maxVolume.
    /// </summary>
    public static bool MeetsFilterCriteria(DateTime createdAt, double volume, double maxVolume, double minVolume = 0)
    {
        if (volume < minVolume)
            return false;

        // maxVolume=0 means no max volume limit — accept all markets above minVolume
        if (maxVolume <= 0)
            return true;

        var isRecent = (DateTime.UtcNow - createdAt).TotalHours < 24;
        var isLowVolume = volume < maxVolume;
        return isRecent || isLowVolume;
    }
}
