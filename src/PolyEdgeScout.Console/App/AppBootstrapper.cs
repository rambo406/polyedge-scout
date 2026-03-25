namespace PolyEdgeScout.Console.App;

using Terminal.Gui.App;
using Terminal.Gui.Views;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Console.Views;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Manages the Terminal.Gui application lifecycle.
/// </summary>
public sealed class AppBootstrapper
{
    private readonly DashboardViewModel _dashboardVm;
    private readonly MarketTableView _marketTableView;
    private readonly PortfolioView _portfolioView;
    private readonly TradesView _tradesView;
    private readonly LogPanelView _logPanelView;
    private readonly ErrorIndicatorView _errorIndicatorView;
    private readonly FullLogView _fullLogView;
    private readonly TradesManagementView _tradesManagementView;
    private readonly BacktestView _backtestView;
    private readonly EdgeBacktestView _edgeBacktestView;
    private readonly ILogService _log;

    public AppBootstrapper(
        DashboardViewModel dashboardVm,
        MarketTableView marketTableView,
        PortfolioView portfolioView,
        TradesView tradesView,
        LogPanelView logPanelView,
        ErrorIndicatorView errorIndicatorView,
        FullLogView fullLogView,
        TradesManagementView tradesManagementView,
        BacktestView backtestView,
        EdgeBacktestView edgeBacktestView,
        ILogService log)
    {
        _dashboardVm = dashboardVm;
        _marketTableView = marketTableView;
        _portfolioView = portfolioView;
        _tradesView = tradesView;
        _logPanelView = logPanelView;
        _errorIndicatorView = errorIndicatorView;
        _fullLogView = fullLogView;
        _tradesManagementView = tradesManagementView;
        _backtestView = backtestView;
        _edgeBacktestView = edgeBacktestView;
        _log = log;
    }

    public void Run(CancellationToken ct)
    {
        using IApplication app = Application.Create().Init();

        var mainWindow = new MainWindow(
            _marketTableView,
            _portfolioView,
            _tradesView,
            _logPanelView,
            _errorIndicatorView,
            _fullLogView,
            _tradesManagementView,
            _backtestView,
            _edgeBacktestView);

        mainWindow.OnResetPaperTrading = () =>
        {
            var confirm = MessageBox.Query(app, "Reset Paper Trading",
                "Reset all paper trades and bankroll to $10,000?", "Yes", "No");
            if (confirm == 0)
            {
                _ = _dashboardVm.ResetPaperTradingAsync();
            }
        };

        var menuBar = MenuBarFactory.CreateMenuBar(_dashboardVm, mainWindow.Navigator, app, mainWindow.ShowHelp);
        var statusBar = MenuBarFactory.CreateStatusBar(_dashboardVm, mainWindow.Navigator);

        _dashboardVm.QuitRequested += () =>
        {
            app.Invoke(() => app.RequestStop());
        };

        // Sync initial log messages
        _dashboardVm.Log.SyncFromLogService();

        // Start the background scan loop
        var scanTask = Task.Run(() => _dashboardVm.RunScanLoopAsync(ct), ct);

        mainWindow.Add(menuBar, statusBar);
        app.Run(mainWindow);

        // After app.Run() returns, wait briefly for scan to wind down
        try
        {
            scanTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch { /* scan loop cancelled */ }
    }
}
