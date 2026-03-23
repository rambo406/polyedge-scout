using System.Text.Json;
using System.Text.RegularExpressions;
using PolyEdgeScout.Models;

namespace PolyEdgeScout.Services;

/// <summary>
/// Crypto milestone probability model.
/// Fetches real-time price data from Binance and uses a volatility-scaled
/// normal-distribution model to estimate the probability that a token will
/// reach a target price before the market deadline.
/// </summary>
public sealed class ProbabilityModelService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Regex for recognising well-known crypto ticker symbols.
    /// </summary>
    private static readonly Regex TokenSymbolRegex = new(
        @"\b(BTC|ETH|SOL|DOGE|XRP|ADA|BNB|AVAX|MATIC|LINK|DOT|SHIB|PEPE|ARB|OP|SUI|APT|SEI|TIA|NEAR|ATOM|FTM|INJ|RUNE|AAVE|UNI|MKR|LDO|CRV|SNX)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Maps full cryptocurrency names to their ticker symbols.
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
    /// Regex for $SYMBOL patterns.
    /// </summary>
    private static readonly Regex DollarSymbolRegex = new(
        @"\$([A-Z]{2,6})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Regex for price targets: $50,000 / $100K / $1.5M / 50000 dollars / etc.
    /// </summary>
    private static readonly Regex PriceRegex = new(
        @"\$[\d,]+(?:\.\d+)?[KkMm]?|\b[\d,]+(?:\.\d+)?\s*(?:dollars|usd|USD|\$)|(?<=(?:price|hit|reach|above|below|target)\s{0,5})\b[\d,]+(?:\.\d+)?[KkMm]?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AppConfig _config;
    private readonly HttpClient _http;
    private readonly LogService _log;

    /// <summary>
    /// Initializes a new <see cref="ProbabilityModelService"/>.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="httpClient">Shared HTTP client for Binance API calls.</param>
    /// <param name="logService">Logging service.</param>
    public ProbabilityModelService(AppConfig config, HttpClient httpClient, LogService logService)
    {
        _config = config;
        _http = httpClient;
        _log = logService;
    }

    /// <summary>
    /// Calculates the estimated probability that the market's underlying crypto token
    /// will reach the target price before the deadline.
    /// Returns 0.5 (no edge) when the question cannot be parsed or price data is unavailable.
    /// </summary>
    /// <param name="market">The Polymarket market to evaluate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Probability in the range [0.01, 0.99].</returns>
    public async Task<double> CalculateProbabilityAsync(Market market, CancellationToken ct)
    {
        try
        {
            _log.Info($"Calculating probability for: {market.Question}");

            // Step 1–2: Parse token symbol and target price from question
            (string? symbol, double? targetPrice) = ParseMarketQuestion(market.Question);

            // Step 3: If we can't extract both, return default 0.5
            if (symbol is null || targetPrice is null)
            {
                _log.Warn($"Could not parse symbol/target from question. symbol={symbol}, target={targetPrice}. Returning 0.5");
                return 0.5;
            }

            _log.Info($"Parsed: symbol={symbol}, target=${targetPrice:N2}");

            // Step 4: Fetch real-time price from Binance
            BinanceTickerResponse? ticker = await FetchBinanceTickerAsync(symbol, ct);
            if (ticker is null)
            {
                _log.Warn($"Failed to fetch Binance ticker for {symbol}USDT. Returning 0.5");
                return 0.5;
            }

            double currentPrice = ParseDouble(ticker.LastPrice);
            double highPrice = ParseDouble(ticker.HighPrice);
            double lowPrice = ParseDouble(ticker.LowPrice);

            if (currentPrice <= 0)
            {
                _log.Warn($"Invalid current price ({currentPrice}) for {symbol}. Returning 0.5");
                return 0.5;
            }

            // Step 5: Calculate 24h volatility
            double volatility = (highPrice - lowPrice) / currentPrice;
            _log.Debug($"{symbol} price={currentPrice:N4}, high={highPrice:N4}, low={lowPrice:N4}, vol24h={volatility:P2}");

            // Step 6: Calculate hours left
            DateTime deadline = market.EndDate ?? DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1); // 23:59:59 UTC today
            double hoursLeft = Math.Max((deadline - DateTime.UtcNow).TotalHours, 0.1);
            _log.Debug($"Hours left until deadline: {hoursLeft:F2}");

            // Step 7: Calculate expected move & probability
            double dailyVol = volatility; // already 24h-based
            double hoursRatio = hoursLeft / 24.0;
            double scaledVol = dailyVol * Math.Sqrt(hoursRatio);

            if (scaledVol <= 0)
            {
                _log.Warn("Scaled volatility is zero. Returning 0.5");
                return 0.5;
            }

            double zScore = (targetPrice.Value - currentPrice) / (currentPrice * scaledVol);

            // Probability price goes above target = 1 - Φ(z)
            // If target is below current price, probability = Φ(z)
            double rawProbability = targetPrice.Value >= currentPrice
                ? 1.0 - MathHelper.NormCdf(zScore)
                : MathHelper.NormCdf(zScore);

            _log.Debug($"z={zScore:F4}, rawProb={rawProbability:P4}");

            // Step 8: Apply fade multiplier
            double adjustedProbability = rawProbability * _config.FadeMultiplier;
            _log.Debug($"After fade ({_config.FadeMultiplier}): {adjustedProbability:P4}");

            // Step 9: Clamp to [0.01, 0.99]
            double clamped = Math.Clamp(adjustedProbability, 0.01, 0.99);
            _log.Info($"Final probability for {symbol} → ${targetPrice:N2}: {clamped:P2}");

            return clamped;
        }
        catch (Exception ex)
        {
            _log.Error($"Probability calculation failed for: {market.Question}", ex);
            return 0.5;
        }
    }

    /// <summary>
    /// Parses a market question to extract the crypto token symbol and numeric target price.
    /// </summary>
    /// <param name="question">Market question text.</param>
    /// <returns>A tuple of (symbol, targetPrice), either of which may be null.</returns>
    public (string? Symbol, double? TargetPrice) ParseMarketQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return (null, null);

        string? symbol = ExtractSymbol(question);
        double? targetPrice = ExtractPrice(question);

        return (symbol, targetPrice);
    }

    /// <summary>
    /// Extracts a crypto token symbol from question text,
    /// checking explicit tickers, $SYMBOL patterns, and full names.
    /// </summary>
    private static string? ExtractSymbol(string question)
    {
        // 1. Direct ticker match
        Match tickerMatch = TokenSymbolRegex.Match(question);
        if (tickerMatch.Success)
            return tickerMatch.Groups[1].Value.ToUpperInvariant();

        // 2. $SYMBOL pattern
        Match dollarMatch = DollarSymbolRegex.Match(question);
        if (dollarMatch.Success)
            return dollarMatch.Groups[1].Value.ToUpperInvariant();

        // 3. Full name → symbol
        foreach (KeyValuePair<string, string> kvp in NameToSymbol)
        {
            if (question.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }

    /// <summary>
    /// Extracts a target price from question text, handling $, commas, and K/M suffixes.
    /// </summary>
    private static double? ExtractPrice(string question)
    {
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
    /// Fetches the 24-hour ticker for a given symbol from the Binance API.
    /// Returns <c>null</c> on failure.
    /// </summary>
    private async Task<BinanceTickerResponse?> FetchBinanceTickerAsync(string symbol, CancellationToken ct)
    {
        string url = $"{_config.BinanceApiBaseUrl}/api/v3/ticker/24hr?symbol={symbol}USDT";

        try
        {
            _log.Debug($"Fetching Binance ticker: {url}");
            using HttpResponseMessage response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                _log.Warn($"Binance API returned {(int)response.StatusCode} for {symbol}USDT: {body}");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<BinanceTickerResponse>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _log.Error($"Binance API call failed for {symbol}USDT", ex);
            return null;
        }
    }

    /// <summary>
    /// Safely parses a numeric string to double, returning 0 on failure.
    /// </summary>
    private static double ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double result)
            ? result
            : 0;
    }
}
