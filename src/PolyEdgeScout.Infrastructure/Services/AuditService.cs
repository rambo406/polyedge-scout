namespace PolyEdgeScout.Infrastructure.Services;

using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;

public sealed class AuditService(IAuditLogRepository auditLogRepository) : IAuditService
{
    public async Task LogAsync(
        string entityType,
        string entityId,
        AuditAction action,
        string correlationId,
        string? propertyName = null,
        string? oldValue = null,
        string? newValue = null)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = "system",
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = newValue,
            CorrelationId = correlationId,
        };

        await auditLogRepository.AddAsync(entry);
    }

    public async Task LogBatchAsync(IEnumerable<AuditLogEntry> entries)
    {
        await auditLogRepository.AddRangeAsync(entries);
    }
}
