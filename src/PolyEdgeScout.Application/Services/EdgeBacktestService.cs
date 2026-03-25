namespace PolyEdgeScout.Application.Services;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;
using PolyEdgeScout.Domain.ValueObjects;

/// <summary>
/// Runs edge-focused backtests against resolved markets using all registered edge formulas.
/// Evaluates each market×formula combination, computes hypothetical P&amp;L, and streams
/// incremental progress events for live UI updates.
/// </summary>
public sealed class EdgeBacktestService : IEdgeBacktestService
{
    private readonly AppConfig _config;
    private readonly IGammaApiClient _gammaClient;
    private readonly IProbabilityModelService _probModel;
    private readonly ILogService _log;
    private readonly IReadOnlyList<IEdgeFormula> _formulas;

    /// <inheritdoc />
    public event EventHandler<EdgeBacktestProgressEvent>? OnProgress;

    public EdgeBacktestService(
        AppConfig config,
        IGammaApiClient gammaClient,
        IProbabilityModelService probModel,
        ILogService log,
        IEnumerable<IEdgeFormula> formulas)
    {
        _config = config;
        _gammaClient = gammaClient;
        _probModel = probModel;
        _log = log;
        _formulas = formulas.ToList();
    }

    /// <inheritdoc />
    public async Task<EdgeBacktestResult> RunEdgeBacktestAsync(
        List<string> symbols, string leftTimeframe, string rightTimeframe, CancellationToken ct = default)
    {
        _log.Info("═══ EDGE BACKTEST STARTING ═══");
        _log.Info($"Symbols: {string.Join(", ", symbols)} | Left: {leftTimeframe} | Right: {rightTimeframe}");

        var resolvedMarkets = await _gammaClient.FetchResolvedMarketsAsync(200, ct);

        if (resolvedMarkets.Count == 0)
        {
            _log.Warn("Edge backtest: no resolved markets returned from Gamma API.");
            return EmptyResult(symbols);
        }

        _log.Info($"Edge backtest: fetched {resolvedMarkets.Count} raw resolved markets.");

        // Filter for crypto markets matching the selected symbols
        var qualifiedMarkets = FilterBySymbols(resolvedMarkets, symbols);

        if (qualifiedMarkets.Count == 0)
        {
            _log.Warn("Edge backtest: no markets matched the selected symbols.");
            return EmptyResult(symbols);
        }

        _log.Info($"Edge backtest: {qualifiedMarkets.Count} markets matched selected symbols.");

        // Run both timeframes concurrently
        var leftTask = RunTimeframeBacktestAsync(qualifiedMarkets, leftTimeframe, ct);
        var rightTask = RunTimeframeBacktestAsync(qualifiedMarkets, rightTimeframe, ct);

        await Task.WhenAll(leftTask, rightTask);

        var leftResult = leftTask.Result;
        var rightResult = rightTask.Result;

        _log.Info($"Left [{leftTimeframe}]: {leftResult.TotalMarkets} markets, PnL=${leftResult.GrandTotalPnl:F2}");
        _log.Info($"Right [{rightTimeframe}]: {rightResult.TotalMarkets} markets, PnL=${rightResult.GrandTotalPnl:F2}");
        _log.Info("═══ EDGE BACKTEST COMPLETE ═══");

        return new EdgeBacktestResult
        {
            LeftTimeframeResult = leftResult,
            RightTimeframeResult = rightResult,
            SelectedSymbols = symbols,
            FormulaNames = _formulas.Select(f => f.Name).ToList(),
        };
    }

