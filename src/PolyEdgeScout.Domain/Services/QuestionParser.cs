namespace PolyEdgeScout.Domain.Services;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Extracts structured data (token symbols, target prices) from market question text.
/// Consolidates duplicate parsing logic that was previously in ScannerService and ProbabilityModelService.
/// </summary>
public static partial class QuestionParser
{
    // Use source-generated regex for performance (.NET 7+)

    private static readonly Dictionary<string, string> FullNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bitcoin"] = "BTC", ["Ethereum"] = "ETH", ["Solana"] = "SOL",
        ["Dogecoin"] = "DOGE", ["Ripple"] = "XRP", ["Cardano"] = "ADA",
        ["Avalanche"] = "AVAX", ["Polygon"] = "MATIC", ["Chainlink"] = "LINK",
        ["Polkadot"] = "DOT", ["Shiba"] = "SHIB", ["Arbitrum"] = "ARB",
        ["Optimism"] = "OP", ["Sui"] = "SUI", ["Aptos"] = "APT",
        ["Sei"] = "SEI", ["Celestia"] = "TIA", ["Near"] = "NEAR",
        ["Cosmos"] = "ATOM", ["Fantom"] = "FTM", ["Injective"] = "INJ",
        ["THORChain"] = "RUNE", ["Aave"] = "AAVE", ["Uniswap"] = "UNI",
        ["Maker"] = "MKR", ["Lido"] = "LDO", ["Curve"] = "CRV",
        ["Synthetix"] = "SNX", ["Pepe"] = "PEPE", ["Binance Coin"] = "BNB"
    };

    private static readonly string[] TokenSymbols =
    [
        "BTC", "ETH", "SOL", "DOGE", "XRP", "ADA", "BNB", "AVAX", "MATIC", "LINK",
        "DOT", "SHIB", "PEPE", "ARB", "OP", "SUI", "APT", "SEI", "TIA", "NEAR",
        "ATOM", "FTM", "INJ", "RUNE", "AAVE", "UNI", "MKR", "LDO", "CRV", "SNX"
    ];

    /// <summary>
    /// Extracts a crypto token symbol from market question text.
    /// Checks for $TOKEN format, bare symbol matches, and full cryptocurrency names.
    /// </summary>
    public static string? ExtractTokenSymbol(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return null;

        // Check $TOKEN pattern first (e.g., "$BTC", "$ETH")
        var dollarMatch = Regex.Match(question, @"\$([A-Z]{2,6})\b", RegexOptions.IgnoreCase);
        if (dollarMatch.Success)
        {
            var sym = dollarMatch.Groups[1].Value.ToUpperInvariant();
            if (TokenSymbols.Contains(sym)) return sym;
        }

        // Check full names (e.g. "Bitcoin", "Ethereum")
        foreach (var (name, symbol) in FullNameMap)
        {
            if (question.Contains(name, StringComparison.OrdinalIgnoreCase))
                return symbol;
        }

        // Check bare symbols (e.g. "BTC", "ETH") - word boundary match
        var symbolPattern = @"\b(" + string.Join("|", TokenSymbols) + @")\b";
        var symbolMatch = Regex.Match(question, symbolPattern, RegexOptions.IgnoreCase);
        if (symbolMatch.Success)
            return symbolMatch.Groups[1].Value.ToUpperInvariant();

        return null;
    }

    /// <summary>
    /// Extracts a target price from market question text.
    /// Handles formats: $50,000 | $50K | $1.5M | 50000 USD | etc.
    /// </summary>
    public static double? ExtractTargetPrice(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return null;

        // Pattern 1: $XX,XXX.XX with optional K/M suffix
        var match = Regex.Match(question, @"\$\s*([\d,]+(?:\.\d+)?)\s*([KkMm])?");
        if (match.Success)
            return ParsePriceToken(match.Groups[1].Value, match.Groups[2].Value);

        // Pattern 2: XX,XXX USD or XX,XXX dollars
        match = Regex.Match(question, @"([\d,]+(?:\.\d+)?)\s*(?:dollars|usd|USD|USDT|usdt)", RegexOptions.IgnoreCase);
        if (match.Success)
            return ParsePriceToken(match.Groups[1].Value, "");

        // Pattern 3: Large standalone numbers near price context words
        match = Regex.Match(question, @"(?:above|below|hit|reach|cross|break|exceed|surpass|target|price)\s+\$?\s*([\d,]+(?:\.\d+)?)\s*([KkMm])?", RegexOptions.IgnoreCase);
        if (match.Success)
            return ParsePriceToken(match.Groups[1].Value, match.Groups[2].Value);

        return null;
    }

    /// <summary>
    /// Parses a price string with optional K/M suffix.
    /// </summary>
    public static double? ParsePriceToken(string numStr, string suffix)
    {
        var cleaned = numStr.Replace(",", "");
        if (!double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return null;

        var sfx = suffix?.Trim().ToUpperInvariant() ?? "";
        return sfx switch
        {
            "K" => value * 1_000,
            "M" => value * 1_000_000,
            _ => value
        };
    }

    /// <summary>
    /// Extracts both token symbol and target price from a market question.
    /// </summary>
    public static (string? Symbol, double? TargetPrice) Parse(string question)
        => (ExtractTokenSymbol(question), ExtractTargetPrice(question));
}
