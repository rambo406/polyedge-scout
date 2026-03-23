namespace PolyEdgeScout.Domain.Entities;

using PolyEdgeScout.Domain.Enums;

public sealed record AuditLogEntry
{
    public long Id { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string EntityType { get; init; } = "";
    public string EntityId { get; init; } = "";
    public AuditAction Action { get; init; }
    public string UserId { get; init; } = "system";
    public string? PropertyName { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
}
