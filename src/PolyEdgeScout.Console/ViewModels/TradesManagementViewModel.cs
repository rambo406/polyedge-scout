namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;

/// <summary>
/// View model for the full-page trades management view.
/// Provides access to all open trades and settled trade results.
/// </summary>
public sealed class TradesManagementViewModel
{
    private readonly IOrderService _orderService;

    private IReadOnlyList<Trade> _openTrades = [];
    private IReadOnlyList<TradeResult> _settledTrades = [];

    /// <summary>Gets the current list of open trades.</summary>
    public IReadOnlyList<Trade> OpenTrades => _openTrades;

    /// <summary>Gets the current list of settled trade results.</summary>
    public IReadOnlyList<TradeResult> SettledTrades => _settledTrades;

    /// <summary>Raised when trade data has been refreshed.</summary>
    public event Action? TradesUpdated;

    public TradesManagementViewModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Refreshes trade data from the order service.
    /// </summary>
    public void RefreshTrades()
    {
        _openTrades = _orderService.OpenTrades;
        _settledTrades = _orderService.SettledTrades;
        TradesUpdated?.Invoke();
    }
}
