namespace PolyEdgeScout.Application.Tests;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Application.Services;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Tests for <see cref="OrderService.ResetPaperTradingAsync"/>.
/// </summary>
public sealed class OrderServiceResetTests
{
    private readonly IClobClient _clobClient = Substitute.For<IClobClient>();
    private readonly ILogService _log = Substitute.For<ILogService>();
    private readonly ITradeRepository _tradeRepo = Substitute.For<ITradeRepository>();
    private readonly ITradeResultRepository _tradeResultRepo = Substitute.For<ITradeResultRepository>();
    private readonly IAppStateRepository _appStateRepo = Substitute.For<IAppStateRepository>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private OrderService CreateService(bool paperMode = true)
    {
        var config = new AppConfig { PaperMode = paperMode };

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

        // Setup default returns for async methods
        _tradeRepo.GetOpenTradesAsync().Returns([]);
        _tradeResultRepo.GetAllAsync().Returns([]);
        _appStateRepo.GetBankrollAsync().Returns(10_000.0);
        _appStateRepo.GetValueAsync("Bankroll").Returns("10000");

        return new OrderService(config, _clobClient, _log, scopeFactory);
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InPaperMode_ClearsTradesAndResetsBankroll()
    {
        var service = CreateService(paperMode: true);
        await service.InitializeAsync();

        await service.ResetPaperTradingAsync();

        Assert.Empty(service.OpenTrades);
        Assert.Empty(service.SettledTrades);
        Assert.Equal(10_000.0, service.Bankroll);
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InPaperMode_DeletesAllTradesFromDatabase()
    {
        var service = CreateService(paperMode: true);
        await service.InitializeAsync();

        await service.ResetPaperTradingAsync();

        await _tradeRepo.Received(1).DeleteAllAsync();
        await _tradeResultRepo.Received(1).DeleteAllAsync();
        await _appStateRepo.Received().SetBankrollAsync(10_000.0);
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InPaperMode_LogsAuditEntry()
    {
        var service = CreateService(paperMode: true);
        await service.InitializeAsync();

        await service.ResetPaperTradingAsync();

        await _auditService.Received(1).LogAsync(
            "AppState",
            "PaperReset",
            AuditAction.Deleted,
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Is<string?>(v => v != null && v.Contains("Paper trading reset")));
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InPaperMode_CommitsTransaction()
    {
        var service = CreateService(paperMode: true);
        await service.InitializeAsync();

        await service.ResetPaperTradingAsync();

        await _unitOfWork.Received(1).BeginTransactionAsync();
        await _unitOfWork.Received(1).CommitTransactionAsync();
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InLiveMode_ThrowsInvalidOperationException()
    {
        var service = CreateService(paperMode: false);
        await service.InitializeAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPaperTradingAsync());

        Assert.Equal("Cannot reset trades in Live mode", ex.Message);
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InLiveMode_DoesNotDeleteTrades()
    {
        var service = CreateService(paperMode: false);
        await service.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPaperTradingAsync());

        await _tradeRepo.DidNotReceive().DeleteAllAsync();
        await _tradeResultRepo.DidNotReceive().DeleteAllAsync();
    }

    [Fact]
    public async Task ResetPaperTradingAsync_InPaperMode_LogsInfoMessage()
    {
        var service = CreateService(paperMode: true);
        await service.InitializeAsync();

        await service.ResetPaperTradingAsync();

        _log.Received(1).Info(Arg.Is<string>(s => s.Contains("Paper trading reset")));
    }
}
