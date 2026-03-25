namespace PolyEdgeScout.Application.Tests;

using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Application.Services;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Tests for <see cref="EdgeBacktestService"/>.
/// Verifies symbol filtering, multi-formula evaluation, metric computation,
/// cancellation handling, and per-market error isolation.
/// </summary>
public sealed class EdgeBacktestServiceTests
{
    private readonly IGammaApiClient _gammaClient = Substitute.For<IGammaApiClient>();
    private readonly IProbabilityModelService _probModel = Substitute.For<IProbabilityModelService>();
    private readonly ILogService _log = Substitute.For<ILogService>();

    private static AppConfig CreateConfig(double minEdge = 0.08) => new() { MinEdge = minEdge };

    private EdgeBacktestService CreateService(
        AppConfig? config = null,
        params IEdgeFormula[] formulas)
    {
        config ??= CreateConfig();
        IEnumerable<IEdgeFormula> formulaList = formulas.Length > 0
            ? formulas
            : [CreateFormula("Default", (mp, price, _, _) => mp - price)];

        return new EdgeBacktestService(config, _gammaClient, _probModel, _log, formulaList);
    }

    private static IEdgeFormula CreateFormula(
        string name,
        Func<double, double, double?, double?, double> calc)
    {
        var formula = Substitute.For<IEdgeFormula>();
        formula.Name.Returns(name);
        formula.CalculateEdge(
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<double?>(),
            Arg.Any<double?>())
            .Returns(ci => calc(
                ci.ArgAt<double>(0),
                ci.ArgAt<double>(1),
                ci.ArgAt<double?>(2),
                ci.ArgAt<double?>(3)));
        return formula;
    }

    /// <summary>
    /// Creates a resolved GammaMarketResponse with the given question and outcome.
    /// </summary>
    private static GammaMarketResponse CreateResolvedMarket(
        string question,
        string outcome,
        double yesPrice = 0.50)
    {
        return new GammaMarketResponse
        {
            ConditionId = $"cond-{Guid.NewGuid():N}",
            QuestionId = $"q-{Guid.NewGuid():N}",
            Question = question,
            Outcome = outcome,
            ResolutionSource = "resolved",
            Active = false,
            Closed = true,
            Tokens =
            [
                new GammaToken { TokenId = "tok-yes", Outcome = "Yes", Price = yesPrice },
                new GammaToken { TokenId = "tok-no", Outcome = "No", Price = 1 - yesPrice },
            ],
        };
    }

    /// <summary>
    /// Multi-symbol filtering correctly includes only markets matching the specified symbols.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_FiltersMarketsBySymbol()
    {
        var btcMarket = CreateResolvedMarket("Will BTC hit $100k?", "Yes");
        var ethMarket = CreateResolvedMarket("Will ETH hit $5k?", "No");
        var solMarket = CreateResolvedMarket("Will SOL hit $200?", "Yes");

        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([btcMarket, ethMarket, solMarket]);

        SetupProbabilityModel(0.60, 100_000, 60_000);

        var service = CreateService();
        var result = await service.RunEdgeBacktestAsync(["BTC", "ETH"], "5m", "15m");

        // SOL market should be excluded
        var leftEntries = result.LeftTimeframeResult.FormulaResults
            .SelectMany(f => f.Entries)
            .ToList();

        Assert.Equal(2, leftEntries.Count);
        Assert.Contains(leftEntries, e => e.Symbol == "BTC");
        Assert.Contains(leftEntries, e => e.Symbol == "ETH");
        Assert.DoesNotContain(leftEntries, e => e.Symbol == "SOL");
    }

    /// <summary>
    /// Multiple formulas all produce results for each market.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_MultipleFormulas_AllProduceResults()
    {
        var btcMarket = CreateResolvedMarket("Will BTC hit $100k?", "Yes");

        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([btcMarket]);

        SetupProbabilityModel(0.60, 100_000, 60_000);

        var formula1 = CreateFormula("Base", (mp, price, _, _) => mp - price);
        var formula2 = CreateFormula("Scaled", (mp, price, tp, cp) =>
        {
            double baseEdge = mp - price;
            if (tp.HasValue && cp.HasValue && cp.Value > 0)
            {
                double ratio = Math.Abs(tp.Value - cp.Value) / cp.Value;
                return baseEdge * (1 + ratio);
            }
            return baseEdge;
        });

        var service = CreateService(config: null, formula1, formula2);
        var result = await service.RunEdgeBacktestAsync(["BTC"], "5m", "15m");

        // Both formulas should produce results
        Assert.Equal(2, result.LeftTimeframeResult.FormulaResults.Count);
        Assert.Contains(result.LeftTimeframeResult.FormulaResults, f => f.FormulaName == "Base");
        Assert.Contains(result.LeftTimeframeResult.FormulaResults, f => f.FormulaName == "Scaled");
    }

