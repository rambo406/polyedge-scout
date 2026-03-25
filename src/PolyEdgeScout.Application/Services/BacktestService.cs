namespace PolyEdgeScout.Application.Services;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;
using PolyEdgeScout.Domain.ValueObjects;

/// <summary>
/// Application-level backtesting service.
/// Evaluates the probability model against historically resolved Polymarket markets
/// to measure prediction accuracy and hypothetical P&amp;L.
/// Does NOT reference Spectre.Console — all display logic is in the Console layer.
/// </summary>
public sealed class BacktestService : IBacktestService
{
    private static readonly IEdgeFormula EdgeFormula = new DefaultScaledEdgeFormula();

    private readonly AppConfig _config;
    private readonly IGammaApiClient _gammaClient;
    private readonly IProbabilityModelService _probModel;
    private readonly ILogService _log;

    public BacktestService(
        AppConfig config,
        IGammaApiClient gammaClient,
        IProbabilityModelService probModel,
        ILogService log)
    {
        _config = config;
        _gammaClient = gammaClient;
        _probModel = probModel;
        _log = log;
    }

    public async Task<BacktestResult> RunBacktestAsync(CancellationToken ct = default)
    {
        _log.Info("═══ BACKTEST STARTING ═══");

        var resolvedMarkets = await _gammaClient.FetchResolvedMarketsAsync(100, ct);

        if (resolvedMarkets.Count == 0)
        {
            _log.Warn("Backtest: no resolved markets returned from Gamma API.");
            return EmptyResult();
        }

        _log.Info($"Backtest: fetched {resolvedMarkets.Count} raw resolved markets.");

        // Filter for crypto micro markets with clear resolution
        var dataPoints = new List<(Domain.Entities.Market Market, bool ResolvedYes)>();

        foreach (var response in resolvedMarkets)
        {
            if (dataPoints.Count >= 30)
                break;

            // Must have a resolved outcome
            if (string.IsNullOrWhiteSpace(response.ResolutionSource) &&
                string.IsNullOrWhiteSpace(response.Outcome))
            {
                continue;
            }

            bool? resolvedYes = MarketMapper.DetermineResolution(response);
            if (resolvedYes is null)
            {
                _log.Debug($"Backtest: skipping (ambiguous resolution): {Truncate(response.Question)}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(response.Question) ||
                !MarketClassifier.IsCryptoMicro(
                    response.Question,
                    _config.MarketFilter.IncludeKeywords,
                    _config.MarketFilter.ExcludeKeywords))
            {
                continue;
            }

            var market = MarketMapper.ToDomain(response);
            dataPoints.Add((market, resolvedYes.Value));

            _log.Debug($"Backtest: included — resolved={resolvedYes.Value} | {Truncate(response.Question)}");
        }

        if (dataPoints.Count == 0)
        {
            _log.Warn("Backtest: no qualifying resolved markets found.");
            return EmptyResult();
        }

        _log.Info($"Backtest: evaluating {dataPoints.Count} resolved markets.");

        // Run the model against each resolved market
        var entries = new List<BacktestEntry>();

        foreach (var (market, resolvedYes) in dataPoints)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var modelProb = await _probModel.CalculateProbabilityAsync(market, ct);
                if (modelProb is null)
                {
                    _log.Debug($"Backtest: skipping unparseable market — {Truncate(market.Question)}");
                    continue;
                }

                var edgeCalc = EdgeCalculation.Create(
                    EdgeFormula,
                    modelProb.ModelProbability,
                    market.YesPrice,
                    modelProb.TargetPrice,
                    modelProb.CurrentAssetPrice);

                double actual = resolvedYes ? 1.0 : 0.0;
                bool modelCorrect =
                    (modelProb.ModelProbability > 0.5 && actual == 1.0) ||
                    (modelProb.ModelProbability < 0.5 && actual == 0.0);

                entries.Add(new BacktestEntry
                {
                    MarketQuestion = market.Question,
                    ModelProbability = modelProb.ModelProbability,
                    MarketYesPrice = market.YesPrice,
                    Edge = edgeCalc.Edge,
                    ActualOutcome = actual,
                    ModelCorrect = modelCorrect,
                });

                _log.Debug(
                    $"BT: P={modelProb.ModelProbability:F3} Actual={actual:F0} Mkt={market.YesPrice:F3} | " +
                    $"{Truncate(market.Question)}");
            }
            catch (Exception ex)
            {
                _log.Warn($"Backtest: skipping market due to error — {Truncate(market.Question)}: {ex.Message}");
            }
        }

