using System.Reflection;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Infrastructure.Persistence.Repositories;

namespace PolyEdgeScout.Infrastructure.Tests.Persistence;

public sealed class AuditLogRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task AddAsync_And_GetByEntityAsync_Works()
    {
        using var context = _factory.CreateContext();
        var repo = new AuditLogRepository(context);

        var entry = new AuditLogEntry
        {
            EntityType = "Trade",
            EntityId = "abc123",
            Action = AuditAction.Created,
            CorrelationId = "corr1",
        };

        await repo.AddAsync(entry);
        await context.SaveChangesAsync();

        var results = await repo.GetByEntityAsync("Trade", "abc123");
        Assert.Single(results);
        Assert.Equal(AuditAction.Created, results[0].Action);
    }

    [Fact]
    public async Task AddRangeAsync_And_GetByCorrelationIdAsync_Works()
    {
        using var context = _factory.CreateContext();
        var repo = new AuditLogRepository(context);

        var corrId = Guid.NewGuid().ToString("N");
        var entries = new[]
        {
            new AuditLogEntry { EntityType = "Trade", EntityId = "t1", Action = AuditAction.Created, CorrelationId = corrId },
            new AuditLogEntry { EntityType = "AppState", EntityId = "Bankroll", Action = AuditAction.Updated, CorrelationId = corrId },
        };

        await repo.AddRangeAsync(entries);
        await context.SaveChangesAsync();

        var results = await repo.GetByCorrelationIdAsync(corrId);
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(corrId, r.CorrelationId));
    }

    [Fact]
    public async Task GetByDateRangeAsync_FiltersCorrectly()
    {
        using var context = _factory.CreateContext();
        var repo = new AuditLogRepository(context);

        var now = DateTime.UtcNow;
        var entries = new[]
        {
            new AuditLogEntry { EntityType = "Trade", EntityId = "t1", Timestamp = now.AddHours(-2), CorrelationId = "c1" },
            new AuditLogEntry { EntityType = "Trade", EntityId = "t2", Timestamp = now, CorrelationId = "c2" },
            new AuditLogEntry { EntityType = "Trade", EntityId = "t3", Timestamp = now.AddHours(2), CorrelationId = "c3" },
        };

        await repo.AddRangeAsync(entries);
        await context.SaveChangesAsync();

        var results = await repo.GetByDateRangeAsync(now.AddHours(-1), now.AddHours(1));
        Assert.Single(results);
        Assert.Equal("t2", results[0].EntityId);
    }

    [Fact]
    public void Repository_HasNoUpdateOrDeleteMethods()
    {
        var methods = typeof(AuditLogRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var methodNames = methods.Select(m => m.Name).ToList();

        Assert.DoesNotContain("UpdateAsync", methodNames);
        Assert.DoesNotContain("DeleteAsync", methodNames);
        Assert.DoesNotContain("RemoveAsync", methodNames);
    }

    public void Dispose() => _factory.Dispose();
}
