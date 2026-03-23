using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Infrastructure.Persistence.Repositories;

namespace PolyEdgeScout.Infrastructure.Tests.Persistence;

public sealed class TradeRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_RoundTrips()
    {
        using var context = _factory.CreateContext();
        var repo = new TradeRepository(context);

        var trade = new Trade
        {
            MarketQuestion = "Will BTC > 100k?",
            ConditionId = "cond1",
            TokenId = "tok1",
            Action = TradeAction.Buy,
            Status = TradeStatus.Filled,
            EntryPrice = 0.65,
            Shares = 10,
            ModelProbability = 0.75,
            Edge = 0.10,
            Outlay = 6.5,
            IsPaper = true,
        };

        await repo.AddAsync(trade);
        await context.SaveChangesAsync();

        var retrieved = await repo.GetByIdAsync(trade.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(trade.Id, retrieved.Id);
        Assert.Equal("Will BTC > 100k?", retrieved.MarketQuestion);
        Assert.Equal(TradeStatus.Filled, retrieved.Status);
    }

    [Fact]
    public async Task GetOpenTradesAsync_ReturnsOnlyFilledTrades()
    {
        using var context = _factory.CreateContext();
        var repo = new TradeRepository(context);

        var filled = new Trade { Status = TradeStatus.Filled, MarketQuestion = "Filled trade", ConditionId = "c1", TokenId = "t1" };
        var pending = new Trade { Status = TradeStatus.Pending, MarketQuestion = "Pending trade", ConditionId = "c2", TokenId = "t2" };
        var settled = new Trade { Status = TradeStatus.Settled, MarketQuestion = "Settled trade", ConditionId = "c3", TokenId = "t3" };

        await repo.AddAsync(filled);
        await repo.AddAsync(pending);
        await repo.AddAsync(settled);
        await context.SaveChangesAsync();

        var open = await repo.GetOpenTradesAsync();
        Assert.Single(open);
        Assert.Equal("Filled trade", open[0].MarketQuestion);
    }

    [Fact]
    public async Task GetAllTradesAsync_ReturnsAllTrades()
    {
        using var context = _factory.CreateContext();
        var repo = new TradeRepository(context);

        await repo.AddAsync(new Trade { Status = TradeStatus.Filled, ConditionId = "c1", TokenId = "t1" });
        await repo.AddAsync(new Trade { Status = TradeStatus.Pending, ConditionId = "c2", TokenId = "t2" });
        await context.SaveChangesAsync();

        var all = await repo.GetAllTradesAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesTrade()
    {
        using var context = _factory.CreateContext();
        var repo = new TradeRepository(context);

        var trade = new Trade { Status = TradeStatus.Filled, MarketQuestion = "Test", ConditionId = "c1", TokenId = "t1" };
        await repo.AddAsync(trade);
        await context.SaveChangesAsync();

        // Detach so we can update with a new instance
        context.ChangeTracker.Clear();

        var updated = trade with { Status = TradeStatus.Settled };
        await repo.UpdateAsync(updated);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var retrieved = await repo.GetByIdAsync(trade.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(TradeStatus.Settled, retrieved.Status);
    }

    public void Dispose() => _factory.Dispose();
}
