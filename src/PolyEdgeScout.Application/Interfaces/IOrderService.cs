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
    Trade? EvaluateAndTrade(Market market, double modelProbability, CancellationToken ct = default);
    Task<Trade> ExecuteTradeAsync(Trade trade, CancellationToken ct = default);
    void SettleTrade(string tradeId, bool won);
}
