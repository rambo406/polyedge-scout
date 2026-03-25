namespace PolyEdgeScout.Application.Tests;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Application.Services;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Tests for <see cref="OrderService.EvaluateAndTrade"/> SELL signal behaviour.
/// </summary>
public sealed class OrderServiceSellTests
{
    private readonly IClobClient _clobClient = Substitute.For<IClobClient>();
    private readonly ILogService _log = Substitute.For<ILogService>();
    private readonly ITradeRepository _tradeRepo = Substitute.For<ITradeRepository>();
    private readonly ITradeResultRepository _tradeResultRepo = Substitute.For<ITradeResultRepository>();
    private readonly IAppStateRepository _appStateRepo = Substitute.For<IAppStateRepository>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private OrderService CreateService(double minEdge = 0.08)
    {
        var config = new AppConfig
        {
            PaperMode = true,
            MinEdge = minEdge,
            DefaultBetSize = 100,
            KellyFraction = 0.5,
            MaxBankrollPercent = 0.02,
            MaxVolume = 3000,
        };

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ITradeRepository)).Returns(_tradeRepo);
        serviceProvider.GetService(typeof(ITradeResultRepository)).Returns(_tradeResultRepo);
        serviceProvider.GetService(typeof(IAppStateRepository)).Returns(_appStateRepo);
        serviceProvider.GetService(typeof(IAuditService)).Returns(_auditService);
        serviceProvider.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _tradeRepo.GetOpenTradesAsync().Returns([]);
        _tradeResultRepo.GetAllAsync().Returns([]);
        _appStateRepo.GetBankrollAsync().Returns(10_000.0);
        _appStateRepo.GetValueAsync("Bankroll").Returns("10000");

        return new OrderService(config, _clobClient, _log, scopeFactory);
    }

    private static Market CreateMarket(double yesPrice = 0.55, double noPrice = 0.45, double volume = 500) => new()
    {
        ConditionId = "cond-1",
        QuestionId = "q-1",
        TokenId = "tok-1",
        Question = "Will BTC hit $100k?",
        YesPrice = yesPrice,
        NoPrice = noPrice,
        Volume = volume,
        Active = true,
    };

    [Fact]
    public async Task EvaluateAndTrade_NegativeEdgeExceedingThreshold_ReturnsSellTrade()
    {
        var service = CreateService(minEdge: 0.08);
        await service.InitializeAsync();

        // Model=0.40, Market(YesPrice)=0.55 → edge = 0.40 - 0.55 = -0.15 < -0.08
        var market = CreateMarket(yesPrice: 0.55, noPrice: 0.45);
        var trade = service.EvaluateAndTrade(market, modelProbability: 0.40);

        Assert.NotNull(trade);
        Assert.Equal(TradeAction.Sell, trade.Action);
        Assert.Equal(market.NoPrice, trade.EntryPrice);
    }

    [Fact]
    public async Task EvaluateAndTrade_NegativeEdgeWithinThreshold_ReturnsNull()
    {
        var service = CreateService(minEdge: 0.08);
        await service.InitializeAsync();

        // Model=0.50, Market(YesPrice)=0.55 → edge = 0.50 - 0.55 = -0.05, |−0.05| < 0.08
        var market = CreateMarket(yesPrice: 0.55, noPrice: 0.45);
        var trade = service.EvaluateAndTrade(market, modelProbability: 0.50);

        Assert.Null(trade);
    }
}
