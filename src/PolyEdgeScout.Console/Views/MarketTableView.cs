namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Terminal.Gui view for the market scanner table.
/// Binds to MarketTableViewModel events.
/// </summary>
public sealed class MarketTableView : FrameView
{
    private readonly MarketTableViewModel _vm;
    private readonly TableView _table;

    public MarketTableView(MarketTableViewModel vm) : base()
    {
        Title = "Market Scanner";
        _vm = vm;

        _table = new TableView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            Table = new MarketTableSource(Array.Empty<MarketScanResult>())
        };

        _table.SelectedCellChanged += (sender, args) =>
        {
            _vm.SelectedIndex = _table.SelectedRow;
        };

        _table.CellActivated += (sender, args) =>
        {
            _ = Task.Run(async () =>
            {
                try { await _vm.ExecuteTradeAsync(); }
                catch { /* logged by VM */ }
            });
        };

        Add(_table);

        _vm.MarketsUpdated += OnMarketsUpdated;
    }

    private void OnMarketsUpdated()
    {
        Application.Invoke(() =>
        {
            _table.Table = new MarketTableSource(_vm.Markets);
            _table.Update();
        });
    }
}
