namespace PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Represents a single error or warning log entry for display in the Error Log view.
/// </summary>
public sealed record ErrorLogEntry(DateTime Timestamp, string Level, string Message);
