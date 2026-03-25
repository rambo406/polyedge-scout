namespace PolyEdgeScout.Console.ViewModels;

/// <summary>
/// View model that maintains a bounded buffer of ALL log entries
/// for display in the full-page log view.
/// </summary>
public sealed class FullLogViewModel
{
    private const int MaxEntries = 500;
    private readonly List<LogEntry> _entries = [];
    private readonly object _lock = new();

    /// <summary>
    /// Gets a snapshot of the current log entries (thread-safe).
    /// </summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether word wrap is enabled for log entry display.
    /// </summary>
    public bool WordWrap { get; private set; }

    /// <summary>
    /// Raised when a new entry is added to the log.
    /// </summary>
    public event Action? EntryAdded;

    /// <summary>
    /// Raised when the <see cref="WordWrap"/> setting changes.
    /// </summary>
    public event Action? WordWrapChanged;

    /// <summary>
    /// Toggles the <see cref="WordWrap"/> setting and raises <see cref="WordWrapChanged"/>.
    /// </summary>
    public void ToggleWordWrap()
    {
        WordWrap = !WordWrap;
        WordWrapChanged?.Invoke();
    }

    /// <summary>
    /// Returns the message text for the entry at the given index,
    /// or <see langword="null"/> if the index is out of range.
    /// </summary>
    /// <param name="index">Zero-based index of the entry.</param>
    public string? GetSelectedEntryText(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _entries.Count)
                return null;

            return _entries[index].Message;
        }
    }

    /// <summary>
    /// Returns all entry messages joined by <see cref="Environment.NewLine"/>,
    /// or <see langword="null"/> if the list is empty.
    /// </summary>
    public string? GetAllEntriesText()
    {
        lock (_lock)
        {
            if (_entries.Count == 0)
                return null;

            return string.Join(Environment.NewLine, _entries.Select(e => e.Message));
        }
    }

    /// <summary>
    /// Handles an incoming log entry. Accepts ALL levels.
    /// </summary>
    /// <param name="level">The log level (e.g. "ERR", "WRN", "INF", "DBG").</param>
    /// <param name="formattedLine">The fully formatted log line.</param>
    public void OnLogEntry(string level, string formattedLine)
    {
        lock (_lock)
        {
            var entry = new LogEntry(DateTime.UtcNow, level, formattedLine);
            _entries.Add(entry);

            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
        }

        EntryAdded?.Invoke();
    }
}
