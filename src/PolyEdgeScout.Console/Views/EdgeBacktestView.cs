namespace PolyEdgeScout.Console.Views;

using System.Data;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Console.App;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Console.Views.Charts;
using PolyEdgeScout.Application.DTOs;

/// <summary>
/// Full-page view for running edge backtests and comparing formula performance
/// across two timeframes. Contains a config bar, status label, and a tabbed layout
/// with Metrics, Charts, and Details tabs.
/// </summary>
public sealed class EdgeBacktestView : FrameView, IShortcutHelpProvider
{
    private readonly EdgeBacktestViewModel _vm;

    // Config bar controls
    private readonly TextField _symbolsField;
    private readonly TextField _leftTimeframeField;
    private readonly TextField _rightTimeframeField;

    // Status
    private readonly Label _statusLabel;

    // Metrics tab — left timeframe
    private readonly TableView _leftFormulaTable;
    private readonly TableView _leftSymbolTable;
    private readonly Label _leftGrandTotalLabel;
    private readonly FrameView _leftMetricsFrame;

    // Metrics tab — right timeframe
    private readonly TableView _rightFormulaTable;
    private readonly TableView _rightSymbolTable;
    private readonly Label _rightGrandTotalLabel;
    private readonly FrameView _rightMetricsFrame;

    // Charts tab
    private readonly EquityCurveChart _leftEquityChart;
    private readonly WinRateBarChart _leftWinRateChart;
    private readonly EquityCurveChart _rightEquityChart;
    private readonly WinRateBarChart _rightWinRateChart;

    // Details tab
    private readonly TableView _detailsTable;

    // Tab view
    private readonly TabView _tabView;

    /// <summary>
    /// Gets or sets the view navigator for switching back to the dashboard.
    /// </summary>
    public ViewNavigator? Navigator { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EdgeBacktestView"/> class.
    /// </summary>
    /// <param name="vm">The edge backtest view model to bind to.</param>
    public EdgeBacktestView(EdgeBacktestViewModel vm) : base()
    {
        Title = "Edge Backtest";
        Visible = false;
        _vm = vm;

        // ── Config Bar (Y=0, Height=1) ──
        var configBar = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        var symbolsLabel = new Label { Text = "Symbols:", X = 0, Y = 0 };
        _symbolsField = new TextField
        {
            Text = _vm.Symbols,
            X = Pos.Right(symbolsLabel) + 1,
            Y = 0,
            Width = 16
        };

        var leftTfLabel = new Label { Text = "Left:", X = Pos.Right(_symbolsField) + 2, Y = 0 };
        _leftTimeframeField = new TextField
        {
            Text = _vm.LeftTimeframe,
            X = Pos.Right(leftTfLabel) + 1,
            Y = 0,
            Width = 6
        };

        var rightTfLabel = new Label { Text = "Right:", X = Pos.Right(_leftTimeframeField) + 2, Y = 0 };
        _rightTimeframeField = new TextField
        {
            Text = _vm.RightTimeframe,
            X = Pos.Right(rightTfLabel) + 1,
            Y = 0,
            Width = 6
        };

        configBar.Add(symbolsLabel, _symbolsField, leftTfLabel, _leftTimeframeField, rightTfLabel, _rightTimeframeField);

        // ── Status Label ──
        _statusLabel = new Label
        {
            Text = "Press Ctrl+R to run edge backtest.",
            X = 0,
            Y = Pos.Bottom(configBar),
            Width = Dim.Fill()
        };

        // ── Metrics Tab ──
        var metricsContainer = BuildMetricsTab(
            out _leftMetricsFrame,
            out _leftFormulaTable,
            out _leftSymbolTable,
            out _leftGrandTotalLabel,
            out _rightMetricsFrame,
            out _rightFormulaTable,
            out _rightSymbolTable,
            out _rightGrandTotalLabel);

        // ── Charts Tab ──
        var chartsContainer = BuildChartsTab(
            out _leftEquityChart,
            out _leftWinRateChart,
            out _rightEquityChart,
            out _rightWinRateChart);

        // ── Details Tab ──
        _detailsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = new DataTableSource(BuildDetailsDataTable(null, null))
        };

        var detailsContainer = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        detailsContainer.Add(_detailsTable);

        // ── TabView ──
        _tabView = new TabView
        {
            X = 0,
            Y = Pos.Bottom(_statusLabel) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var metricsTab = new Tab { DisplayText = "Metrics" };
        metricsTab.View = metricsContainer;

        var chartsTab = new Tab { DisplayText = "Charts" };
        chartsTab.View = chartsContainer;

        var detailsTab = new Tab { DisplayText = "Details" };
        detailsTab.View = detailsContainer;

        _tabView.AddTab(metricsTab, andSelect: true);
        _tabView.AddTab(chartsTab, andSelect: false);
        _tabView.AddTab(detailsTab, andSelect: false);

        Add(configBar, _statusLabel, _tabView);

        // Subscribe to VM events
        _vm.ProgressUpdated += OnProgressUpdated;
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
                SyncViewModelFromFields();
                _statusLabel.Text = "Running edge backtest...";
                _leftMetricsFrame.Title = $"{_vm.LeftTimeframe} — Starting...";
                _rightMetricsFrame.Title = $"{_vm.RightTimeframe} — Starting...";
                Task.Run(() => _vm.RunEdgeBacktestAsync());
            }

            return true;
        }

