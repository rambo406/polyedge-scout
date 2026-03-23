namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Enums;

public interface IAuditService
{
    Task LogAsync(string entityType, string entityId, AuditAction action, string correlationId, string? propertyName = null, string? oldValue = null, string? newValue = null);
    Task LogBatchAsync(IEnumerable<Domain.Entities.AuditLogEntry> entries);
}
