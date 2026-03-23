namespace PolyEdgeScout.Infrastructure.Logging;

using System.Globalization;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Thread-safe logging service that writes to both console (via a circular buffer
/// for TUI display) and daily rotating log files.
/// </summary>
public sealed class FileLogService : ILogService, IDisposable
{
    private const int MaxBufferSize = 100;

    private readonly string _logDirectory;
    private readonly List<string> _buffer = new(MaxBufferSize);
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string _currentLogDate = "";

    /// <summary>
    /// Initializes the log service, creating the log directory if it does not exist.
    /// </summary>
    /// <param name="logDirectory">Directory path for daily log files.</param>
    public FileLogService(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Gets the most recent log messages (up to 100) for TUI rendering.
    /// </summary>
    public IReadOnlyList<string> RecentMessages
    {
        get
        {
            lock (_lock)
            {
                return _buffer.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void Info(string message) => Write("INF", message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warn(string message) => Write("WRN", message);

    /// <summary>
    /// Logs an error message with an optional exception.
    /// </summary>
    public void Error(string message, Exception? ex = null)
    {
        string full = ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}";
        Write("ERR", full);
    }

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    public void Debug(string message) => Write("DBG", message);

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void Write(string level, string message)
    {
        string timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string line = $"[{timestamp}] [{level}] {message}";

        lock (_lock)
        {
            // Circular buffer — remove oldest when full
            if (_buffer.Count >= MaxBufferSize)
                _buffer.RemoveAt(0);
            _buffer.Add(line);

            // Console output
            Console.WriteLine(line);

            // File output — rotate daily
            EnsureWriter();
            _writer!.WriteLine(line);
            _writer.Flush();
        }
    }

    private void EnsureWriter()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (today == _currentLogDate && _writer is not null)
            return;

        _writer?.Flush();
        _writer?.Dispose();

        string path = Path.Combine(_logDirectory, $"polyedge-scout-{today}.log");
        _writer = new StreamWriter(path, append: true) { AutoFlush = false };
        _currentLogDate = today;
    }
}
