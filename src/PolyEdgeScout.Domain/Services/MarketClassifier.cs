namespace PolyEdgeScout.Domain.Services;

/// <summary>
/// Classifies markets into niches (e.g., "crypto micro").
/// Consolidates duplicate classification logic from ScannerService and BacktestService.
/// </summary>
public static class MarketClassifier
{
    private static readonly string[] CryptoKeywords =
    [
        "hit", "reach", "milestone", "eod", "today", "price", "above", "below",
        "by", "$", "bitcoin", "btc", "eth", "sol", "doge", "xrp", "ada", "bnb",
        "avax", "matic", "link", "dot", "crypto", "token", "coin",
        "shib", "pepe", "arb", "op", "sui", "apt", "sei", "tia", "near",
        "atom", "ftm", "inj", "rune", "aave", "uni", "mkr", "ldo", "crv", "snx"
    ];

    /// <summary>
    /// Determines if a market qualifies as a "crypto micro" market.
    /// A market qualifies if its question contains any crypto-related keyword.
    /// </summary>
    public static bool IsCryptoMicro(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var lower = question.ToLowerInvariant();
        return CryptoKeywords.Any(kw => lower.Contains(kw));
    }

    /// <summary>
    /// Checks if a market meets the freshness/volume criteria for scanning.
    /// Market must be created in last 24h OR have volume &lt; maxVolume.
    /// </summary>
    public static bool MeetsFilterCriteria(DateTime createdAt, double volume, double maxVolume)
    {
        var isRecent = (DateTime.UtcNow - createdAt).TotalHours < 24;
        var isLowVolume = volume < maxVolume;
        return isRecent || isLowVolume;
    }
}
