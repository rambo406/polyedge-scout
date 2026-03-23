namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Manages trade evaluation, execution (paper and live), and settlement.
/// Maintains an in-memory ledger of open and settled trades.
/// </summary>
public interface IOrderService
{
    IReadOnlyList<Trade> OpenTrades { get; }
    IReadOnlyList<TradeResult> SettledTrades { get; }
    double Bankroll { get; }
    bool PaperMode { get; set; }

    PnlSnapshot GetPnlSnapshot();
    Task InitializeAsync();
    Trade? EvaluateAndTrade(Market market, double modelProbability, CancellationToken ct = default);
    Task<Trade> ExecuteTradeAsync(Trade trade, CancellationToken ct = default);
    Task SettleTradeAsync(string tradeId, bool won);
    void SettleTrade(string tradeId, bool won); // Keep sync version for backward compatibility
}
