namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Collections.ObjectModel;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Terminal.Gui view for recent trades.
/// </summary>
public sealed class TradesView : FrameView
{
    private readonly TradesViewModel _vm;
    private readonly ListView _listView;
    private readonly List<string> _items = [];

    public TradesView(TradesViewModel vm) : base()
    {
        Title = "Recent Settlements";
        _vm = vm;

        _listView = new ListView
        {
            Source = new ListWrapper<string>(new ObservableCollection<string>(_items)),
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_listView);

        _vm.TradesUpdated += OnTradesUpdated;
    }

    private void OnTradesUpdated()
    {
        App?.Invoke(() =>
        {
            _items.Clear();
            foreach (var t in _vm.RecentTrades.Take(5))
            {
                var icon = t.Won ? "✓" : "✗";
                var question = t.MarketQuestion.Length > 25
                    ? t.MarketQuestion[..24] + "…"
                    : t.MarketQuestion;
                var pnl = t.NetProfit >= 0 ? $"+${t.NetProfit:N2}" : $"-${Math.Abs(t.NetProfit):N2}";
                _items.Add($"{icon} {question}  {pnl}  {t.Roi:P0}");
            }
            if (_items.Count == 0)
            {
                _items.Add("No settlements yet");
                _items.Add("(Ctrl+O for all trades)");
            }
            _listView.Source = new ListWrapper<string>(new ObservableCollection<string>(_items));
        });
    }
}
