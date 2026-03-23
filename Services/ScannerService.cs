using System.Text.Json;
using System.Text.RegularExpressions;
using PolyEdgeScout.Models;

namespace PolyEdgeScout.Services;

/// <summary>
/// Scans the Polymarket Gamma API for active crypto micro-markets that match
/// the bot's filtering criteria (fresh, low-volume, crypto-price-related).
/// </summary>
public sealed class ScannerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Case-insensitive keywords used to identify crypto micro-market questions.
    /// </summary>
    private static readonly string[] CryptoKeywords =
    [
        "hit", "reach", "milestone", "eod", "today", "price",
        "above", "below", "by", "$",
        "bitcoin", "btc", "eth", "sol", "doge", "xrp", "ada",
        "bnb", "avax", "matic", "link", "dot",
    ];

    /// <summary>
    /// Regex for recognising well-known crypto ticker symbols in question text.
    /// </summary>
    private static readonly Regex TokenSymbolRegex = new(
        @"\b(BTC|ETH|SOL|DOGE|XRP|ADA|BNB|AVAX|MATIC|LINK|DOT|SHIB|PEPE|ARB|OP|SUI|APT|SEI|TIA|NEAR|ATOM|FTM|INJ|RUNE|AAVE|UNI|MKR|LDO|CRV|SNX)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Regex for $SYMBOL patterns like "$BTC" or "$ETH".
    /// </summary>
    private static readonly Regex DollarSymbolRegex = new(
        @"\$([A-Z]{2,6})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Maps full cryptocurrency names to their ticker symbols (case-insensitive lookup).
    /// </summary>
    private static readonly Dictionary<string, string> NameToSymbol = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bitcoin"] = "BTC",
        ["ethereum"] = "ETH",
        ["solana"] = "SOL",
        ["dogecoin"] = "DOGE",
        ["ripple"] = "XRP",
        ["cardano"] = "ADA",
        ["avalanche"] = "AVAX",
        ["polygon"] = "MATIC",
        ["chainlink"] = "LINK",
        ["polkadot"] = "DOT",
    };

    /// <summary>
    /// Regex for price targets: $50,000  /  $100K  /  $1.5M  /  50000 dollars  etc.
    /// </summary>
    private static readonly Regex PriceRegex = new(
        @"\$[\d,]+(?:\.\d+)?[KkMm]?|\b[\d,]+(?:\.\d+)?\s*(?:dollars|usd|USD|\$)|(?<=(?:price|hit|reach|above|below|target)\s{0,5})\b[\d,]+(?:\.\d+)?[KkMm]?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AppConfig _config;
    private readonly HttpClient _http;
    private readonly LogService _log;

    /// <summary>
    /// Initializes a new <see cref="ScannerService"/>.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="httpClient">Shared HTTP client for Gamma API calls.</param>
    /// <param name="logService">Logging service.</param>
    public ScannerService(AppConfig config, HttpClient httpClient, LogService logService)
    {
        _config = config;
        _http = httpClient;
        _log = logService;
    }

    /// <summary>
    /// Queries the Gamma API for active markets, filters for crypto micro-markets,
    /// and returns the matching <see cref="Market"/> list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Filtered list of crypto micro-markets.</returns>
    public async Task<List<Market>> ScanMarketsAsync(CancellationToken ct)
    {
        string url = $"{_config.GammaApiBaseUrl}/markets" +
                     "?active=true&closed=false&volume_num_max=5000" +
                     "&order=created_at&ascending=false&limit=50";

        _log.Info($"Scanning markets: {url}");

        string json = await FetchWithRetryAsync(url, ct);

        List<GammaMarketResponse>? responses = JsonSerializer.Deserialize<List<GammaMarketResponse>>(json, JsonOptions);

        if (responses is null || responses.Count == 0)
        {
            _log.Warn("No markets returned from Gamma API.");
            return [];
        }

        _log.Info($"Fetched {responses.Count} raw markets from Gamma.");

        DateTime cutoff = DateTime.UtcNow.AddHours(-24);
        var markets = new List<Market>();

        foreach (GammaMarketResponse response in responses)
        {
            Market market = Market.FromGammaResponse(response);

            // Filter: must contain a crypto keyword
            if (!IsCryptoMicro(market.Question))
            {
                _log.Debug($"Skipped (no crypto keyword): {Truncate(market.Question)}");
                continue;
            }

            // Filter: created in last 24 h OR volume < 3000
            if (market.CreatedAt < cutoff && market.Volume >= 3000)
            {
                _log.Debug($"Skipped (too old & high volume): {Truncate(market.Question)}");
                continue;
            }

            _log.Info($"Match: {Truncate(market.Question)}  | Yes={market.YesPrice:F3} Vol={market.Volume:F0}");
            markets.Add(market);
        }

        _log.Info($"Scanner found {markets.Count} crypto micro-markets.");
        return markets;
    }

    /// <summary>
    /// Extracts a crypto token ticker symbol (e.g. BTC, ETH) from the given question text.
    /// Checks for explicit ticker symbols, $SYMBOL patterns and full crypto names.
    /// </summary>
    /// <param name="question">Market question text.</param>
    /// <returns>Uppercase ticker symbol, or <c>null</c> if none found.</returns>
    public string? ExtractTokenSymbol(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return null;

        // 1. Direct ticker match (BTC, ETH, …)
        Match tickerMatch = TokenSymbolRegex.Match(question);
        if (tickerMatch.Success)
            return tickerMatch.Groups[1].Value.ToUpperInvariant();

        // 2. $SYMBOL pattern
        Match dollarMatch = DollarSymbolRegex.Match(question);
        if (dollarMatch.Success)
            return dollarMatch.Groups[1].Value.ToUpperInvariant();

        // 3. Full name match (Bitcoin → BTC, …)
        foreach (KeyValuePair<string, string> kvp in NameToSymbol)
        {
            if (question.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }

    /// <summary>
    /// Extracts a numeric price target from the given question text,
    /// handling dollar signs, commas, and K/M suffixes.
    /// </summary>
    /// <param name="question">Market question text.</param>
    /// <returns>Parsed price as a double, or <c>null</c> if none found.</returns>
    public double? ExtractTargetPrice(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return null;

        Match match = PriceRegex.Match(question);
        if (!match.Success)
            return null;

        return ParsePriceToken(match.Value);
    }

    /// <summary>
    /// Parses a raw price string like "$50,000", "100K", or "1.5M" into a double.
    /// </summary>
    private static double? ParsePriceToken(string raw)
    {
        // Strip non-numeric decoration except digits, dots, commas, K, M
        string cleaned = raw
            .Replace("$", "", StringComparison.Ordinal)
            .Replace("dollars", "", StringComparison.OrdinalIgnoreCase)
            .Replace("usd", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        double multiplier = 1.0;
        if (cleaned.EndsWith('K') || cleaned.EndsWith('k'))
        {
            multiplier = 1_000;
            cleaned = cleaned[..^1];
        }
        else if (cleaned.EndsWith('M') || cleaned.EndsWith('m'))
        {
            multiplier = 1_000_000;
            cleaned = cleaned[..^1];
        }

        cleaned = cleaned.Replace(",", "", StringComparison.Ordinal).Trim();

        if (double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return value * multiplier;
        }

        return null;
    }

    /// <summary>
    /// Fetches a URL with exponential-backoff retry on HTTP 429 responses (max 3 retries).
    /// </summary>
    private async Task<string> FetchWithRetryAsync(string url, CancellationToken ct)
    {
        const int maxRetries = 3;
        int delayMs = 1000;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            using HttpResponseMessage response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _log.Error($"Rate-limited after {maxRetries} retries: {url}");
                    response.EnsureSuccessStatusCode(); // will throw
                }

                _log.Warn($"429 Too Many Requests — retrying in {delayMs}ms (attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

        // Unreachable, but satisfies the compiler
        throw new InvalidOperationException("Exhausted retries.");
    }

    /// <summary>
    /// Checks whether a question string contains any crypto micro-market keyword.
    /// </summary>
    private static bool IsCryptoMicro(string question)
    {
        foreach (string keyword in CryptoKeywords)
        {
            if (question.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Truncates a string to a maximum length for log readability.
    /// </summary>
    private static string Truncate(string text, int max = 80) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");
}
