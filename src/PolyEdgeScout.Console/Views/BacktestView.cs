namespace PolyEdgeScout.Console.Views;

using System.Data;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Console.App;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Full-page view for running backtests and displaying results.
/// Shows summary metrics, a status indicator, and a detailed results table.
/// </summary>
public sealed class BacktestView : FrameView, IShortcutHelpProvider
{
    private readonly BacktestViewModel _vm;
    private readonly FrameView _summaryPanel;
    private readonly Label _totalMarketsLabel;
    private readonly Label _marketsWithEdgeLabel;
    private readonly Label _brierScoreLabel;
    private readonly Label _winRateLabel;
    private readonly Label _edgeAccuracyLabel;
    private readonly Label _hypotheticalPnlLabel;
    private readonly Label _statusLabel;
    private readonly TableView _tableView;

    /// <summary>
    /// Gets or sets the view navigator for switching back to the dashboard.
    /// </summary>
    public ViewNavigator? Navigator { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestView"/> class.
    /// </summary>
    /// <param name="vm">The backtest view model to bind to.</param>
    public BacktestView(BacktestViewModel vm) : base()
    {
        Title = "Backtest";
        Visible = false;
        _vm = vm;

        // --- Summary panel (top ~6 rows) ---
        _summaryPanel = new FrameView
        {
            Title = "Summary",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 6
        };

        // Left column labels
        _totalMarketsLabel = new Label
        {
            Text = "Total Markets: —",
            X = 1,
            Y = 0
        };

        _marketsWithEdgeLabel = new Label
        {
            Text = "Markets with Edge: —",
            X = 1,
            Y = 1
        };

        _brierScoreLabel = new Label
        {
            Text = "Brier Score: —",
            X = 1,
            Y = 2
        };

        // Right column labels
        _winRateLabel = new Label
        {
            Text = "Win Rate: —",
            X = 40,
            Y = 0
        };

        _edgeAccuracyLabel = new Label
        {
            Text = "Edge Accuracy: —",
            X = 40,
            Y = 1
        };

        _hypotheticalPnlLabel = new Label
        {
            Text = "Hypothetical P&L: —",
            X = 40,
            Y = 2
        };

        _summaryPanel.Add(
            _totalMarketsLabel,
            _marketsWithEdgeLabel,
            _brierScoreLabel,
            _winRateLabel,
            _edgeAccuracyLabel,
            _hypotheticalPnlLabel
        );

        // --- Status label ---
        _statusLabel = new Label
        {
            Text = "Press Ctrl+R to run a backtest.",
            X = 0,
            Y = Pos.Bottom(_summaryPanel),
            Width = Dim.Fill()
        };

        // --- Results table ---
        _tableView = new TableView
        {
            X = 0,
            Y = Pos.Bottom(_statusLabel) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = new DataTableSource(BuildResultsTable())
        };

        Add(_summaryPanel);
        Add(_statusLabel);
        Add(_tableView);

        _vm.BacktestCompleted += OnBacktestCompleted;
    }

    /// <inheritdoc />
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Esc)
        {
            Navigator?.ShowDashboard();
            return true;
        }

        if (key == Key.R.WithCtrl)
        {
            if (!_vm.IsRunning)
            {
                _statusLabel.Text = "Running backtest...";
                Task.Run(() => _vm.RunBacktestAsync());
            }

            return true;
        }

        return base.OnKeyDown(key);
    }

    private void OnBacktestCompleted()
    {
        App?.Invoke(() =>
        {
            RefreshSummary();
            _tableView.Table = new DataTableSource(BuildResultsTable());

            var totalMarkets = _vm.Results?.TotalMarkets ?? 0;
            _statusLabel.Text = $"Backtest complete — {totalMarkets} markets evaluated";
        });
    }

    private void RefreshSummary()
    {
        var r = _vm.Results;
        if (r is null)
        {
            _totalMarketsLabel.Text = "Total Markets: —";
            _marketsWithEdgeLabel.Text = "Markets with Edge: —";
            _brierScoreLabel.Text = "Brier Score: —";
            _winRateLabel.Text = "Win Rate: —";
            _edgeAccuracyLabel.Text = "Edge Accuracy: —";
            _hypotheticalPnlLabel.Text = "Hypothetical P&L: —";
            return;
        }

        _totalMarketsLabel.Text = $"Total Markets: {r.TotalMarkets}";
        _marketsWithEdgeLabel.Text = $"Markets with Edge: {r.MarketsWithEdge}";
        _brierScoreLabel.Text = $"Brier Score: {r.BrierScore:N4}";
        _winRateLabel.Text = $"Win Rate: {r.WinRate:P1}";
        _edgeAccuracyLabel.Text = $"Edge Accuracy: {r.EdgeAccuracy:P1}";
        _hypotheticalPnlLabel.Text = $"Hypothetical P&L: ${r.HypotheticalPnl:N2}";
    }

    private DataTable BuildResultsTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("Market", typeof(string));
        dt.Columns.Add("Model", typeof(string));
        dt.Columns.Add("Market Price", typeof(string));
        dt.Columns.Add("Edge", typeof(string));
        dt.Columns.Add("Actual", typeof(string));
        dt.Columns.Add("Correct", typeof(string));

        var entries = _vm.Results?.Entries;
        if (entries is null || entries.Count == 0)
        {
            dt.Rows.Add("No backtest results. Press Ctrl+R to run.", "", "", "", "", "");
            return dt;
        }

        foreach (var e in entries)
        {
            dt.Rows.Add(
                TruncateMarket(e.MarketQuestion),
                e.ModelProbability.ToString("P1"),
                e.MarketYesPrice.ToString("P1"),
                e.Edge.ToString("P1"),
                e.ActualOutcome.ToString("N2"),
                e.ModelCorrect ? "✓" : "✗"
            );
        }

        return dt;
    }

    private static string TruncateMarket(string question) =>
        question.Length > 40 ? question[..39] + "…" : question;

    /// <inheritdoc />
    public IReadOnlyList<ShortcutHelpItem> GetShortcuts() =>
    [
        new("Escape", "Return to Dashboard"),
        new("Ctrl+R", "Run Backtest")
    ];
}
