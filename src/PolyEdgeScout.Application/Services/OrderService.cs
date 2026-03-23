namespace PolyEdgeScout.Application.Services;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.ValueObjects;

/// <summary>
/// Application-level order management.
/// Manages trade evaluation, execution (paper and live), and settlement.
/// Maintains an in-memory ledger of open and settled trades with thread-safe access.
/// Delegates CLOB order submission to IClobClient.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly AppConfig _config;
    private readonly IClobClient _clobClient;
    private readonly ILogService _log;

    private readonly List<Trade> _openTrades = [];
    private readonly List<TradeResult> _settledTrades = [];
    private double _bankroll = 10_000.0;
    private bool _paperMode;
    private readonly object _lock = new();

    public OrderService(AppConfig config, IClobClient clobClient, ILogService log)
    {
        _config = config;
        _clobClient = clobClient;
        _log = log;
        _paperMode = config.PaperMode;
    }

    /// <inheritdoc />
    public IReadOnlyList<Trade> OpenTrades
    {
        get
        {
            lock (_lock)
            {
                return _openTrades.ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<TradeResult> SettledTrades
    {
        get
        {
            lock (_lock)
            {
                return _settledTrades.ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public double Bankroll
    {
        get
        {
            lock (_lock)
            {
                return _bankroll;
            }
        }
    }

    /// <inheritdoc />
    public bool PaperMode
    {
        get
        {
            lock (_lock)
            {
                return _paperMode;
            }
        }
        set
        {
            lock (_lock)
            {
                _paperMode = value;
                _log.Info($"Paper mode toggled to: {value}");
            }
        }
    }

    /// <inheritdoc />
    public PnlSnapshot GetPnlSnapshot()
    {
        lock (_lock)
        {
            double unrealized = _openTrades.Sum(t => (1.0 - t.EntryPrice) * t.Shares);
            double realized = _settledTrades.Sum(t => t.NetProfit);

            return new PnlSnapshot
            {
                Bankroll = _bankroll,
                OpenPositions = _openTrades.Count,
                UnrealizedPnl = unrealized,
                RealizedPnl = realized,
                LastTrades = _settledTrades
                    .OrderByDescending(t => t.SettledAt)
                    .Take(5)
                    .ToList(),
            };
        }
    }

    /// <inheritdoc />
    public Trade? EvaluateAndTrade(Market market, double modelProbability, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var edge = new EdgeCalculation
        {
            ModelProbability = modelProbability,
            MarketPrice = market.YesPrice,
        };

        if (!edge.HasSufficientEdge(_config.MinEdge))
        {
            _log.Debug($"HOLD — edge {edge.Edge:P2} below min {_config.MinEdge:P2}: {Truncate(market.Question)}");
            return null;
        }

        if (market.Volume >= _config.MaxVolume)
        {
            _log.Debug($"HOLD — volume {market.Volume:F0} exceeds max {_config.MaxVolume:F0}: {Truncate(market.Question)}");
            return null;
        }

        // Size the bet using domain BetSizing value object
        var sizing = BetSizing.Calculate(
            modelProbability,
            market.YesPrice,
            _bankroll,
            _config.DefaultBetSize,
            _config.KellyFraction,
            _config.MaxBankrollPercent);

        double betSize = sizing.RecommendedBet;

        if (betSize <= 0)
        {
            _log.Debug($"HOLD — Kelly sizing returned zero bet for: {Truncate(market.Question)}");
            return null;
        }

        var trade = new Trade
        {
            MarketQuestion = market.Question,
            ConditionId = market.ConditionId,
            TokenId = market.TokenId,
            Action = TradeAction.Buy,
            Status = TradeStatus.Pending,
            EntryPrice = market.YesPrice,
            Shares = sizing.Shares,
            ModelProbability = modelProbability,
            Edge = edge.Edge,
            Outlay = betSize,
            IsPaper = _paperMode,
        };

        _log.Info(
            $"TRADE SIGNAL: {Truncate(market.Question)} | " +
            $"Edge={edge.Edge:P2} ModelP={modelProbability:P2} Price={market.YesPrice:F3} " +
            $"Bet=${betSize:F2} Shares={sizing.Shares:F2}");

        return trade;
    }

    /// <inheritdoc />
    public async Task<Trade> ExecuteTradeAsync(Trade trade, CancellationToken ct = default)
    {
        if (_paperMode)
        {
            return ExecutePaperTrade(trade);
        }

        return await ExecuteLiveTradeAsync(trade, ct);
    }

    /// <inheritdoc />
    public void SettleTrade(string tradeId, bool won)
    {
        lock (_lock)
        {
            Trade? trade = _openTrades.FirstOrDefault(t => t.Id == tradeId);
            if (trade is null)
            {
                _log.Warn($"SettleTrade: trade {tradeId} not found in open trades.");
                return;
            }

            double grossProfit = won
                ? (trade.Shares * 1.0) - (trade.Shares * trade.EntryPrice)
                : -(trade.Shares * trade.EntryPrice);

            double fees = trade.Outlay * 0.02; // 2% fee estimate
            double gas = _paperMode ? 0.0 : 0.50; // estimated gas cost
            double netProfit = grossProfit - fees - gas;
            double roi = trade.Outlay > 0 ? netProfit / trade.Outlay : 0;

            var result = new TradeResult
            {
                TradeId = trade.Id,
                MarketQuestion = trade.MarketQuestion,
                EntryPrice = trade.EntryPrice,
                Shares = trade.Shares,
                GrossProfit = grossProfit,
                Fees = fees,
                Gas = gas,
                Roi = roi,
                SettledAt = DateTime.UtcNow,
                Won = won,
            };

            _settledTrades.Add(result);
            _openTrades.Remove(trade);

            if (won)
            {
                // Winning trade returns the full share value ($1 per share)
                _bankroll += trade.Shares * 1.0 - fees - gas;
            }
            else
            {
                // Loss — outlay was already deducted; only subtract remaining fees/gas
                _bankroll -= fees + gas;
            }

            _log.Info(
                $"SETTLED: {Truncate(trade.MarketQuestion)} | " +
                $"Won={won} Gross=${grossProfit:F2} Fees=${fees:F2} Gas=${gas:F2} " +
                $"Net=${result.NetProfit:F2} ROI={roi:P2} Bankroll=${_bankroll:F2}");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Private helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a trade locally in paper-trading mode.
    /// </summary>
    private Trade ExecutePaperTrade(Trade trade)
    {
        Trade filled = trade with
        {
            Status = TradeStatus.Filled,
            IsPaper = true,
        };

        lock (_lock)
        {
            _openTrades.Add(filled);
            _bankroll -= filled.Outlay;
        }

        _log.Info(
            $"PAPER TRADE: {Truncate(filled.MarketQuestion)} | " +
            $"Price={filled.EntryPrice:F3} Shares={filled.Shares:F2} " +
            $"Outlay=${filled.Outlay:F2} Bankroll=${_bankroll:F2}");

        return filled;
    }

    /// <summary>
    /// Submits a live order via IClobClient and records the filled trade.
    /// </summary>
    private async Task<Trade> ExecuteLiveTradeAsync(Trade trade, CancellationToken ct)
    {
        string? txHash = await _clobClient.PostOrderAsync(trade, ct);

        Trade filled = trade with
        {
            Status = TradeStatus.Filled,
            IsPaper = false,
            TxHash = txHash,
        };

        lock (_lock)
        {
            _openTrades.Add(filled);
            _bankroll -= filled.Outlay;
        }

        _log.Info(
            $"LIVE TRADE: {Truncate(filled.MarketQuestion)} | " +
            $"Price={filled.EntryPrice:F3} Shares={filled.Shares:F2} " +
            $"Outlay=${filled.Outlay:F2} TxHash={txHash} Bankroll=${_bankroll:F2}");

        return filled;
    }

    /// <summary>
    /// Truncates a string to a maximum length for log readability.
    /// </summary>
    private static string Truncate(string text, int max = 80) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");
}
