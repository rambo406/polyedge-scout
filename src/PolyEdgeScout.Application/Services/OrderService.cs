namespace PolyEdgeScout.Application.Services;

using Microsoft.Extensions.DependencyInjection;
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
/// Persists state to SQLite via scoped repositories (using IServiceScopeFactory).
/// Delegates CLOB order submission to IClobClient.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly AppConfig _config;
    private readonly IClobClient _clobClient;
    private readonly ILogService _log;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<Trade> _openTrades = [];
    private readonly List<TradeResult> _settledTrades = [];
    private double _bankroll = 10_000.0;
    private bool _paperMode;
    private readonly object _lock = new();

    public OrderService(AppConfig config, IClobClient clobClient, ILogService log, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _clobClient = clobClient;
        _log = log;
        _scopeFactory = scopeFactory;
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
    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tradeRepo = scope.ServiceProvider.GetRequiredService<ITradeRepository>();
            var tradeResultRepo = scope.ServiceProvider.GetRequiredService<ITradeResultRepository>();
            var appStateRepo = scope.ServiceProvider.GetRequiredService<IAppStateRepository>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var openTrades = await tradeRepo.GetOpenTradesAsync();
            var settledTrades = await tradeResultRepo.GetAllAsync();
            var bankroll = await appStateRepo.GetBankrollAsync();

            lock (_lock)
            {
                _openTrades.AddRange(openTrades);
                _settledTrades.AddRange(settledTrades);
                _bankroll = bankroll;
            }

            if (openTrades.Count > 0 || settledTrades.Count > 0)
            {
                // Log recovery audit entry
                string correlationId = Guid.NewGuid().ToString("N");
                await auditService.LogAsync(
                    "AppState", "Recovery", AuditAction.Recovered, correlationId,
                    newValue: $"OpenTrades={openTrades.Count}, SettledTrades={settledTrades.Count}, Bankroll={bankroll:F2}");
                await unitOfWork.SaveChangesAsync();

                _log.Info($"State recovered: {openTrades.Count} open trades, {settledTrades.Count} settled trades, bankroll=${bankroll:F2}");
            }
            else
            {
                // First run — seed default bankroll
                var existingBankroll = await appStateRepo.GetValueAsync("Bankroll");
                if (existingBankroll is null)
                {
                    await appStateRepo.SetBankrollAsync(10_000.0);

                    string correlationId = Guid.NewGuid().ToString("N");
                    await auditService.LogAsync(
                        "AppState", "Bankroll", AuditAction.Created, correlationId,
                        propertyName: "Bankroll", newValue: "10000");
                    await unitOfWork.SaveChangesAsync();

                    _log.Info("Fresh start: default bankroll $10,000.00 initialized.");
                }
                else
                {
                    _log.Info($"No open trades found. Bankroll=${bankroll:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to recover state from database: {ex.Message}", ex);
            _log.Warn("Starting with default in-memory state.");
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
            return await ExecutePaperTradeAsync(trade);
        }

        return await ExecuteLiveTradeAsync(trade, ct);
    }

    /// <inheritdoc />
    public async Task SettleTradeAsync(string tradeId, bool won)
    {
        Trade? trade;
        double previousBankroll;
        TradeResult result;

        lock (_lock)
        {
            trade = _openTrades.FirstOrDefault(t => t.Id == tradeId);
            if (trade is null)
            {
                _log.Warn($"SettleTrade: trade {tradeId} not found in open trades.");
                return;
            }

            previousBankroll = _bankroll;

            double grossProfit = won
                ? (trade.Shares * 1.0) - (trade.Shares * trade.EntryPrice)
                : -(trade.Shares * trade.EntryPrice);

            double fees = trade.Outlay * 0.02;
            double gas = _paperMode ? 0.0 : 0.50;
            double netProfit = grossProfit - fees - gas;
            double roi = trade.Outlay > 0 ? netProfit / trade.Outlay : 0;

            result = new TradeResult
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
                _bankroll += trade.Shares * 1.0 - fees - gas;
            }
            else
            {
                _bankroll -= fees + gas;
            }
        }

        // Persist to database
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tradeRepo = scope.ServiceProvider.GetRequiredService<ITradeRepository>();
            var tradeResultRepo = scope.ServiceProvider.GetRequiredService<ITradeResultRepository>();
            var appStateRepo = scope.ServiceProvider.GetRequiredService<IAppStateRepository>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string correlationId = Guid.NewGuid().ToString("N");

            await unitOfWork.BeginTransactionAsync();

            // Update trade status
            var settledTrade = trade with { Status = TradeStatus.Settled };
            await tradeRepo.UpdateAsync(settledTrade);

            // Save trade result
            await tradeResultRepo.AddAsync(result);

            // Update bankroll
            await appStateRepo.SetBankrollAsync(_bankroll);

            // Audit entries
            await auditService.LogAsync("Trade", trade.Id, AuditAction.Settled, correlationId,
                propertyName: "Status", oldValue: "Filled", newValue: "Settled");
            await auditService.LogAsync("TradeResult", trade.Id, AuditAction.Created, correlationId);
            await auditService.LogAsync("AppState", "Bankroll", AuditAction.Updated, correlationId,
                propertyName: "Bankroll",
                oldValue: previousBankroll.ToString(System.Globalization.CultureInfo.InvariantCulture),
                newValue: _bankroll.ToString(System.Globalization.CultureInfo.InvariantCulture));

            await unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to persist trade settlement: {ex.Message}", ex);
        }

        _log.Info(
            $"SETTLED: {Truncate(trade.MarketQuestion)} | " +
            $"Won={won} Gross=${result.GrossProfit:F2} Fees=${result.Fees:F2} Gas=${result.Gas:F2} " +
            $"Net=${result.NetProfit:F2} ROI={result.Roi:P2} Bankroll=${_bankroll:F2}");
    }

    /// <inheritdoc />
    public void SettleTrade(string tradeId, bool won)
    {
        // Synchronous wrapper for backward compatibility
        SettleTradeAsync(tradeId, won).GetAwaiter().GetResult();
    }

    // ──────────────────────────────────────────────────────────────
    //  Private helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Trade> ExecutePaperTradeAsync(Trade trade)
    {
        Trade filled = trade with
        {
            Status = TradeStatus.Filled,
            IsPaper = true,
        };

        double previousBankroll;
        lock (_lock)
        {
            previousBankroll = _bankroll;
            _openTrades.Add(filled);
            _bankroll -= filled.Outlay;
        }

        // Persist to database
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tradeRepo = scope.ServiceProvider.GetRequiredService<ITradeRepository>();
            var appStateRepo = scope.ServiceProvider.GetRequiredService<IAppStateRepository>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string correlationId = Guid.NewGuid().ToString("N");

            await unitOfWork.BeginTransactionAsync();

            await tradeRepo.AddAsync(filled);
            await appStateRepo.SetBankrollAsync(_bankroll);

            // Audit entries
            await auditService.LogAsync("Trade", filled.Id, AuditAction.Created, correlationId);
            await auditService.LogAsync("AppState", "Bankroll", AuditAction.Updated, correlationId,
                propertyName: "Bankroll",
                oldValue: previousBankroll.ToString(System.Globalization.CultureInfo.InvariantCulture),
                newValue: _bankroll.ToString(System.Globalization.CultureInfo.InvariantCulture));

            await unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to persist paper trade: {ex.Message}", ex);
        }

        _log.Info(
            $"PAPER TRADE: {Truncate(filled.MarketQuestion)} | " +
            $"Price={filled.EntryPrice:F3} Shares={filled.Shares:F2} " +
            $"Outlay=${filled.Outlay:F2} Bankroll=${_bankroll:F2}");

        return filled;
    }

    private async Task<Trade> ExecuteLiveTradeAsync(Trade trade, CancellationToken ct)
    {
        string? txHash = await _clobClient.PostOrderAsync(trade, ct);

        Trade filled = trade with
        {
            Status = TradeStatus.Filled,
            IsPaper = false,
            TxHash = txHash,
        };

        double previousBankroll;
        lock (_lock)
        {
            previousBankroll = _bankroll;
            _openTrades.Add(filled);
            _bankroll -= filled.Outlay;
        }

        // Persist to database
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tradeRepo = scope.ServiceProvider.GetRequiredService<ITradeRepository>();
            var appStateRepo = scope.ServiceProvider.GetRequiredService<IAppStateRepository>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string correlationId = Guid.NewGuid().ToString("N");

            await unitOfWork.BeginTransactionAsync();

            await tradeRepo.AddAsync(filled);
            await appStateRepo.SetBankrollAsync(_bankroll);

            await auditService.LogAsync("Trade", filled.Id, AuditAction.Created, correlationId);
            await auditService.LogAsync("AppState", "Bankroll", AuditAction.Updated, correlationId,
                propertyName: "Bankroll",
                oldValue: previousBankroll.ToString(System.Globalization.CultureInfo.InvariantCulture),
                newValue: _bankroll.ToString(System.Globalization.CultureInfo.InvariantCulture));

            await unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to persist live trade: {ex.Message}", ex);
        }

        _log.Info(
            $"LIVE TRADE: {Truncate(filled.MarketQuestion)} | " +
            $"Price={filled.EntryPrice:F3} Shares={filled.Shares:F2} " +
            $"Outlay=${filled.Outlay:F2} TxHash={txHash} Bankroll=${_bankroll:F2}");

        return filled;
    }

    private static string Truncate(string text, int max = 80) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");
}
