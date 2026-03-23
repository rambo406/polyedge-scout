namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;

/// <summary>
/// ViewModel for backtest execution and results.
/// </summary>
public sealed class BacktestViewModel
{
    private readonly IBacktestService _backtestService;

    public BacktestResult? Results { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action? BacktestCompleted;

    public BacktestViewModel(IBacktestService backtestService)
    {
        _backtestService = backtestService;
    }

    public async Task RunBacktestAsync(CancellationToken ct = default)
    {
        IsRunning = true;
        try
        {
            Results = await _backtestService.RunBacktestAsync(ct);
        }
        finally
        {
            IsRunning = false;
            BacktestCompleted?.Invoke();
        }
    }
}
