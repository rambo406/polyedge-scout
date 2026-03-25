namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// ViewModel for the edge backtest view, managing state and orchestrating backtest execution.
/// </summary>
public sealed class EdgeBacktestViewModel
{
    private readonly IEdgeBacktestService _edgeBacktestService;
    private readonly AppConfig _config;
    private CancellationTokenSource? _cts;

    /// <summary>Comma-separated symbol list (e.g. "BTC,ETH").</summary>
    public string Symbols { get; set; } = "BTC";

    /// <summary>Left-side timeframe for comparison.</summary>
    public string LeftTimeframe { get; set; } = "5m";

    /// <summary>Right-side timeframe for comparison.</summary>
    public string RightTimeframe { get; set; } = "15m";

    /// <summary>Whether a backtest is currently executing.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Results for the left timeframe after a completed run.</summary>
    public EdgeBacktestTimeframeResult? LeftTimeframeResult { get; private set; }

    /// <summary>Results for the right timeframe after a completed run.</summary>
    public EdgeBacktestTimeframeResult? RightTimeframeResult { get; private set; }

    /// <summary>Display names of all registered edge formulas.</summary>
    public List<string> AvailableFormulas { get; }

    /// <summary>Timeframe options available for selection.</summary>
    public List<string> AvailableTimeframes { get; } = ["1m", "5m", "15m", "1h"];

    /// <summary>Raised after each market is evaluated, providing incremental progress.</summary>
    public event EventHandler<EdgeBacktestProgressEvent>? ProgressUpdated;

    /// <summary>Raised when the backtest run completes (success or cancellation).</summary>
    public event EventHandler? BacktestCompleted;

    public EdgeBacktestViewModel(
        IEdgeBacktestService edgeBacktestService,
        AppConfig config,
        IEnumerable<IEdgeFormula> formulas)
    {
        _edgeBacktestService = edgeBacktestService;
        _config = config;
        AvailableFormulas = formulas.Select(f => f.Name).ToList();

        // Forward progress events from the service
        _edgeBacktestService.OnProgress += (s, e) => ProgressUpdated?.Invoke(this, e);
    }

    /// <summary>
    /// Validates entered symbols against the configured market filter keywords.
    /// Returns a list of symbols that did not match any keyword.
    /// </summary>
    public List<string> ValidateSymbols()
    {
        var symbols = Symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validKeywords = _config.MarketFilter.IncludeKeywords;
        var invalid = symbols
            .Where(s => !validKeywords.Any(k => k.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return invalid;
    }

    /// <summary>
    /// Parses the comma-separated <see cref="Symbols"/> string into a trimmed list.
    /// </summary>
    public List<string> ParseSymbols()
    {
        return Symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// <summary>
    /// Runs the edge backtest asynchronously for the selected symbols and timeframes.
    /// </summary>
    public async Task RunEdgeBacktestAsync()
    {
        if (IsRunning) return;

        var invalid = ValidateSymbols();
        if (invalid.Count > 0) return; // View should check ValidateSymbols() first

        IsRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _edgeBacktestService.RunEdgeBacktestAsync(
                ParseSymbols(), LeftTimeframe, RightTimeframe, _cts.Token);

            LeftTimeframeResult = result.LeftTimeframeResult;
            RightTimeframeResult = result.RightTimeframeResult;
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
            BacktestCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Cancels the currently running backtest, if any.
    /// </summary>
    public void CancelBacktest()
    {
        _cts?.Cancel();
    }
}
