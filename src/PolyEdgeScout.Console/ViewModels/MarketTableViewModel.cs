namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// ViewModel for the market scanner table.
/// Holds the current market list, selected row, and trade execution command.
/// </summary>
public sealed class MarketTableViewModel
{
    private readonly IOrderService _orderService;
    private readonly AppConfig _config;
    private readonly ILogService _log;
    private IReadOnlyList<MarketScanResult> _markets = [];

    public IReadOnlyList<MarketScanResult> Markets => _markets;
    public int SelectedIndex { get; set; }

    public event Action? MarketsUpdated;
    public event Action<string>? TradeExecuted;

    public MarketTableViewModel(IOrderService orderService, AppConfig config, ILogService log)
    {
        _orderService = orderService;
        _config = config;
        _log = log;
    }

    public void UpdateMarkets(IReadOnlyList<MarketScanResult> markets)
    {
        _markets = markets;
        if (SelectedIndex >= _markets.Count)
            SelectedIndex = Math.Max(0, _markets.Count - 1);
        MarketsUpdated?.Invoke();
    }

    public async Task ExecuteTradeAsync(CancellationToken ct = default)
    {
        if (SelectedIndex < 0 || SelectedIndex >= _markets.Count)
            return;

        var selected = _markets[SelectedIndex];
        if (Math.Abs(selected.Edge) <= _config.MinEdge)
        {
            _log.Warn($"Insufficient edge for manual trade on: {selected.Market.Question}");
            TradeExecuted?.Invoke($"Insufficient edge on: {selected.Market.Question}");
            return;
        }

        var trade = _orderService.EvaluateAndTrade(
            selected.Market, selected.ModelProbability,
            selected.TargetPrice, selected.CurrentAssetPrice, ct);
        if (trade is not null)
        {
            _log.Info($"Manual trade: {selected.Market.Question}");
            await _orderService.ExecuteTradeAsync(trade, ct);
            TradeExecuted?.Invoke($"Trade executed: {selected.Market.Question}");
        }
    }
}
