namespace PolyEdgeScout.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;

public sealed class AuditLogRepository(TradingDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry)
    {
        await context.AuditLog.AddAsync(entry);
    }

    public async Task AddRangeAsync(IEnumerable<AuditLogEntry> entries)
    {
        await context.AuditLog.AddRangeAsync(entries);
    }

    public async Task<List<AuditLogEntry>> GetByEntityAsync(string entityType, string entityId)
        => await context.AuditLog
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

    public async Task<List<AuditLogEntry>> GetByCorrelationIdAsync(string correlationId)
        => await context.AuditLog
            .Where(a => a.CorrelationId == correlationId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

    public async Task<List<AuditLogEntry>> GetByDateRangeAsync(DateTime from, DateTime to)
        => await context.AuditLog
            .Where(a => a.Timestamp >= from && a.Timestamp <= to)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();
}
