namespace PolyEdgeScout.Console.ViewModels;

/// <summary>
/// View model that maintains a bounded buffer of error and warning log entries
/// for display in the Error Log view.
/// </summary>
public sealed class ErrorLogViewModel
{
    private const int MaxEntries = 500;
    private readonly List<ErrorLogEntry> _entries = [];

    /// <summary>
    /// Gets the current list of error/warning log entries.
    /// </summary>
    public IReadOnlyList<ErrorLogEntry> Entries => _entries;

    /// <summary>
    /// Raised when a new entry is added to the log.
    /// </summary>
    public event Action? EntryAdded;

    /// <summary>
    /// Handles an incoming log entry, filtering for ERR and WRN levels only.
    /// </summary>
    /// <param name="level">The log level (e.g. "ERR", "WRN", "INF", "DBG").</param>
    /// <param name="formattedLine">The fully formatted log line.</param>
    public void OnLogEntry(string level, string formattedLine)
    {
        if (level is not "ERR" and not "WRN")
            return;

        var entry = new ErrorLogEntry(DateTime.UtcNow, level, formattedLine);
        _entries.Add(entry);

        while (_entries.Count > MaxEntries)
            _entries.RemoveAt(0);

        EntryAdded?.Invoke();
    }
}