    /// <summary>
    /// Metrics computation: win rate, P&amp;L, and ROI are computed from resolved market outcomes.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_ComputesMetricsCorrectly()
    {
        // Two markets, one resolved Yes (model correct), one resolved No (model incorrect)
        var winMarket = CreateResolvedMarket("Will BTC hit $50k?", "Yes", yesPrice: 0.45);
        var loseMarket = CreateResolvedMarket("Will BTC hit $200k?", "No", yesPrice: 0.45);

        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([winMarket, loseMarket]);

        // Model says 60% for both → model is correct on the Yes outcome, incorrect on the No
        SetupProbabilityModel(0.60, 100_000, 60_000);

        // Formula returns a large edge so all trades qualify
        var formula = CreateFormula("Test", (_, _, _, _) => 0.20);
        var service = CreateService(config: CreateConfig(minEdge: 0.05), formula);

        var result = await service.RunEdgeBacktestAsync(["BTC"], "5m", "15m");

        var leftResult = result.LeftTimeframeResult;
        Assert.Equal(2, leftResult.TotalMarkets);
        Assert.True(leftResult.FormulaResults.Count > 0);

        var formulaResult = leftResult.FormulaResults[0];
        // Win rate: 1 correct out of 2 = 0.50
        Assert.Equal(0.50, formulaResult.WinRate, precision: 2);
        // PnL should be non-zero (one win, one loss)
        Assert.NotEqual(0.0, formulaResult.HypotheticalPnl);
    }

    /// <summary>
    /// Cancellation returns partial results without throwing.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_CancellationReturnsPartialResults()
    {
        // Set up many markets so cancellation can interrupt
        var markets = Enumerable.Range(1, 20)
            .Select(i => CreateResolvedMarket($"Will BTC hit ${i}k?", i % 2 == 0 ? "Yes" : "No"))
            .ToList();

        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(markets);

        // Probability model returns normally
        _probModel.CalculateProbabilityAsync(Arg.Any<Market>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.ArgAt<CancellationToken>(1);
                ct.ThrowIfCancellationRequested();
                return new ModelEvaluation(0.60, 100_000, 60_000);
            });

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Cancel after the service starts processing
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        // Should not throw — returns partial results
        var result = await service.RunEdgeBacktestAsync(["BTC"], "5m", "15m", cts.Token);

        // Result should exist (possibly with fewer entries than total markets)
        Assert.NotNull(result);
        Assert.Equal(["BTC"], result.SelectedSymbols);
    }

    /// <summary>
    /// An error processing one market does not stop evaluation of others.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_ErrorInOneMarket_DoesNotStopOthers()
    {
        var goodMarket = CreateResolvedMarket("Will BTC hit $100k?", "Yes", yesPrice: 0.45);
        var badMarket = CreateResolvedMarket("Will BTC hit $999k?", "Yes", yesPrice: 0.45);
        var anotherGoodMarket = CreateResolvedMarket("Will BTC hit $50k?", "No", yesPrice: 0.45);

        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([goodMarket, badMarket, anotherGoodMarket]);

        // First call succeeds, second throws, third succeeds
        int callCount = 0;
        _probModel.CalculateProbabilityAsync(Arg.Any<Market>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount % 3 == 2) // Second call in each batch throws
                    throw new InvalidOperationException("API error for bad market");
                return new ModelEvaluation(0.60, 100_000, 60_000);
            });

        var service = CreateService();
        var result = await service.RunEdgeBacktestAsync(["BTC"], "5m", "15m");

        // Result should still exist with entries from the good markets
        Assert.NotNull(result);
        var leftEntries = result.LeftTimeframeResult.FormulaResults
            .SelectMany(f => f.Entries)
            .ToList();

        // At least some entries should be present from the non-failing markets
        Assert.True(leftEntries.Count > 0, "Expected some entries from non-failing markets");

        // Error should be logged
        _log.Received().Warn(Arg.Is<string>(s => s.Contains("error")));
    }

    /// <summary>
    /// Empty resolved markets returns an empty result without errors.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_NoResolvedMarkets_ReturnsEmptyResult()
    {
        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<GammaMarketResponse>());

        var service = CreateService();
        var result = await service.RunEdgeBacktestAsync(["BTC"], "5m", "15m");

        Assert.NotNull(result);
        Assert.Equal(0, result.LeftTimeframeResult.TotalMarkets);
        Assert.Equal(0, result.RightTimeframeResult.TotalMarkets);
    }

    /// <summary>
    /// No symbols match any markets returns empty results.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktest_NoMatchingSymbols_ReturnsEmptyResult()
    {
        var market = CreateResolvedMarket("Will DOGE reach $1?", "Yes");
        _gammaClient.FetchResolvedMarketsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([market]);

        var service = CreateService();
        var result = await service.RunEdgeBacktestAsync(["BTC"], "5m", "15m");

        Assert.NotNull(result);
        Assert.Equal(0, result.LeftTimeframeResult.TotalMarkets);
    }

    /// <summary>
    /// Sets up the probability model to return a fixed evaluation for any market.
    /// </summary>
    private void SetupProbabilityModel(
        double modelProbability,
        double targetPrice,
        double currentAssetPrice)
    {
        _probModel.CalculateProbabilityAsync(Arg.Any<Market>(), Arg.Any<CancellationToken>())
            .Returns(new ModelEvaluation(modelProbability, targetPrice, currentAssetPrice));
    }
}
