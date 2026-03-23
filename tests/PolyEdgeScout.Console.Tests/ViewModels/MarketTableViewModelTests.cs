namespace PolyEdgeScout.Console.Tests.ViewModels;

using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;
using Xunit;

public class MarketTableViewModelTests
{
    private readonly IOrderService _orderService = Substitute.For<IOrderService>();
    private readonly ILogService _log = Substitute.For<ILogService>();
    private readonly AppConfig _config = new() { MinEdge = 0.08 };

    private MarketTableViewModel CreateVm() => new(_orderService, _config, _log);

    [Fact]
    public void UpdateMarkets_SetsMarketsAndFiresEvent()
    {
        var vm = CreateVm();
        var fired = false;
        vm.MarketsUpdated += () => fired = true;

        var markets = new List<MarketScanResult>
        {
            new() { Market = new Market { Question = "Test" }, Edge = 0.1, ModelProbability = 0.5, Action = "BUY" }
        };
        vm.UpdateMarkets(markets);

        Assert.True(fired);
        Assert.Single(vm.Markets);
    }

    [Fact]
    public void UpdateMarkets_ClampsSelectedIndex()
    {
        var vm = CreateVm();
        vm.SelectedIndex = 5;

        vm.UpdateMarkets([]);

        Assert.Equal(0, vm.SelectedIndex);
    }

    [Fact]
    public async Task ExecuteTradeAsync_InsufficientEdge_LogsWarning()
    {
        var vm = CreateVm();
        vm.UpdateMarkets([new() { Market = new Market { Question = "Low" }, Edge = 0.01, ModelProbability = 0.3, Action = "HOLD" }]);
        vm.SelectedIndex = 0;

        await vm.ExecuteTradeAsync();

        _log.Received(1).Warn(Arg.Is<string>(s => s.Contains("Insufficient edge")));
    }

    [Fact]
    public async Task ExecuteTradeAsync_WithEdge_ExecutesTrade()
    {
        var trade = new Trade { Id = "t1" };
        _orderService.EvaluateAndTrade(Arg.Any<Market>(), Arg.Any<double>(), Arg.Any<CancellationToken>()).Returns(trade);
        _orderService.ExecuteTradeAsync(trade, Arg.Any<CancellationToken>()).Returns(trade);

        var vm = CreateVm();
        vm.UpdateMarkets([new() { Market = new Market { Question = "Good" }, Edge = 0.15, ModelProbability = 0.6, Action = "BUY" }]);
        vm.SelectedIndex = 0;

        await vm.ExecuteTradeAsync();

        await _orderService.Received(1).ExecuteTradeAsync(trade, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteTradeAsync_NoSelection_DoesNothing()
    {
        var vm = CreateVm();
        // Empty markets, index out of range
        await vm.ExecuteTradeAsync();

        _orderService.DidNotReceive().EvaluateAndTrade(Arg.Any<Market>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
    }
}
