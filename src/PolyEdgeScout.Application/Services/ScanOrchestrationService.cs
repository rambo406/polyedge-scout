namespace PolyEdgeScout.Application.Services;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Enums;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;
using PolyEdgeScout.Domain.ValueObjects;

public sealed class ScanOrchestrationService : IScanOrchestrationService
{
    private static readonly IEdgeFormula EdgeFormula = new DefaultScaledEdgeFormula();

    private readonly AppConfig _config;
    private readonly IScannerService _scanner;
    private readonly IProbabilityModelService _probModel;
    private readonly IOrderService _orders;
    private readonly ILogService _log;

    public ScanOrchestrationService(
        AppConfig config,
        IScannerService scanner,
        IProbabilityModelService probModel,
        IOrderService orders,
        ILogService log)
    {
        _config = config;
        _scanner = scanner;
        _probModel = probModel;
        _orders = orders;
        _log = log;
    }

    public async Task<IReadOnlyList<MarketScanResult>> ScanAndEvaluateAsync(CancellationToken ct = default)
    {
        var markets = await _scanner.ScanMarketsAsync(ct);
        var results = new List<MarketScanResult>();

        foreach (var market in markets)
        {
            if (ct.IsCancellationRequested) break;

            var prob = await _probModel.CalculateProbabilityAsync(market, ct);
            if (prob is null) continue; // Skip unparseable markets

            var edgeCalc = EdgeCalculation.Create(
                EdgeFormula,
                prob.ModelProbability,
                market.YesPrice,
                prob.TargetPrice,
                prob.CurrentAssetPrice);

            var action = market.Volume >= _config.MaxVolume
                ? TradeAction.Hold
                : edgeCalc.DetermineAction(_config.MinEdge);

            results.Add(new MarketScanResult
            {
                Market = market,
                ModelProbability = prob.ModelProbability,
                Edge = edgeCalc.Edge,
                Action = action,
                TargetPrice = prob.TargetPrice,
                CurrentAssetPrice = prob.CurrentAssetPrice,
            });
        }

        return results.OrderByDescending(r => r.Edge).ToList();
    }

    public async Task<IReadOnlyList<MarketScanResult>> ScanEvaluateAndAutoTradeAsync(CancellationToken ct = default)
    {
        var results = await ScanAndEvaluateAsync(ct);

        foreach (var result in results)
        {
            if (ct.IsCancellationRequested) break;

            var trade = _orders.EvaluateAndTrade(
                result.Market, result.ModelProbability,
                result.TargetPrice, result.CurrentAssetPrice, ct);
            if (trade is not null)
            {
                _log.Info($"Auto-trade: {result.Market.Question} | Edge={result.Edge:+0.00;-0.00}");
                await _orders.ExecuteTradeAsync(trade, ct);
            }
        }

        return results;
    }
}
