namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using PolyEdgeScout.Console.App;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Creates the MenuBar and StatusBar for the dashboard.
/// </summary>
public static class MenuBarFactory
{
    public static MenuBar CreateMenuBar(DashboardViewModel vm, ViewNavigator navigator, IApplication app, Action? showHelp = null)
    {
        return new MenuBar(new MenuBarItem[]
        {
            new ("_File", new MenuItem[]
            {
                new ("_Quit", "Ctrl+Q", () => vm.RequestQuit(), Key.Q.WithCtrl)
            }),
            new ("_View", new MenuItem[]
            {
                new ("_Refresh", "F5", () => vm.RequestRefresh(), Key.F5),
                new ("Full _Log", "Ctrl+L", () => navigator.ShowFullLog(), Key.L.WithCtrl)
            }),
            new ("_Trading", new MenuItem[]
            {
                new ("_Toggle Mode", "Ctrl+T", () => vm.ToggleMode(), Key.T.WithCtrl),
                new ("_Reset Paper", "Ctrl+Shift+R", () =>
                {
                    var confirm = MessageBox.Query(app, "Reset Paper Trading",
                        "Reset all paper trades and bankroll to $10,000?", "Yes", "No");
                    if (confirm == 0)
                    {
                        _ = vm.ResetPaperTradingAsync();
                    }
                }, Key.R.WithCtrl.WithShift)
            }),
            new ("_Help", new MenuItem[]
            {
                new ("_Keyboard Shortcuts", "F1", () => showHelp?.Invoke(), Key.F1)
            })
        });
    }

    public static StatusBar CreateStatusBar(DashboardViewModel vm, ViewNavigator navigator)
    {
        var modeItem = new Shortcut(Key.T.WithCtrl, "Mode: PAPER", () => vm.ToggleMode());
        var bankrollItem = new Shortcut(Key.Empty, "Bankroll: $10,000.00", null);
        var scanItem = new Shortcut(Key.F5, "Refresh", () => vm.RequestRefresh());
        var logItem = new Shortcut(Key.L.WithCtrl, "Full Log", () => navigator.ShowFullLog());
        var quitItem = new Shortcut(Key.Q.WithCtrl, "Quit", () => vm.RequestQuit());

        var statusBar = new StatusBar(new Shortcut[] { modeItem, bankrollItem, scanItem, logItem, quitItem });

        // Update status bar when mode changes
        vm.ModeChanged += () =>
        {
            statusBar.App?.Invoke(() =>
            {
                modeItem.Title = vm.PaperMode
                    ? "Mode: PAPER"
                    : "Mode: LIVE";
                statusBar.SetNeedsDraw();
            });
        };

        // Update status bar when scan status changes
        vm.ScanStatusChanged += () =>
        {
            statusBar.App?.Invoke(() =>
            {
                var lastScan = vm.LastScanTime == DateTime.MinValue
                    ? "Never"
                    : $"{(DateTime.UtcNow - vm.LastScanTime).TotalSeconds:N0}s ago";
                scanItem.Title = vm.IsScanning
                    ? "Scanning..."
                    : $"Last: {lastScan}";
                statusBar.SetNeedsDraw();
            });
        };

        // Update bankroll from portfolio updates
        vm.Portfolio.SnapshotUpdated += () =>
        {
            statusBar.App?.Invoke(() =>
            {
                bankrollItem.Title = $"Bankroll: ${vm.Portfolio.Snapshot.Bankroll:N2}";
                statusBar.SetNeedsDraw();
            });
        };

        return statusBar;
    }
}
