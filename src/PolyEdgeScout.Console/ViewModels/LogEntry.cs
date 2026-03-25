namespace PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Represents a single log entry for display in the full log view.
/// </summary>
public sealed record LogEntry(DateTime Timestamp, string Level, string Message);