        if (entries.Count == 0)
        {
            _log.Warn("Backtest: no results after model evaluation.");
            return EmptyResult();
        }

        var result = ComputeMetrics(entries);
        _log.Info($"Brier={result.BrierScore:F4} WinRate={result.WinRate:P1} " +
                 $"EdgeAcc={result.EdgeAccuracy:P1} HypPnL=${result.HypotheticalPnl:F2}");
        _log.Info("═══ BACKTEST COMPLETE ═══");

        return result;
    }

    // ──────────────────────────────────────────────────────────────
    //  Metrics computation
    // ──────────────────────────────────────────────────────────────

    private BacktestResult ComputeMetrics(List<BacktestEntry> entries)
    {
        // Brier score
        double brierScore = entries.Average(r =>
            Math.Pow(r.ModelProbability - r.ActualOutcome, 2));

        // Win rate
        int correctCalls = entries.Count(r => r.ModelCorrect);
        double winRate = (double)correctCalls / entries.Count;

        // Edge accuracy: markets where edge > MinEdge
        var edgeMarkets = entries
            .Where(r => r.Edge > _config.MinEdge)
            .ToList();

        int edgeCorrect = edgeMarkets.Count(r => r.ModelCorrect);
        double edgeAccuracy = edgeMarkets.Count > 0
            ? (double)edgeCorrect / edgeMarkets.Count
            : 0;

        // Hypothetical P&L: flat $100 bet on every market with sufficient edge
        double hypotheticalPnl = 0;
        foreach (var entry in edgeMarkets)
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

        // Calibration buckets
        var calibrationBuckets = ComputeCalibrationBuckets(entries);

        return new BacktestResult
        {
            BrierScore = brierScore,
            WinRate = winRate,
            EdgeAccuracy = edgeAccuracy,
            HypotheticalPnl = hypotheticalPnl,
            TotalMarkets = entries.Count,
            MarketsWithEdge = edgeMarkets.Count,
            CalibrationBuckets = calibrationBuckets,
            Entries = entries,
        };
    }

    private static List<CalibrationBucket> ComputeCalibrationBuckets(List<BacktestEntry> entries)
    {
        (string Label, double Low, double High)[] ranges =
        [
            ("0–20%",   0.00, 0.20),
            ("20–40%",  0.20, 0.40),
            ("40–60%",  0.40, 0.60),
            ("60–80%",  0.60, 0.80),
            ("80–100%", 0.80, 1.01),
        ];

        var buckets = new List<CalibrationBucket>();

        foreach (var (label, low, high) in ranges)
        {
            var bucket = entries
                .Where(r => r.ModelProbability >= low && r.ModelProbability < high)
                .ToList();

            if (bucket.Count == 0)
            {
                buckets.Add(new CalibrationBucket { Range = label, Count = 0 });
                continue;
            }

            buckets.Add(new CalibrationBucket
            {
                Range = label,
                AveragePredicted = bucket.Average(r => r.ModelProbability),
                AverageActual = bucket.Average(r => r.ActualOutcome),
                Count = bucket.Count,
            });
        }

        return buckets;
    }

    private static BacktestResult EmptyResult() => new()
    {
        BrierScore = 0,
        WinRate = 0,
        EdgeAccuracy = 0,
        HypotheticalPnl = 0,
        TotalMarkets = 0,
        MarketsWithEdge = 0,
    };

    /// <summary>
    /// Truncates a string to a maximum length for log readability.
    /// </summary>
    private static string Truncate(string text, int max = 80) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");
}
