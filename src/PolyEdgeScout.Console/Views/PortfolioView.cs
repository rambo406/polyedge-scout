namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Terminal.Gui view for the portfolio summary.
/// </summary>
public sealed class PortfolioView : FrameView
{
    private readonly PortfolioViewModel _vm;
    private readonly Label _bankrollLabel;
    private readonly Label _openLabel;
    private readonly Label _unrealizedLabel;
    private readonly Label _realizedLabel;
    private readonly Label _totalLabel;

    public PortfolioView(PortfolioViewModel vm) : base()
    {
        Title = "Portfolio";
        _vm = vm;

        _bankrollLabel = new Label { Text = "Bankroll:     $10,000.00", X = 1, Y = 0 };
        _openLabel = new Label { Text = "Open:         0", X = 1, Y = 1 };
        _unrealizedLabel = new Label { Text = "Unrealized:   $0.00", X = 1, Y = 2 };
        _realizedLabel = new Label { Text = "Realized:     $0.00", X = 1, Y = 3 };
        _totalLabel = new Label { Text = "Total P&L:    $0.00", X = 1, Y = 4 };

        Add(_bankrollLabel, _openLabel, _unrealizedLabel, _realizedLabel, _totalLabel);

        _vm.SnapshotUpdated += OnSnapshotUpdated;
    }

    private void OnSnapshotUpdated()
    {
        App?.Invoke(() =>
        {
            var s = _vm.Snapshot;
            _bankrollLabel.Text = $"Bankroll:     ${s.Bankroll:N2}";
            _openLabel.Text = $"Open:         {s.OpenPositions}";
            _unrealizedLabel.Text = $"Unrealized:   {FormatPnl(s.UnrealizedPnl)}";
            _realizedLabel.Text = $"Realized:     {FormatPnl(s.RealizedPnl)}";
            _totalLabel.Text = $"Total P&L:    {FormatPnl(s.TotalPnl)}";
        });
    }

    private static string FormatPnl(double value) =>
        value >= 0 ? $"+${value:N2}" : $"-${Math.Abs(value):N2}";
}