    // ──────────────────────────────────────────────────────────────
    //  Timeframe-level backtest
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the backtest for a single timeframe across all markets and formulas.
    /// </summary>
    private async Task<EdgeBacktestTimeframeResult> RunTimeframeBacktestAsync(
        List<(GammaMarketResponse Response, string Symbol, bool ResolvedYes)> markets,
        string timeframe,
        CancellationToken ct)
    {
        var allEntries = new List<EdgeBacktestEntry>();
        int evaluatedCount = 0;
        int totalCount = markets.Count * _formulas.Count;

        foreach (var (response, symbol, resolvedYes) in markets)
        {
            if (ct.IsCancellationRequested)
            {
                _log.Warn($"Edge backtest [{timeframe}]: cancelled — returning partial results.");
                break;
            }

            try
            {
                var market = MarketMapper.ToDomain(response);
                var modelEval = await _probModel.CalculateProbabilityAsync(market, ct);

                if (modelEval is null)
                {
                    _log.Debug($"Edge backtest [{timeframe}]: skipping unparseable — {Truncate(market.Question)}");
                    evaluatedCount += _formulas.Count;
                    continue;
                }

                double actual = resolvedYes ? 1.0 : 0.0;

                foreach (var formula in _formulas)
                {
                    var edgeCalc = EdgeCalculation.Create(
                        formula,
                        modelEval.ModelProbability,
                        market.YesPrice,
                        modelEval.TargetPrice,
                        modelEval.CurrentAssetPrice);

                    bool modelCorrect =
                        (modelEval.ModelProbability > 0.5 && actual == 1.0) ||
                        (modelEval.ModelProbability < 0.5 && actual == 0.0);

                    double pnl = CalculatePnL(edgeCalc, market.YesPrice, actual);

                    var entry = new EdgeBacktestEntry
                    {
                        MarketQuestion = market.Question,
                        ModelProbability = modelEval.ModelProbability,
                        MarketYesPrice = market.YesPrice,
                        Edge = edgeCalc.Edge,
                        ActualOutcome = actual,
                        ModelCorrect = modelCorrect,
                        Timeframe = timeframe,
                        FormulaName = formula.Name,
                        Symbol = symbol,
                        PnL = pnl,
                    };

                    allEntries.Add(entry);
                    evaluatedCount++;

                    // Emit progress after each formula evaluation
                    var runningMetrics = AggregateTimeframeResult(allEntries, timeframe);

                    OnProgress?.Invoke(this, new EdgeBacktestProgressEvent
                    {
                        Entry = entry,
                        FormulaName = formula.Name,
                        Timeframe = timeframe,
                        EvaluatedCount = evaluatedCount,
                        TotalCount = totalCount,
                        RunningMetrics = runningMetrics,
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Edge backtest [{timeframe}]: skipping market due to error — " +
                          $"{Truncate(response.Question)}: {ex.Message}");
                evaluatedCount += _formulas.Count;
            }
        }

        return AggregateTimeframeResult(allEntries, timeframe);
    }

    // ──────────────────────────────────────────────────────────────
    //  Symbol filtering
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Filters resolved markets to those containing a selected symbol in the question text.
    /// Also determines the resolution outcome for each market.
    /// </summary>
    private List<(GammaMarketResponse Response, string Symbol, bool ResolvedYes)> FilterBySymbols(
        List<GammaMarketResponse> markets, List<string> symbols)
    {
        var result = new List<(GammaMarketResponse, string, bool)>();

        foreach (var response in markets)
        {
            // Must have a resolved outcome
            if (string.IsNullOrWhiteSpace(response.ResolutionSource) &&
                string.IsNullOrWhiteSpace(response.Outcome))
            {
                continue;
            }

            bool? resolvedYes = MarketMapper.DetermineResolution(response);
            if (resolvedYes is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(response.Question))
            {
                continue;
            }

            // Match symbol in question text (case-insensitive)
            string matchedSymbol = FindMatchingSymbol(response.Question, symbols);
            if (string.IsNullOrEmpty(matchedSymbol))
            {
                continue;
            }

            result.Add((response, matchedSymbol, resolvedYes.Value));
        }

        return result;
    }

    /// <summary>
    /// Finds the first symbol from the list that appears in the question text (case-insensitive).
    /// </summary>
    private static string FindMatchingSymbol(string question, List<string> symbols)
    {
        var lowerQuestion = question.ToLowerInvariant();

        foreach (var symbol in symbols)
        {
            if (lowerQuestion.Contains(symbol.ToLowerInvariant(), StringComparison.Ordinal))
            {
                return symbol;
            }
        }

        return "";
    }

    // ──────────────────────────────────────────────────────────────
    //  PnL calculation
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates hypothetical P&amp;L for a single entry.
    /// Uses a flat $100 bet when edge exceeds the configured minimum.
    /// </summary>
    private double CalculatePnL(EdgeCalculation edgeCalc, double marketYesPrice, double actual)
    {
        if (!edgeCalc.HasSufficientEdge(_config.MinEdge))
        {
            return 0.0;
        }

        if (marketYesPrice <= 0)
        {
            return 0.0;
        }

        const double betSize = 100.0;
        double shares = betSize / marketYesPrice;
        bool won = actual == 1.0;
        double gross = won
            ? shares - betSize
            : -betSize;
        double fees = betSize * 0.02;

        return gross - fees;
    }

    // ──────────────────────────────────────────────────────────────
    //  Metrics aggregation
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggregates all entries into per-formula and per-symbol results for a timeframe.
    /// </summary>
    private static EdgeBacktestTimeframeResult AggregateTimeframeResult(
        List<EdgeBacktestEntry> entries, string timeframe)
    {
        if (entries.Count == 0)
        {
            return new EdgeBacktestTimeframeResult { Timeframe = timeframe };
        }

        var formulaResults = entries
            .GroupBy(e => e.FormulaName)
            .Select(g => AggregateFormulaResult(g.Key, g.ToList()))
            .ToList();

        var symbolResults = entries
            .GroupBy(e => e.Symbol)
            .Select(g => AggregateSymbolResult(g.Key, g.ToList()))
            .ToList();

        double grandTotalPnl = entries.Sum(e => e.PnL);
        int marketsWithEdge = entries.Count(e => e.Edge > 0 && e.PnL != 0.0);
        double grandTotalRoi = marketsWithEdge > 0
            ? grandTotalPnl / (marketsWithEdge * 100.0)
            : 0.0;

        return new EdgeBacktestTimeframeResult
        {
            Timeframe = timeframe,
            FormulaResults = formulaResults,
            SymbolResults = symbolResults,
            TotalMarkets = entries.Count,
            GrandTotalPnl = grandTotalPnl,
            GrandTotalRoi = grandTotalRoi,
        };
    }

    /// <summary>
    /// Aggregates entries for a single formula into a <see cref="EdgeFormulaResult"/>.
    /// </summary>
    private static EdgeFormulaResult AggregateFormulaResult(string formulaName, List<EdgeBacktestEntry> entries)
    {
        int correct = entries.Count(e => e.ModelCorrect);
        double winRate = entries.Count > 0 ? (double)correct / entries.Count : 0;

        var edgeEntries = entries.Where(e => e.PnL != 0.0).ToList();
        int edgeCorrect = edgeEntries.Count(e => e.ModelCorrect);
        double edgeAccuracy = edgeEntries.Count > 0
            ? (double)edgeCorrect / edgeEntries.Count
            : 0;

        double totalPnl = entries.Sum(e => e.PnL);
        int marketsWithEdge = edgeEntries.Count;
        double roi = marketsWithEdge > 0
            ? totalPnl / (marketsWithEdge * 100.0)
            : 0;

        return new EdgeFormulaResult
        {
            FormulaName = formulaName,
            WinRate = winRate,
            EdgeAccuracy = edgeAccuracy,
            HypotheticalPnl = totalPnl,
            Roi = roi,
            TotalMarkets = entries.Count,
            MarketsWithEdge = marketsWithEdge,
            Entries = entries,
        };
    }

    /// <summary>
    /// Aggregates entries for a single symbol into a <see cref="EdgeSymbolResult"/>.
    /// </summary>
    private static EdgeSymbolResult AggregateSymbolResult(string symbol, List<EdgeBacktestEntry> entries)
    {
        int correct = entries.Count(e => e.ModelCorrect);
        double winRate = entries.Count > 0 ? (double)correct / entries.Count : 0;

        double totalPnl = entries.Sum(e => e.PnL);
        int marketsWithEdge = entries.Count(e => e.PnL != 0.0);
        double roi = marketsWithEdge > 0
            ? totalPnl / (marketsWithEdge * 100.0)
            : 0;

        return new EdgeSymbolResult
        {
            Symbol = symbol,
            Markets = entries.Count,
            WinRate = winRate,
            PnL = totalPnl,
            Roi = roi,
        };
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

    private EdgeBacktestResult EmptyResult(List<string> symbols) => new()
    {
        SelectedSymbols = symbols,
        FormulaNames = _formulas.Select(f => f.Name).ToList(),
    };

    private static string Truncate(string text, int maxLength = 60) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "…");
}
