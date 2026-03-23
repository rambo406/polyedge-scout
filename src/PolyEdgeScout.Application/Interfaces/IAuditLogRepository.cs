namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry);
    Task AddRangeAsync(IEnumerable<AuditLogEntry> entries);
    Task<List<AuditLogEntry>> GetByEntityAsync(string entityType, string entityId);
    Task<List<AuditLogEntry>> GetByCorrelationIdAsync(string correlationId);
    Task<List<AuditLogEntry>> GetByDateRangeAsync(DateTime from, DateTime to);
}
