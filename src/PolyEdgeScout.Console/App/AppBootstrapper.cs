namespace PolyEdgeScout.Console.App;

using Terminal.Gui;
using Terminal.Gui.App;
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
    private readonly ILogService _log;

    public AppBootstrapper(
        DashboardViewModel dashboardVm,
        MarketTableView marketTableView,
        PortfolioView portfolioView,
        TradesView tradesView,
        LogPanelView logPanelView,
        ILogService log)
    {
        _dashboardVm = dashboardVm;
        _marketTableView = marketTableView;
        _portfolioView = portfolioView;
        _tradesView = tradesView;
        _logPanelView = logPanelView;
        _log = log;
    }

    public void Run(CancellationToken ct)
    {
        Application.Init();

        try
        {
            var mainWindow = new MainWindow(
                _marketTableView,
                _portfolioView,
                _tradesView,
                _logPanelView);

            var menuBar = MenuBarFactory.CreateMenuBar(_dashboardVm);
            var statusBar = MenuBarFactory.CreateStatusBar(_dashboardVm);

            _dashboardVm.QuitRequested += () =>
            {
                Application.Invoke(() => Application.RequestStop());
            };

            // Sync initial log messages
            _dashboardVm.Log.SyncFromLogService();

            // Start the background scan loop
            var scanTask = Task.Run(() => _dashboardVm.RunScanLoopAsync(ct), ct);

            mainWindow.Add(menuBar, statusBar);
            Application.Run(mainWindow);

            // After Application.Run() returns, wait briefly for scan to wind down
            try
            {
                scanTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch { /* scan loop cancelled */ }
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
