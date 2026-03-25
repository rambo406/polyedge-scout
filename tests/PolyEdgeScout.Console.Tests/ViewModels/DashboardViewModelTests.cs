namespace PolyEdgeScout.Console.Tests.ViewModels;

using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;
using Xunit;

public class DashboardViewModelTests
{
    private readonly IScanOrchestrationService _orchestrator = Substitute.For<IScanOrchestrationService>();
    private readonly IOrderService _orderService = Substitute.For<IOrderService>();
    private readonly ILogService _log = Substitute.For<ILogService>();
    private readonly AppConfig _config = new() { ScanIntervalSeconds = 1, MinEdge = 0.08 };

    private DashboardViewModel CreateVm()
    {
        var marketVm = new MarketTableViewModel(_orderService, _config, _log);
        var portfolioVm = new PortfolioViewModel();
        var tradesVm = new TradesViewModel();
        var tradesManagementVm = new TradesManagementViewModel(_orderService);
        var logVm = new LogViewModel(_log);
        return new DashboardViewModel(_orchestrator, _orderService, _config, _log, marketVm, portfolioVm, tradesVm, tradesManagementVm, logVm);
    }

    [Fact]
    public void ToggleMode_TogglesPaperModeAndFiresEvent()
    {
        _orderService.PaperMode = true;
        _orderService.PaperMode.Returns(false); // after toggle

        var vm = CreateVm();
        var fired = false;
        vm.ModeChanged += () => fired = true;

        vm.ToggleMode();

        Assert.True(fired);
    }

    [Fact]
    public void RequestQuit_FiresQuitEvent()
    {
        var vm = CreateVm();
        var fired = false;
        vm.QuitRequested += () => fired = true;

        vm.RequestQuit();

        Assert.True(fired);
    }

    [Fact]
    public async Task RunScanLoopAsync_UpdatesChildViewModels()
    {
        var markets = new List<MarketScanResult>
        {
            new() { Market = new Market { Question = "BTC" }, Edge = 0.12, ModelProbability = 0.7, Action = TradeAction.Buy }
        };
        _orchestrator.ScanEvaluateAndAutoTradeAsync(Arg.Any<CancellationToken>()).Returns(markets);
        _orderService.GetPnlSnapshot().Returns(new PnlSnapshot { Bankroll = 9500 });

        var vm = CreateVm();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try { await vm.RunScanLoopAsync(cts.Token); }
        catch (OperationCanceledException) { }

        Assert.NotEqual(DateTime.MinValue, vm.LastScanTime);
        Assert.Single(vm.MarketTable.Markets);
        Assert.Equal(9500, vm.Portfolio.Snapshot.Bankroll);
    }

    [Fact]
    public void PaperMode_DelegatesToOrderService()
    {
        _orderService.PaperMode.Returns(true);
        var vm = CreateVm();

        Assert.True(vm.PaperMode);
    }

    [Fact]
    public async Task RunScanLoopAsync_SetsLastErrorOnException()
    {
        _orchestrator.ScanEvaluateAndAutoTradeAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<MarketScanResult>>(x => throw new InvalidOperationException("API down"));

        var vm = CreateVm();
        var errorFired = false;
        vm.ErrorChanged += () => errorFired = true;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try { await vm.RunScanLoopAsync(cts.Token); }
        catch (OperationCanceledException) { }

        Assert.Equal("API down", vm.LastError);
        Assert.True(errorFired);
    }

    [Fact]
    public async Task RunScanLoopAsync_ClearsLastErrorOnSuccess()
    {
        var callCount = 0;
        _orchestrator.ScanEvaluateAndAutoTradeAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("transient failure");
                return new List<MarketScanResult>();
            });
        _orderService.GetPnlSnapshot().Returns(new PnlSnapshot { Bankroll = 10000 });

        var vm = CreateVm();
        string? lastErrorSnapshot = null;
        var errorChangedCount = 0;
        vm.ErrorChanged += () =>
        {
            errorChangedCount++;
            lastErrorSnapshot = vm.LastError;
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2500));

        try { await vm.RunScanLoopAsync(cts.Token); }
        catch (OperationCanceledException) { }

        Assert.Null(vm.LastError);
        Assert.Null(lastErrorSnapshot);
        Assert.True(errorChangedCount >= 2, "ErrorChanged should fire at least twice (once for error, once for clear)");
    }
}
