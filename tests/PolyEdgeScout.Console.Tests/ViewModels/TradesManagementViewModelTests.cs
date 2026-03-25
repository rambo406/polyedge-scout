namespace PolyEdgeScout.Console.Tests.ViewModels;

using NSubstitute;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using Xunit;

public sealed class TradesManagementViewModelTests
{
    private readonly IOrderService _orderService = Substitute.For<IOrderService>();

    private TradesManagementViewModel CreateSut() => new(_orderService);

    [Fact]
    public void OpenTrades_DefaultsToEmpty()
    {
        var sut = CreateSut();

        Assert.Empty(sut.OpenTrades);
    }

    [Fact]
    public void SettledTrades_DefaultsToEmpty()
    {
        var sut = CreateSut();

        Assert.Empty(sut.SettledTrades);
    }

    [Fact]
    public void RefreshTrades_UpdatesFromOrderService()
    {
        var openTrades = new List<Trade>
        {
            new() { MarketQuestion = "Will it rain?", Action = TradeAction.Buy, EntryPrice = 0.65 }
        };
        var settledTrades = new List<TradeResult>
        {
            new() { TradeId = "abc123", MarketQuestion = "Did it rain?", Won = true }
        };

        _orderService.OpenTrades.Returns(openTrades);
        _orderService.SettledTrades.Returns(settledTrades);

        var sut = CreateSut();
        sut.RefreshTrades();

        Assert.Single(sut.OpenTrades);
        Assert.Equal("Will it rain?", sut.OpenTrades[0].MarketQuestion);
        Assert.Single(sut.SettledTrades);
        Assert.Equal("abc123", sut.SettledTrades[0].TradeId);
    }

    [Fact]
    public void RefreshTrades_FiresTradesUpdatedEvent()
    {
        _orderService.OpenTrades.Returns(new List<Trade>());
        _orderService.SettledTrades.Returns(new List<TradeResult>());

        var sut = CreateSut();
        var fired = false;
        sut.TradesUpdated += () => fired = true;

        sut.RefreshTrades();

        Assert.True(fired);
    }

    [Fact]
    public void RefreshTrades_WithEmptyService_SetsEmptyLists()
    {
        _orderService.OpenTrades.Returns(new List<Trade>());
        _orderService.SettledTrades.Returns(new List<TradeResult>());

        var sut = CreateSut();
        sut.RefreshTrades();

        Assert.Empty(sut.OpenTrades);
        Assert.Empty(sut.SettledTrades);
    }
}
