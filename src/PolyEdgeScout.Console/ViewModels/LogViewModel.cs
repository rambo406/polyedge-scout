namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// ViewModel for the log panel.
/// Maintains a bounded buffer of log messages.
/// </summary>
public sealed class LogViewModel
{
    private const int MaxMessages = 500;
    private readonly List<string> _messages = [];
    private readonly ILogService _logService;

    public IReadOnlyList<string> Messages => _messages;

    public event Action? MessageAdded;

    public LogViewModel(ILogService logService)
    {
        _logService = logService;
    }

    public void AddMessage(string message)
    {
        var timestamped = $"[{DateTime.UtcNow:HH:mm:ss}] {message}";
        _messages.Add(timestamped);

        while (_messages.Count > MaxMessages)
            _messages.RemoveAt(0);

        MessageAdded?.Invoke();
    }

    /// <summary>
    /// Syncs the log buffer with the underlying ILogService.RecentMessages.
    /// Call during initialization or refresh.
    /// </summary>
    public void SyncFromLogService()
    {
        _messages.Clear();
        foreach (var msg in _logService.RecentMessages)
            _messages.Add(msg);
        MessageAdded?.Invoke();
    }
}