        return base.OnKeyDown(key);
    }

    /// <summary>
    /// Copies current text field values back to the view model before running.
    /// </summary>
    private void SyncViewModelFromFields()
    {
        _vm.Symbols = _symbolsField.Text;
        _vm.LeftTimeframe = _leftTimeframeField.Text;
        _vm.RightTimeframe = _rightTimeframeField.Text;
    }

    /// <summary>
    /// Handles incremental progress events from the view model.
    /// Updates the progress label in the appropriate timeframe column.
    /// </summary>
    private void OnProgressUpdated(object? sender, EdgeBacktestProgressEvent e)
    {
        App?.Invoke(() =>
        {
            var progress = $"Evaluating {e.EvaluatedCount}/{e.TotalCount} markets...";

            if (string.Equals(e.Timeframe, _vm.LeftTimeframe, StringComparison.OrdinalIgnoreCase))
            {
                _leftMetricsFrame.Title = $"{_vm.LeftTimeframe} — {progress}";
            }
            else
            {
                _rightMetricsFrame.Title = $"{_vm.RightTimeframe} — {progress}";
            }

            _statusLabel.Text = $"Running: {e.FormulaName} [{e.Timeframe}] — {e.EvaluatedCount}/{e.TotalCount}";
        });
    }

    /// <summary>
    /// Handles backtest completion by refreshing all tabs with final results.
    /// </summary>
    private void OnBacktestCompleted(object? sender, EventArgs e)
    {
        App?.Invoke(() =>
        {
            var left = _vm.LeftTimeframeResult;
            var right = _vm.RightTimeframeResult;

            // Update Metrics tab
            RefreshMetricsColumn(left, _leftMetricsFrame, _leftFormulaTable, _leftSymbolTable, _leftGrandTotalLabel);
            RefreshMetricsColumn(right, _rightMetricsFrame, _rightFormulaTable, _rightSymbolTable, _rightGrandTotalLabel);

            // Update Charts tab
            RefreshChartsColumn(left, _leftEquityChart, _leftWinRateChart);
            RefreshChartsColumn(right, _rightEquityChart, _rightWinRateChart);

            // Update Details tab
            _detailsTable.Table = new DataTableSource(BuildDetailsDataTable(left, right));

            var totalMarkets = (left?.TotalMarkets ?? 0) + (right?.TotalMarkets ?? 0);
            _statusLabel.Text = $"Edge backtest complete — {totalMarkets} total evaluations";
            _leftMetricsFrame.Title = left?.Timeframe ?? _vm.LeftTimeframe;
            _rightMetricsFrame.Title = right?.Timeframe ?? _vm.RightTimeframe;
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Metrics Tab Construction
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the Metrics tab container with dual-column layout for left and right timeframes.
    /// </summary>
    private static View BuildMetricsTab(
        out FrameView leftFrame,
        out TableView leftFormulaTable,
        out TableView leftSymbolTable,
        out Label leftGrandTotal,
        out FrameView rightFrame,
        out TableView rightFormulaTable,
        out TableView rightSymbolTable,
        out Label rightGrandTotal)
    {
        var container = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        leftFrame = new FrameView
        {
            Title = "Left TF",
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill()
        };

        leftFormulaTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(45),
            Table = new DataTableSource(BuildFormulaDataTable(null))
        };

        leftSymbolTable = new TableView
        {
            X = 0,
            Y = Pos.Bottom(leftFormulaTable),
            Width = Dim.Fill(),
            Height = Dim.Percent(45),
            Table = new DataTableSource(BuildSymbolDataTable(null))
        };

        leftGrandTotal = new Label
        {
            Text = "Grand Total: P&L —  ROI —",
            X = 1,
            Y = Pos.Bottom(leftSymbolTable),
            Width = Dim.Fill()
        };

        leftFrame.Add(leftFormulaTable, leftSymbolTable, leftGrandTotal);

        rightFrame = new FrameView
        {
            Title = "Right TF",
            X = Pos.Right(leftFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        rightFormulaTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(45),
            Table = new DataTableSource(BuildFormulaDataTable(null))
        };

        rightSymbolTable = new TableView
        {
            X = 0,
            Y = Pos.Bottom(rightFormulaTable),
            Width = Dim.Fill(),
            Height = Dim.Percent(45),
            Table = new DataTableSource(BuildSymbolDataTable(null))
        };

        rightGrandTotal = new Label
        {
            Text = "Grand Total: P&L —  ROI —",
            X = 1,
            Y = Pos.Bottom(rightSymbolTable),
            Width = Dim.Fill()
        };

        rightFrame.Add(rightFormulaTable, rightSymbolTable, rightGrandTotal);

        container.Add(leftFrame, rightFrame);
        return container;
    }

    // ═══════════════════════════════════════════════════════════════
    // Charts Tab Construction
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the Charts tab container with dual-column equity curves and win rate bars.
    /// </summary>
    private static View BuildChartsTab(
        out EquityCurveChart leftEquity,
        out WinRateBarChart leftWinRate,
        out EquityCurveChart rightEquity,
        out WinRateBarChart rightWinRate)
    {
        var container = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var leftFrame = new FrameView
        {
            Title = "Left TF Charts",
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill()
        };

        leftEquity = new EquityCurveChart
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50)
        };

        leftWinRate = new WinRateBarChart
        {
            X = 0,
            Y = Pos.Bottom(leftEquity),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        leftFrame.Add(leftEquity, leftWinRate);

        var rightFrame = new FrameView
        {
            Title = "Right TF Charts",
            X = Pos.Right(leftFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        rightEquity = new EquityCurveChart
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50)
        };

        rightWinRate = new WinRateBarChart
        {
            X = 0,
            Y = Pos.Bottom(rightEquity),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        rightFrame.Add(rightEquity, rightWinRate);

        container.Add(leftFrame, rightFrame);
        return container;
    }

    // ═══════════════════════════════════════════════════════════════
    // Data Refresh Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Refreshes one metrics column (formula table, symbol table, grand total) with timeframe results.
    /// </summary>
    private static void RefreshMetricsColumn(
        EdgeBacktestTimeframeResult? result,
        FrameView frame,
        TableView formulaTable,
        TableView symbolTable,
        Label grandTotal)
    {
        frame.Title = result?.Timeframe ?? "—";
        formulaTable.Table = new DataTableSource(BuildFormulaDataTable(result));
        symbolTable.Table = new DataTableSource(BuildSymbolDataTable(result));

        if (result is not null)
        {
            grandTotal.Text = $"Grand Total: P&L ${result.GrandTotalPnl:N2}  ROI {result.GrandTotalRoi:P1}";
        }
        else
        {
            grandTotal.Text = "Grand Total: P&L —  ROI —";
        }
    }

    /// <summary>
    /// Refreshes one charts column (equity curve + win rate bar) with timeframe results.
    /// </summary>
    private static void RefreshChartsColumn(
        EdgeBacktestTimeframeResult? result,
        EquityCurveChart equityChart,
        WinRateBarChart winRateChart)
    {
        if (result is null)
        {
            equityChart.SetData([]);
            winRateChart.SetData([]);
            return;
        }

        // Build equity curve data: cumulative P&L per formula
        var equityData = result.FormulaResults
            .Select(fr => (
                fr.FormulaName,
                CumulativePnL: BuildCumulativePnl(fr.Entries)))
            .ToList();

        equityChart.SetData(equityData);

        // Build win rate data
        var winRateData = result.FormulaResults
            .Select(fr => (fr.FormulaName, fr.WinRate))
            .ToList();

        winRateChart.SetData(winRateData);
    }

    /// <summary>
    /// Builds a cumulative P&amp;L series from individual backtest entries.
    /// </summary>
    private static List<double> BuildCumulativePnl(List<EdgeBacktestEntry> entries)
    {
        var cumulative = new List<double>();
        var running = 0.0;

        foreach (var entry in entries)
        {
            running += entry.PnL;
            cumulative.Add(running);
        }

        return cumulative;
    }

    // ═══════════════════════════════════════════════════════════════
    // DataTable Builders
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the formula comparison DataTable for the Metrics tab.
    /// Columns: Formula, Win%, Edge Acc., P&amp;L, ROI.
    /// </summary>
    private static DataTable BuildFormulaDataTable(EdgeBacktestTimeframeResult? result)
    {
        var dt = new DataTable();
        dt.Columns.Add("Formula", typeof(string));
        dt.Columns.Add("Win%", typeof(string));
        dt.Columns.Add("Edge Acc.", typeof(string));
        dt.Columns.Add("P&L", typeof(string));
        dt.Columns.Add("ROI", typeof(string));

        if (result is null || result.FormulaResults.Count == 0)
        {
            dt.Rows.Add("No data", "—", "—", "—", "—");
            return dt;
        }

        foreach (var fr in result.FormulaResults)
        {
            dt.Rows.Add(
                fr.FormulaName,
                fr.WinRate.ToString("P1"),
                fr.EdgeAccuracy.ToString("P1"),
                $"${fr.HypotheticalPnl:N2}",
                fr.Roi.ToString("P1"));
        }

        return dt;
    }

    /// <summary>
    /// Builds the per-symbol breakdown DataTable for the Metrics tab.
    /// Columns: Symbol, Markets, Win%, P&amp;L, ROI.
    /// </summary>
    private static DataTable BuildSymbolDataTable(EdgeBacktestTimeframeResult? result)
    {
        var dt = new DataTable();
        dt.Columns.Add("Symbol", typeof(string));
        dt.Columns.Add("Markets", typeof(string));
        dt.Columns.Add("Win%", typeof(string));
        dt.Columns.Add("P&L", typeof(string));
        dt.Columns.Add("ROI", typeof(string));

        if (result is null || result.SymbolResults.Count == 0)
        {
            dt.Rows.Add("No data", "—", "—", "—", "—");
            return dt;
        }

        foreach (var sr in result.SymbolResults)
        {
            dt.Rows.Add(
                sr.Symbol,
                sr.Markets.ToString(),
                sr.WinRate.ToString("P1"),
                $"${sr.PnL:N2}",
                sr.Roi.ToString("P1"));
        }

        return dt;
    }

    /// <summary>
    /// Builds the Details tab DataTable showing individual market x formula entries.
    /// Columns: Market, Formula, TF, Outcome, Edge, P&amp;L.
    /// </summary>
    private static DataTable BuildDetailsDataTable(
        EdgeBacktestTimeframeResult? left,
        EdgeBacktestTimeframeResult? right)
    {
        var dt = new DataTable();
        dt.Columns.Add("Market", typeof(string));
        dt.Columns.Add("Formula", typeof(string));
        dt.Columns.Add("TF", typeof(string));
        dt.Columns.Add("Outcome", typeof(string));
        dt.Columns.Add("Edge", typeof(string));
        dt.Columns.Add("P&L", typeof(string));

        var allEntries = new List<EdgeBacktestEntry>();

        if (left is not null)
        {
            allEntries.AddRange(left.FormulaResults.SelectMany(fr => fr.Entries));
        }

        if (right is not null)
        {
            allEntries.AddRange(right.FormulaResults.SelectMany(fr => fr.Entries));
        }

        if (allEntries.Count == 0)
        {
            dt.Rows.Add("No signal data. Press Ctrl+R to run.", "", "", "", "", "");
            return dt;
        }

        foreach (var entry in allEntries)
        {
            dt.Rows.Add(
                TruncateMarket(entry.MarketQuestion),
                entry.FormulaName,
                entry.Timeframe,
                entry.ModelCorrect ? "✅" : "❌",
                entry.Edge.ToString("P1"),
                $"${entry.PnL:N2}");
        }

        return dt;
    }

    /// <summary>
    /// Truncates a market question string for table display.
    /// </summary>
    private static string TruncateMarket(string question) =>
        question.Length > 40 ? question[..39] + "…" : question;

    /// <inheritdoc />
    public IReadOnlyList<ShortcutHelpItem> GetShortcuts() =>
    [
        new("Escape", "Return to Dashboard"),
        new("Ctrl+R", "Run Edge Backtest")
    ];
}
