namespace PolyEdgeScout.Console.Tests.ViewModels;

using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Entities;
using Xunit;

public class TradesViewModelTests
{
    [Fact]
    public void UpdateTrades_SetsTradesAndFiresEvent()
    {
        var vm = new TradesViewModel();
        var fired = false;
        vm.TradesUpdated += () => fired = true;

        var trades = new List<TradeResult>
        {
            new() { TradeId = "t1", Won = true, MarketQuestion = "Q1" }
        };
        vm.UpdateTrades(trades);

        Assert.True(fired);
        Assert.Single(vm.RecentTrades);
    }

    [Fact]
    public void DefaultTrades_IsEmpty()
    {
        var vm = new TradesViewModel();
        Assert.Empty(vm.RecentTrades);
    }
}
