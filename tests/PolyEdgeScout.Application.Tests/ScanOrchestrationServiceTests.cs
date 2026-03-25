namespace PolyEdgeScout.Application.Tests;

using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Application.Services;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Tests for <see cref="ScanOrchestrationService.ScanAndEvaluateAsync"/> —
/// verifies edge calculation with target-price scaling and fallback.
/// </summary>
public sealed class ScanOrchestrationServiceTests
{
    private readonly IScannerService _scanner = Substitute.For<IScannerService>();
    private readonly IProbabilityModelService _probModel = Substitute.For<IProbabilityModelService>();
    private readonly IOrderService _orders = Substitute.For<IOrderService>();
    private readonly ILogService _log = Substitute.For<ILogService>();

    private ScanOrchestrationService CreateService(double minEdge = 0.08, double maxVolume = 3000)
    {
        var config = new AppConfig { MinEdge = minEdge, MaxVolume = maxVolume };
        return new ScanOrchestrationService(config, _scanner, _probModel, _orders, _log);
    }

    private static Market CreateMarket(double yesPrice = 0.45, double volume = 500) => new()
    {
        ConditionId = "cond-1",
        QuestionId = "q-1",
        TokenId = "tok-1",
        Question = "Will BTC hit $100k?",
        YesPrice = yesPrice,
        NoPrice = 1 - yesPrice,
        Volume = volume,
        Active = true,
    };

    [Fact]
    public async Task ScanAndEvaluate_WithTargetAndCurrentPrice_ReturnsEnhancedEdge()
    {
        var market = CreateMarket(yesPrice: 0.45);
        _scanner.ScanMarketsAsync(Arg.Any<CancellationToken>())
            .Returns([market]);

        // Model=0.60, Target=100000, Current=60000
        // baseEdge = 0.60 - 0.45 = 0.15
        // priceRatio = |100000-60000|/60000 ≈ 0.6667
        // edge = 0.15 * 1.6667 ≈ 0.25
        _probModel.CalculateProbabilityAsync(market, Arg.Any<CancellationToken>())
            .Returns(new ModelEvaluation(0.60, TargetPrice: 100_000, CurrentAssetPrice: 60_000));

        var service = CreateService();
        var results = await service.ScanAndEvaluateAsync();

        Assert.Single(results);
        Assert.Equal(0.25, results[0].Edge, precision: 2);
        Assert.Equal(100_000, results[0].TargetPrice);
        Assert.Equal(60_000, results[0].CurrentAssetPrice);
    }

    [Fact]
    public async Task ScanAndEvaluate_WithNullTargetPrice_ReturnsFallbackEdge()
    {
        var market = CreateMarket(yesPrice: 0.45);
        _scanner.ScanMarketsAsync(Arg.Any<CancellationToken>())
            .Returns([market]);

        // No target price → fallback: edge = 0.60 - 0.45 = 0.15
        _probModel.CalculateProbabilityAsync(market, Arg.Any<CancellationToken>())
            .Returns(new ModelEvaluation(0.60, TargetPrice: null, CurrentAssetPrice: null));

        var service = CreateService();
        var results = await service.ScanAndEvaluateAsync();

        Assert.Single(results);
        Assert.Equal(0.15, results[0].Edge, precision: 10);
        Assert.Null(results[0].TargetPrice);
        Assert.Null(results[0].CurrentAssetPrice);
    }

    [Fact]
    public async Task ScanAndEvaluate_ActionIsTradeActionEnum()
    {
        var market = CreateMarket(yesPrice: 0.45);
        _scanner.ScanMarketsAsync(Arg.Any<CancellationToken>())
            .Returns([market]);

        _probModel.CalculateProbabilityAsync(market, Arg.Any<CancellationToken>())
            .Returns(new ModelEvaluation(0.60, TargetPrice: null, CurrentAssetPrice: null));

        var service = CreateService(minEdge: 0.08);
        var results = await service.ScanAndEvaluateAsync();

        Assert.Single(results);
        Assert.IsType<TradeAction>(results[0].Action);
        Assert.Equal(TradeAction.Buy, results[0].Action); // edge 0.15 > 0.08
    }

    [Fact]
    public async Task ScanAndEvaluate_SellSignal_HasTradeActionSell()
    {
        var market = CreateMarket(yesPrice: 0.55);
        _scanner.ScanMarketsAsync(Arg.Any<CancellationToken>())
            .Returns([market]);

        // Model=0.40, Market=0.55 → edge = -0.15 < -0.08 → Sell
        _probModel.CalculateProbabilityAsync(market, Arg.Any<CancellationToken>())
            .Returns(new ModelEvaluation(0.40, TargetPrice: null, CurrentAssetPrice: null));

        var service = CreateService(minEdge: 0.08);
        var results = await service.ScanAndEvaluateAsync();

        Assert.Single(results);
        Assert.Equal(TradeAction.Sell, results[0].Action);
    }

    [Fact]
    public async Task ScanAndEvaluate_HighVolume_ForcesHold()
    {
        var market = CreateMarket(yesPrice: 0.45, volume: 5000);
        _scanner.ScanMarketsAsync(Arg.Any<CancellationToken>())
            .Returns([market]);

        _probModel.CalculateProbabilityAsync(market, Arg.Any<CancellationToken>())
            .Returns(new ModelEvaluation(0.60, TargetPrice: null, CurrentAssetPrice: null));

        var service = CreateService(minEdge: 0.08, maxVolume: 3000);
        var results = await service.ScanAndEvaluateAsync();

        Assert.Single(results);
        Assert.Equal(TradeAction.Hold, results[0].Action);
    }
}
