namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Console.App;

/// <summary>
/// Main Terminal.Gui window composing all child views.
/// </summary>
public sealed class MainWindow : Window, IShortcutHelpProvider
{
    /// <summary>
    /// Gets the view navigator for switching between dashboard and error log.
    /// </summary>
    public ViewNavigator Navigator { get; }

    /// <summary>
    /// Delegate invoked when the user presses Ctrl+Shift+R to reset paper trading.
    /// Wired by <see cref="PolyEdgeScout.Console.App.AppBootstrapper"/>.
    /// </summary>
    public Action? OnResetPaperTrading { get; set; }

    public MainWindow(
        MarketTableView marketTable,
        PortfolioView portfolio,
        TradesView trades,
        LogPanelView logPanel,
        ErrorIndicatorView errorIndicator,
        FullLogView fullLogView,
        TradesManagementView tradesManagementView,
        BacktestView backtestView,
        EdgeBacktestView edgeBacktestView) : base()
    {
        Title = "PolyEdge Scout v1.0";

        // Error indicator at top, hidden by default
        errorIndicator.X = 0;
        errorIndicator.Y = 0;
        errorIndicator.Width = Dim.Fill();

        // Market table: top-left, 65% width, 60% height
        marketTable.X = 0;
        marketTable.Y = Pos.Bottom(errorIndicator);
        marketTable.Width = Dim.Percent(65);
        marketTable.Height = Dim.Percent(60);

        // Portfolio: top-right
        portfolio.X = Pos.Right(marketTable);
        portfolio.Y = Pos.Bottom(errorIndicator);
        portfolio.Width = Dim.Fill();
        portfolio.Height = Dim.Percent(30);

        // Trades: below portfolio, right side
        trades.X = Pos.Right(marketTable);
        trades.Y = Pos.Bottom(portfolio);
        trades.Width = Dim.Fill();
        trades.Height = Dim.Percent(30);

        // Log panel: bottom, full width, remaining height
        logPanel.X = 0;
        logPanel.Y = Pos.Bottom(marketTable);
        logPanel.Width = Dim.Fill();
        logPanel.Height = Dim.Fill();

        // Full log view: full-page, initially hidden
        fullLogView.X = 0;
        fullLogView.Y = 0;
        fullLogView.Width = Dim.Fill();
        fullLogView.Height = Dim.Fill();
        fullLogView.Visible = false;

        // Trades management view: full-page, initially hidden
        tradesManagementView.X = 0;
        tradesManagementView.Y = 0;
        tradesManagementView.Width = Dim.Fill();
        tradesManagementView.Height = Dim.Fill();
        tradesManagementView.Visible = false;

        // Backtest view: full-page, initially hidden
        backtestView.X = 0;
        backtestView.Y = 0;
        backtestView.Width = Dim.Fill();
        backtestView.Height = Dim.Fill();
        backtestView.Visible = false;

        // Edge backtest view: full-page, initially hidden
        edgeBacktestView.X = 0;
        edgeBacktestView.Y = 0;
        edgeBacktestView.Width = Dim.Fill();
        edgeBacktestView.Height = Dim.Fill();
        edgeBacktestView.Visible = false;

        // Create view navigator
        var dashboardViews = new View[] { errorIndicator, marketTable, portfolio, trades, logPanel };
        Navigator = new ViewNavigator(dashboardViews);
        Navigator.RegisterView(ActiveView.FullLog, fullLogView);
        Navigator.RegisterView(ActiveView.TradesManagement, tradesManagementView);
        Navigator.RegisterView(ActiveView.Backtest, backtestView);
        Navigator.RegisterView(ActiveView.EdgeBacktest, edgeBacktestView);
        Navigator.DashboardProvider = this;
        fullLogView.Navigator = Navigator;
        tradesManagementView.Navigator = Navigator;
        backtestView.Navigator = Navigator;
        edgeBacktestView.Navigator = Navigator;

        Add(errorIndicator, marketTable, portfolio, trades, logPanel, fullLogView, tradesManagementView, backtestView, edgeBacktestView);
    }

    /// <summary>
    /// Shows the keyboard shortcuts help dialog for the currently active view.
    /// </summary>
    public void ShowHelp()
    {
        var viewShortcuts = Navigator.ActiveProvider?.GetShortcuts() ?? [];
        var allShortcuts = viewShortcuts.Concat(GlobalShortcuts.Items).ToList();
        var dialog = new ShortcutHelpDialog(allShortcuts);
        App?.Run(dialog);
        dialog.Dispose();
    }

    /// <inheritdoc />
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.F1)
        {
            ShowHelp();
            return true;
        }

        if (key == Key.O.WithCtrl)
        {
            Navigator.Show(ActiveView.TradesManagement);
            return true;
        }

        if (key == Key.R.WithCtrl.WithShift)
        {
            OnResetPaperTrading?.Invoke();
            return true;
        }

        if (key == Key.B.WithCtrl)
        {
            Navigator.Show(ActiveView.Backtest);
            return true;
        }

        if (key == Key.E.WithCtrl)
        {
            Navigator.Show(ActiveView.EdgeBacktest);
            return true;
        }

        return base.OnKeyDown(key);
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutHelpItem> GetShortcuts() =>
    [
        new("F5", "Refresh"),
        new("Ctrl+T", "Toggle Mode"),
        new("Ctrl+Shift+R", "Reset Paper Trading"),
        new("Ctrl+L", "Full Log"),
        new("Ctrl+O", "All Trades"),
        new("Ctrl+B", "Backtest"),
        new("Ctrl+E", "Edge Backtest")
    ];
}
