namespace PolyEdgeScout.Domain.Entities;

public sealed record AppStateEntry
{
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
}
