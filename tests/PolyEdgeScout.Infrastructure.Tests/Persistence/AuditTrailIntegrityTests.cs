using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Infrastructure.Persistence.Repositories;

namespace PolyEdgeScout.Infrastructure.Tests.Persistence;

public sealed class AuditTrailIntegrityTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public void IAuditLogRepository_HasNoUpdateOrDeleteMethods()
    {
        var methods = typeof(IAuditLogRepository).GetMethods();
        var methodNames = methods.Select(m => m.Name).ToList();

        Assert.DoesNotContain("UpdateAsync", methodNames);
        Assert.DoesNotContain("DeleteAsync", methodNames);
        Assert.DoesNotContain("RemoveAsync", methodNames);
    }

    [Fact]
    public async Task CorrelationId_GroupsRelatedEntries()
    {
        using var context = _factory.CreateContext();
        var repo = new AuditLogRepository(context);

        var corrId1 = Guid.NewGuid().ToString("N");
        var corrId2 = Guid.NewGuid().ToString("N");

        await repo.AddRangeAsync(new[]
        {
            new AuditLogEntry { EntityType = "Trade", EntityId = "t1", Action = AuditAction.Created, CorrelationId = corrId1 },
            new AuditLogEntry { EntityType = "AppState", EntityId = "Bankroll", Action = AuditAction.Updated, CorrelationId = corrId1 },
            new AuditLogEntry { EntityType = "Trade", EntityId = "t2", Action = AuditAction.Created, CorrelationId = corrId2 },
        });
        await context.SaveChangesAsync();

        var group1 = await repo.GetByCorrelationIdAsync(corrId1);
        var group2 = await repo.GetByCorrelationIdAsync(corrId2);

        Assert.Equal(2, group1.Count);
        Assert.Single(group2);
    }

    [Fact]
    public async Task Timestamps_AreStoredInUtc()
    {
        using var context = _factory.CreateContext();
        var repo = new AuditLogRepository(context);

        var utcNow = DateTime.UtcNow;
        var entry = new AuditLogEntry
        {
            EntityType = "Trade",
            EntityId = "t1",
            Timestamp = utcNow,
            CorrelationId = "c1",
        };

        await repo.AddAsync(entry);
        await context.SaveChangesAsync();

        var results = await repo.GetByEntityAsync("Trade", "t1");
        Assert.Single(results);
        // Verify timestamp is close to what was stored (within 1 second tolerance)
        Assert.True((results[0].Timestamp - utcNow).TotalSeconds < 1.0);
    }

    public void Dispose() => _factory.Dispose();
}
