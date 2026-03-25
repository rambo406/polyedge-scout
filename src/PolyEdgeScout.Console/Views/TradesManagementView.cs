namespace PolyEdgeScout.Console.Views;

using System.Data;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Console.App;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Full-page view for managing all trades — open and settled.
/// Displays tabbed tables with detailed trade information.
/// </summary>
public sealed class TradesManagementView : FrameView, IShortcutHelpProvider
{
    private readonly TradesManagementViewModel _vm;
    private readonly TableView _openTradesTable;
    private readonly TableView _settledTradesTable;
    private readonly TabView _tabView;

    /// <summary>
    /// Gets or sets the view navigator for switching back to the dashboard.
    /// </summary>
    public ViewNavigator? Navigator { get; set; }

    public TradesManagementView(TradesManagementViewModel vm) : base()
    {
        Title = "Trades Management";
        Visible = false;
        _vm = vm;

        _openTradesTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = new DataTableSource(BuildOpenTradesTable())
        };

        _settledTradesTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = new DataTableSource(BuildSettledTradesTable())
        };

        _tabView = new TabView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var openTab = new Tab { DisplayText = "Open Trades" };
        openTab.View = _openTradesTable;

        var settledTab = new Tab { DisplayText = "Settled Trades" };
        settledTab.View = _settledTradesTable;

        _tabView.AddTab(openTab, andSelect: true);
        _tabView.AddTab(settledTab, andSelect: false);

        Add(_tabView);

        _vm.TradesUpdated += OnTradesUpdated;
    }

    /// <inheritdoc />
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Esc)
        {
            Navigator?.ShowDashboard();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private void OnTradesUpdated()
    {
        App?.Invoke(() =>
        {
            _openTradesTable.Table = new DataTableSource(BuildOpenTradesTable());
            _settledTradesTable.Table = new DataTableSource(BuildSettledTradesTable());
        });
    }

    private DataTable BuildOpenTradesTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("Market", typeof(string));
        dt.Columns.Add("Action", typeof(string));
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Entry", typeof(string));
        dt.Columns.Add("Shares", typeof(string));
        dt.Columns.Add("Outlay", typeof(string));
        dt.Columns.Add("Edge", typeof(string));
        dt.Columns.Add("Time", typeof(string));

        if (_vm.OpenTrades.Count == 0)
        {
            dt.Rows.Add("No open trades", "", "", "", "", "", "", "");
            return dt;
        }

        foreach (var t in _vm.OpenTrades)
        {
            dt.Rows.Add(
                TruncateMarket(t.MarketQuestion),
                t.Action.ToString(),
                t.Status.ToString(),
                $"${t.EntryPrice:N2}",
                t.Shares.ToString("N2"),
                $"${t.Outlay:N2}",
                t.Edge.ToString("P1"),
                t.Timestamp.ToString("g")
            );
        }

        return dt;
    }

    private DataTable BuildSettledTradesTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("Market", typeof(string));
        dt.Columns.Add("Result", typeof(string));
        dt.Columns.Add("Entry", typeof(string));
        dt.Columns.Add("Shares", typeof(string));
        dt.Columns.Add("Net P&L", typeof(string));
        dt.Columns.Add("ROI", typeof(string));
        dt.Columns.Add("Settled", typeof(string));

        if (_vm.SettledTrades.Count == 0)
        {
            dt.Rows.Add("No settled trades", "", "", "", "", "", "");
            return dt;
        }

        foreach (var t in _vm.SettledTrades)
        {
            dt.Rows.Add(
                TruncateMarket(t.MarketQuestion),
                t.Won ? "Won ✓" : "Lost ✗",
                $"${t.EntryPrice:N2}",
                t.Shares.ToString("N2"),
                $"${t.NetProfit:N2}",
                t.Roi.ToString("P1"),
                t.SettledAt.ToString("g")
            );
        }

        return dt;
    }

    private static string TruncateMarket(string question) =>
        question.Length > 40 ? question[..39] + "…" : question;

    /// <inheritdoc />
    public IReadOnlyList<ShortcutHelpItem> GetShortcuts() =>
    [
        new("Escape", "Return to Dashboard")
    ];
}
