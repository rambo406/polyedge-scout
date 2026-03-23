using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Infrastructure.Persistence.Repositories;
using PolyEdgeScout.Infrastructure.Services;

namespace PolyEdgeScout.Infrastructure.Tests.Services;

public sealed class AuditServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task LogAsync_CreatesEntryWithCorrectFields()
    {
        using var context = _factory.CreateContext();
        var auditRepo = new AuditLogRepository(context);
        var service = new AuditService(auditRepo);

        var correlationId = Guid.NewGuid().ToString("N");
        await service.LogAsync("Trade", "trade123", AuditAction.Created, correlationId);
        await context.SaveChangesAsync();

        var entries = await auditRepo.GetByEntityAsync("Trade", "trade123");
        Assert.Single(entries);

        var entry = entries[0];
        Assert.Equal("Trade", entry.EntityType);
        Assert.Equal("trade123", entry.EntityId);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal("system", entry.UserId);
        Assert.Equal(correlationId, entry.CorrelationId);
        Assert.True(entry.Timestamp <= DateTime.UtcNow);
        Assert.True(entry.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task LogAsync_WithPropertyChange_StoresOldAndNewValues()
    {
        using var context = _factory.CreateContext();
        var auditRepo = new AuditLogRepository(context);
        var service = new AuditService(auditRepo);

        var correlationId = Guid.NewGuid().ToString("N");
        await service.LogAsync("AppState", "Bankroll", AuditAction.Updated, correlationId,
            propertyName: "Bankroll", oldValue: "10000", newValue: "9500");
        await context.SaveChangesAsync();

        var entries = await auditRepo.GetByEntityAsync("AppState", "Bankroll");
        Assert.Single(entries);

        var entry = entries[0];
        Assert.Equal("Bankroll", entry.PropertyName);
        Assert.Equal("10000", entry.OldValue);
        Assert.Equal("9500", entry.NewValue);
    }

    [Fact]
    public async Task LogBatchAsync_CreatesMultipleEntries()
    {
        using var context = _factory.CreateContext();
        var auditRepo = new AuditLogRepository(context);
        var service = new AuditService(auditRepo);

        var correlationId = Guid.NewGuid().ToString("N");
        var entries = new[]
        {
            new Domain.Entities.AuditLogEntry { EntityType = "Trade", EntityId = "t1", Action = AuditAction.Created, CorrelationId = correlationId },
            new Domain.Entities.AuditLogEntry { EntityType = "AppState", EntityId = "Bankroll", Action = AuditAction.Updated, CorrelationId = correlationId },
        };

        await service.LogBatchAsync(entries);
        await context.SaveChangesAsync();

        var results = await auditRepo.GetByCorrelationIdAsync(correlationId);
        Assert.Equal(2, results.Count);
    }

    public void Dispose() => _factory.Dispose();
}
