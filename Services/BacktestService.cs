using System.Text.Json;
using System.Text.Json.Serialization;
using PolyEdgeScout.Models;
using Spectre.Console;

namespace PolyEdgeScout.Services;

/// <summary>
/// Backtesting service that evaluates the probability model against historically
/// resolved Polymarket markets to measure prediction accuracy and hypothetical P&amp;L.
/// </summary>
public sealed class BacktestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Case-insensitive keywords used to identify crypto micro-market questions.
    /// Mirrors the filter logic in <see cref="ScannerService"/>.
    /// </summary>
    private static readonly string[] CryptoKeywords =
    [
        "hit", "reach", "milestone", "eod", "today", "price",
        "above", "below", "by", "$",
        "bitcoin", "btc", "eth", "sol", "doge", "xrp", "ada",
        "bnb", "avax", "matic", "link", "dot",
    ];

    private readonly AppConfig _config;
    private readonly HttpClient _http;
    private readonly ProbabilityModelService _probModel;
    private readonly LogService _log;

    /// <summary>
    /// Initializes a new <see cref="BacktestService"/>.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="httpClient">Shared HTTP client for Gamma API calls.</param>
    /// <param name="probModel">Probability model service for generating predictions.</param>
    /// <param name="log">Logging service.</param>
    public BacktestService(AppConfig config, HttpClient httpClient, ProbabilityModelService probModel, LogService log)
    {
        _config = config;
        _http = httpClient;
        _probModel = probModel;
        _log = log;
    }

    /// <summary>
    /// Runs a full backtest against recently resolved Polymarket markets.
    /// Fetches up to 100 resolved markets, filters for crypto micro-markets,
    /// evaluates the model, and displays calibration and P&amp;L metrics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task RunBacktestAsync(CancellationToken ct)
    {
        _log.Info("═══ BACKTEST STARTING ═══");

        List<ResolvedMarketData> dataPoints;
        try
        {
            dataPoints = await CollectResolvedMarketsAsync(ct);
        }
        catch (Exception ex)
        {
            _log.Error("Backtest failed — could not fetch resolved markets.", ex);
            return;
        }

        if (dataPoints.Count == 0)
        {
            _log.Warn("Backtest: no qualifying resolved markets found.");
            return;
        }

        _log.Info($"Backtest: evaluating {dataPoints.Count} resolved markets.");

        // Run the model against each resolved market
        var results = new List<BacktestEntry>();

        foreach (ResolvedMarketData data in dataPoints)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                double modelProb = await _probModel.CalculateProbabilityAsync(data.Market, ct);
                double actual = data.ResolvedYes ? 1.0 : 0.0;

                results.Add(new BacktestEntry
                {
                    Question = data.Market.Question,
                    ModelProbability = modelProb,
                    ActualOutcome = actual,
                    MarketYesPrice = data.Market.YesPrice,
                });

                _log.Debug(
                    $"BT: P={modelProb:F3} Actual={actual:F0} Mkt={data.Market.YesPrice:F3} | " +
                    $"{Truncate(data.Market.Question)}");
            }
            catch (Exception ex)
            {
                _log.Warn($"Backtest: skipping market due to error — {Truncate(data.Market.Question)}: {ex.Message}");
            }
        }

        if (results.Count == 0)
        {
            _log.Warn("Backtest: no results after model evaluation.");
            return;
        }

        DisplayResults(results);
        _log.Info("═══ BACKTEST COMPLETE ═══");
    }

    // ──────────────────────────────────────────────────────────────
    //  Data collection
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches recently resolved markets from the Gamma API and filters for
    /// crypto micro-markets that have a definitive outcome.
    /// </summary>
    private async Task<List<ResolvedMarketData>> CollectResolvedMarketsAsync(CancellationToken ct)
    {
        string url = $"{_config.GammaApiBaseUrl}/markets" +
                     "?active=false&closed=true" +
                     "&order=end_date_iso&ascending=false&limit=100";

        _log.Info($"Backtest: fetching resolved markets from {url}");

        string json = await FetchWithRetryAsync(url, ct);

        List<ResolvedGammaMarketResponse>? responses =
            JsonSerializer.Deserialize<List<ResolvedGammaMarketResponse>>(json, JsonOptions);

        if (responses is null || responses.Count == 0)
        {
            _log.Warn("Backtest: no resolved markets returned from Gamma API.");
            return [];
        }

        _log.Info($"Backtest: fetched {responses.Count} raw resolved markets.");

        var dataPoints = new List<ResolvedMarketData>();

        foreach (ResolvedGammaMarketResponse response in responses)
        {
            if (dataPoints.Count >= 30)
                break;

            // Must have a resolved outcome
            if (string.IsNullOrWhiteSpace(response.ResolutionSource) &&
                string.IsNullOrWhiteSpace(response.Outcome))
            {
                continue;
            }

            // Determine resolution: YES or NO
            bool? resolvedYes = DetermineResolution(response);
            if (resolvedYes is null)
            {
                _log.Debug($"Backtest: skipping (ambiguous resolution): {Truncate(response.Question ?? "")}");
                continue;
            }

            // Must be crypto-related
            if (string.IsNullOrWhiteSpace(response.Question) || !IsCryptoMicro(response.Question))
            {
                continue;
            }

            Market market = BuildMarketFromResolved(response);
            dataPoints.Add(new ResolvedMarketData(market, resolvedYes.Value));

            _log.Debug($"Backtest: included — resolved={resolvedYes.Value} | {Truncate(response.Question ?? "")}");
        }

        _log.Info($"Backtest: {dataPoints.Count} crypto markets with clear resolution.");
        return dataPoints;
    }

    /// <summary>
    /// Determines the YES/NO resolution from a resolved Gamma market response.
    /// Returns <c>null</c> if the resolution is ambiguous or missing.
    /// </summary>
    private static bool? DetermineResolution(ResolvedGammaMarketResponse response)
    {
        // Check explicit outcome field
        if (!string.IsNullOrWhiteSpace(response.Outcome))
        {
            if (response.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (response.Outcome.Equals("No", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Infer from token prices — if the YES token settled at ~1.0 it resolved YES
        GammaToken? yesToken = response.Tokens?
            .FirstOrDefault(t => t.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase));

        if (yesToken is not null)
        {
            if (yesToken.Price >= 0.95)
                return true;
            if (yesToken.Price <= 0.05)
                return false;
        }

        return null;
    }

    /// <summary>
    /// Constructs a <see cref="Market"/> domain model from a resolved Gamma API response.
    /// Reuses the same conversion logic as <see cref="Market.FromGammaResponse"/>.
    /// </summary>
    private static Market BuildMarketFromResolved(ResolvedGammaMarketResponse response)
    {
        GammaToken? yesToken = response.Tokens?
            .FirstOrDefault(t => t.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase));
        GammaToken? noToken = response.Tokens?
            .FirstOrDefault(t => t.Outcome.Equals("No", StringComparison.OrdinalIgnoreCase));

        double yesPrice = yesToken?.Price ?? 0;
        double noPrice = noToken?.Price ?? 0;

        // Fallback: parse outcomePrices
        if (yesPrice == 0 && noPrice == 0 && !string.IsNullOrEmpty(response.OutcomePrices))
        {
            try
            {
                var prices = JsonSerializer.Deserialize<List<string>>(response.OutcomePrices);
                if (prices is { Count: >= 2 })
                {
                    _ = double.TryParse(prices[0], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out yesPrice);
                    _ = double.TryParse(prices[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out noPrice);
                }
            }
            catch
            {
                // Ignore parse errors for fallback
            }
        }

        string tokenId = yesToken?.TokenId ?? response.Tokens?.FirstOrDefault()?.TokenId ?? "";

        DateTime? endDate = DateTime.TryParse(response.EndDateIso,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var ed)
            ? ed.ToUniversalTime()
            : null;

        DateTime createdAt = DateTime.TryParse(response.CreatedAt,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var ca)
            ? ca.ToUniversalTime()
            : DateTime.UtcNow;

        return new Market
        {
            ConditionId = response.ConditionId ?? "",
            QuestionId = response.QuestionId ?? "",
            TokenId = tokenId,
            Question = response.Question ?? "",
            YesPrice = yesPrice,
            NoPrice = noPrice,
            Volume = response.VolumeNum,
            CreatedAt = createdAt,
            EndDate = endDate,
            MarketSlug = response.MarketSlug ?? "",
            Active = false,
            Closed = true,
        };
    }

    // ──────────────────────────────────────────────────────────────
    //  Metrics & display
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates all backtest metrics and renders them as formatted Spectre.Console tables.
    /// </summary>
    private void DisplayResults(List<BacktestEntry> results)
    {
        // ── Summary metrics ──
        double brierScore = results.Average(r =>
            Math.Pow(r.ModelProbability - r.ActualOutcome, 2));

        int correctCalls = results.Count(r =>
            (r.ModelProbability > 0.5 && r.ActualOutcome == 1.0) ||
            (r.ModelProbability < 0.5 && r.ActualOutcome == 0.0));
        double winRate = (double)correctCalls / results.Count;

        // Edge accuracy: markets where edge > MinEdge
        var edgeMarkets = results
            .Where(r => (r.ModelProbability - r.MarketYesPrice) > _config.MinEdge)
            .ToList();

        int edgeCorrect = edgeMarkets.Count(r =>
            (r.ModelProbability > 0.5 && r.ActualOutcome == 1.0) ||
            (r.ModelProbability < 0.5 && r.ActualOutcome == 0.0));
        double edgeAccuracy = edgeMarkets.Count > 0
            ? (double)edgeCorrect / edgeMarkets.Count
            : 0;

        // Hypothetical P&L: flat $100 bet on every market with sufficient edge
        double hypotheticalPnl = 0;
        foreach (BacktestEntry entry in edgeMarkets)
        {
            const double betSize = 100.0;
            double shares = betSize / entry.MarketYesPrice;
            bool won = entry.ActualOutcome == 1.0;
            double gross = won
                ? (shares * 1.0) - (shares * entry.MarketYesPrice)
                : -(shares * entry.MarketYesPrice);
            double fees = betSize * 0.02;
            hypotheticalPnl += gross - fees;
        }

        // ── Summary table ──
        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold yellow]BACKTEST RESULTS[/]")
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]")
            .AddColumn("[bold]Notes[/]");

        summaryTable.AddRow("Markets Evaluated", results.Count.ToString(), "Resolved crypto micro-markets");
        summaryTable.AddRow(
            "Brier Score",
            $"{brierScore:F4}",
            brierScore < 0.20 ? "[green]Good (< 0.20)[/]" : brierScore < 0.25 ? "[yellow]Fair[/]" : "[red]Poor (random=0.25)[/]");
        summaryTable.AddRow("Win Rate", $"{winRate:P1}", $"{correctCalls}/{results.Count} correct direction");
        summaryTable.AddRow(
            "Edge Accuracy",
            edgeMarkets.Count > 0 ? $"{edgeAccuracy:P1}" : "N/A",
            $"{edgeCorrect}/{edgeMarkets.Count} with edge > {_config.MinEdge:P0}");
        summaryTable.AddRow(
            "Hypothetical P&L",
            hypotheticalPnl >= 0 ? $"[green]+${hypotheticalPnl:F2}[/]" : $"[red]-${Math.Abs(hypotheticalPnl):F2}[/]",
            $"$100 flat bet × {edgeMarkets.Count} trades");

        AnsiConsole.Write(summaryTable);

        _log.Info($"Brier={brierScore:F4} WinRate={winRate:P1} EdgeAcc={edgeAccuracy:P1} HypPnL=${hypotheticalPnl:F2}");

        // ── Calibration table ──
        DisplayCalibrationTable(results);

        // ── Per-market detail table (top 15) ──
        DisplayDetailTable(results);
    }

    /// <summary>
    /// Groups predictions into probability buckets and compares predicted vs actual outcome rates.
    /// </summary>
    private void DisplayCalibrationTable(List<BacktestEntry> results)
    {
        var calibrationTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]CALIBRATION[/]")
            .AddColumn("[bold]Bucket[/]")
            .AddColumn("[bold]Count[/]")
            .AddColumn("[bold]Avg Predicted[/]")
            .AddColumn("[bold]Avg Actual[/]")
            .AddColumn("[bold]Δ[/]");

        (string Label, double Low, double High)[] buckets =
        [
            ("0–20%",  0.00, 0.20),
            ("20–40%", 0.20, 0.40),
            ("40–60%", 0.40, 0.60),
            ("60–80%", 0.60, 0.80),
            ("80–100%", 0.80, 1.01), // 1.01 to include 1.0
        ];

        foreach (var (label, low, high) in buckets)
        {
            var bucket = results.Where(r => r.ModelProbability >= low && r.ModelProbability < high).ToList();

            if (bucket.Count == 0)
            {
                calibrationTable.AddRow(label, "0", "—", "—", "—");
                continue;
            }

            double avgPredicted = bucket.Average(r => r.ModelProbability);
            double avgActual = bucket.Average(r => r.ActualOutcome);
            double delta = avgPredicted - avgActual;

            string deltaColor = Math.Abs(delta) < 0.10 ? "green" : Math.Abs(delta) < 0.20 ? "yellow" : "red";

            calibrationTable.AddRow(
                label,
                bucket.Count.ToString(),
                $"{avgPredicted:P1}",
                $"{avgActual:P1}",
                $"[{deltaColor}]{delta:+0.00;-0.00}[/]");

            _log.Debug($"Calibration {label}: n={bucket.Count} pred={avgPredicted:P1} actual={avgActual:P1} Δ={delta:+0.00;-0.00}");
        }

        AnsiConsole.Write(calibrationTable);
    }

    /// <summary>
    /// Displays per-market details in a formatted table (capped at 15 entries for readability).
    /// </summary>
    private void DisplayDetailTable(List<BacktestEntry> results)
    {
        var detailTable = new Table()
            .Border(TableBorder.Simple)
            .Title("[bold]MARKET DETAILS (top 15)[/]")
            .AddColumn("[bold]Market[/]")
            .AddColumn("[bold]Model P[/]")
            .AddColumn("[bold]Mkt Price[/]")
            .AddColumn("[bold]Edge[/]")
            .AddColumn("[bold]Actual[/]")
            .AddColumn("[bold]Hit?[/]");

        foreach (BacktestEntry entry in results.Take(15))
        {
            double edge = entry.ModelProbability - entry.MarketYesPrice;
            bool modelCorrect =
                (entry.ModelProbability > 0.5 && entry.ActualOutcome == 1.0) ||
                (entry.ModelProbability < 0.5 && entry.ActualOutcome == 0.0);

            detailTable.AddRow(
                Markup.Escape(Truncate(entry.Question, 50)),
                $"{entry.ModelProbability:P1}",
                $"{entry.MarketYesPrice:F3}",
                edge > 0 ? $"[green]+{edge:P1}[/]" : $"[red]{edge:P1}[/]",
                entry.ActualOutcome == 1.0 ? "[green]YES[/]" : "[red]NO[/]",
                modelCorrect ? "[green]✓[/]" : "[red]✗[/]");
        }

        AnsiConsole.Write(detailTable);
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

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
                    response.EnsureSuccessStatusCode();
                }

                _log.Warn($"429 Too Many Requests — retrying in {delayMs}ms (attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

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

    // ──────────────────────────────────────────────────────────────
    //  Internal DTOs
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Extended Gamma API response including resolution fields for closed markets.
    /// </summary>
    private record ResolvedGammaMarketResponse
    {
        [JsonPropertyName("condition_id")]
        public string? ConditionId { get; init; }

        [JsonPropertyName("question_id")]
        public string? QuestionId { get; init; }

        [JsonPropertyName("tokens")]
        public List<GammaToken>? Tokens { get; init; }

        [JsonPropertyName("question")]
        public string? Question { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("market_slug")]
        public string? MarketSlug { get; init; }

        [JsonPropertyName("end_date_iso")]
        public string? EndDateIso { get; init; }

        [JsonPropertyName("active")]
        public bool Active { get; init; }

        [JsonPropertyName("closed")]
        public bool Closed { get; init; }

        [JsonPropertyName("volume")]
        public string? Volume { get; init; }

        [JsonPropertyName("volume_num")]
        public double VolumeNum { get; init; }

        [JsonPropertyName("outcomePrices")]
        public string? OutcomePrices { get; init; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        [JsonPropertyName("outcome")]
        public string? Outcome { get; init; }

        [JsonPropertyName("resolution_source")]
        public string? ResolutionSource { get; init; }

        [JsonPropertyName("resolved_by")]
        public string? ResolvedBy { get; init; }
    }

    /// <summary>
    /// Pairs a market with its resolved outcome for backtest evaluation.
    /// </summary>
    private sealed record ResolvedMarketData(Market Market, bool ResolvedYes);

    /// <summary>
    /// A single backtest result entry pairing model prediction with actual outcome.
    /// </summary>
    private sealed record BacktestEntry
    {
        public required string Question { get; init; }
        public required double ModelProbability { get; init; }
        public required double ActualOutcome { get; init; }
        public required double MarketYesPrice { get; init; }
    }
}
