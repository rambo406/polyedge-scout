namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Root ViewModel for the dashboard. Orchestrates child ViewModels 
/// and runs the background scan loop.
/// </summary>
public sealed class DashboardViewModel
{
    private readonly IScanOrchestrationService _orchestrator;
    private readonly IOrderService _orderService;
    private readonly AppConfig _config;
    private readonly ILogService _log;

    public MarketTableViewModel MarketTable { get; }
    public PortfolioViewModel Portfolio { get; }
    public TradesViewModel Trades { get; }
    public TradesManagementViewModel TradesManagement { get; }
    public LogViewModel Log { get; }

    public bool IsScanning { get; private set; }
    public DateTime LastScanTime { get; private set; } = DateTime.MinValue;
    public bool PaperMode => _orderService.PaperMode;

    /// <summary>Most recent scan error message, or <c>null</c> when the last scan succeeded.</summary>
    public string? LastError { get; private set; }

    public event Action? ScanStatusChanged;
    public event Action? ModeChanged;
    public event Action? ErrorChanged;
    public event Action? QuitRequested;

    public DashboardViewModel(
        IScanOrchestrationService orchestrator,
        IOrderService orderService,
        AppConfig config,
        ILogService log,
        MarketTableViewModel marketTable,
        PortfolioViewModel portfolio,
        TradesViewModel trades,
        TradesManagementViewModel tradesManagement,
        LogViewModel logVm)
    {
        _orchestrator = orchestrator;
        _orderService = orderService;
        _config = config;
        _log = log;
        MarketTable = marketTable;
        Portfolio = portfolio;
        Trades = trades;
        TradesManagement = tradesManagement;
        Log = logVm;
    }

    /// <summary>
    /// Runs the continuous scan loop. Call from a background task.
    /// </summary>
    public async Task RunScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            IsScanning = true;
            ScanStatusChanged?.Invoke();

            try
            {
                var results = await _orchestrator.ScanEvaluateAndAutoTradeAsync(ct);
                MarketTable.UpdateMarkets(results);

                var pnl = _orderService.GetPnlSnapshot();
                Portfolio.UpdateSnapshot(pnl);
                Trades.UpdateTrades(pnl.LastTrades);
                TradesManagement.RefreshTrades();

                LastScanTime = DateTime.UtcNow;
                Log.AddMessage("Scan cycle completed");

                LastError = null;
                ErrorChanged?.Invoke();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _log.Error("Scan cycle failed", ex);
                Log.AddMessage($"ERROR: Scan failed — {ex.Message}");

                LastError = ex.Message;
                ErrorChanged?.Invoke();
            }
            finally
            {
                IsScanning = false;
                ScanStatusChanged?.Invoke();
            }

            // Wait for the configured interval
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.ScanIntervalSeconds), ct);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    public void ToggleMode()
    {
        _orderService.PaperMode = !_orderService.PaperMode;
        var mode = _orderService.PaperMode ? "PAPER" : "LIVE";
        _log.Info($"Mode toggled to {mode}");
        Log.AddMessage($"Mode changed to {mode}");
        ModeChanged?.Invoke();
    }

    public void RequestRefresh()
    {
        _log.Info("Manual refresh requested");
        Log.AddMessage("Manual refresh requested");
    }

    public void RequestQuit()
    {
        QuitRequested?.Invoke();
    }

    /// <summary>
    /// Resets paper trading state.
    /// Clears all trades, resets bankroll, and refreshes views.
    /// </summary>
    public async Task ResetPaperTradingAsync()
    {
        if (!PaperMode)
        {
            Log.AddMessage("Reset is only available in Paper mode");
            return;
        }

        try
        {
            await _orderService.ResetPaperTradingAsync();

            var pnl = _orderService.GetPnlSnapshot();
            Portfolio.UpdateSnapshot(pnl);
            Trades.UpdateTrades(pnl.LastTrades);
            TradesManagement.RefreshTrades();

            Log.AddMessage("\u2713 Paper trading reset: bankroll restored to $10,000");
        }
        catch (Exception ex)
        {
            _log.Error("Paper trading reset failed", ex);
            Log.AddMessage($"ERROR: Reset failed \u2014 {ex.Message}");
        }
    }
}
