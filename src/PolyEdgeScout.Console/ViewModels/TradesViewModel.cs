namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// ViewModel for the recent trades panel.
/// Holds a list of recent trade results.
/// </summary>
public sealed class TradesViewModel
{
    private IReadOnlyList<TradeResult> _recentTrades = [];

    public IReadOnlyList<TradeResult> RecentTrades => _recentTrades;

    public event Action? TradesUpdated;

    public void UpdateTrades(IReadOnlyList<TradeResult> trades)
    {
        _recentTrades = trades;
        TradesUpdated?.Invoke();
    }
}
