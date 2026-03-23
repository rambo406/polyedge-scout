using PolyEdgeScout.Infrastructure.Persistence.Repositories;

namespace PolyEdgeScout.Infrastructure.Tests.Persistence;

public sealed class AppStateRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetBankrollAsync_ReturnsDefault_WhenNotSet()
    {
        using var context = _factory.CreateContext();
        var repo = new AppStateRepository(context);

        var bankroll = await repo.GetBankrollAsync();
        Assert.Equal(10_000.0, bankroll);
    }

    [Fact]
    public async Task SetBankrollAsync_And_GetBankrollAsync_RoundTrips()
    {
        using var context = _factory.CreateContext();
        var repo = new AppStateRepository(context);

        await repo.SetBankrollAsync(9500.50);
        await context.SaveChangesAsync();

        var bankroll = await repo.GetBankrollAsync();
        Assert.Equal(9500.50, bankroll, precision: 2);
    }

    [Fact]
    public async Task SetValueAsync_OverwritesExistingValue()
    {
        using var context = _factory.CreateContext();
        var repo = new AppStateRepository(context);

        await repo.SetValueAsync("TestKey", "Value1");
        await context.SaveChangesAsync();

        await repo.SetValueAsync("TestKey", "Value2");
        await context.SaveChangesAsync();

        var value = await repo.GetValueAsync("TestKey");
        Assert.Equal("Value2", value);
    }

    [Fact]
    public async Task GetValueAsync_ReturnsNull_WhenKeyNotFound()
    {
        using var context = _factory.CreateContext();
        var repo = new AppStateRepository(context);

        var value = await repo.GetValueAsync("NonexistentKey");
        Assert.Null(value);
    }

    public void Dispose() => _factory.Dispose();
}
