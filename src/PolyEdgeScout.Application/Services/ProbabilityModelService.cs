namespace PolyEdgeScout.Application.Services;

using System.Globalization;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Domain.Services;

/// <summary>
/// Application-level probability calculation orchestration.
/// Fetches real-time price data from Binance and uses a volatility-scaled
/// normal-distribution model to estimate the probability of a market outcome.
/// </summary>
public sealed class ProbabilityModelService : IProbabilityModelService
{
    private readonly AppConfig _config;
    private readonly IBinanceApiClient _binanceClient;
    private readonly ILogService _log;

    public ProbabilityModelService(AppConfig config, IBinanceApiClient binanceClient, ILogService log)
    {
        _config = config;
        _binanceClient = binanceClient;
        _log = log;
    }

    public async Task<double> CalculateProbabilityAsync(Market market, CancellationToken ct = default)
    {
        try
        {
            var (symbol, targetPrice) = QuestionParser.Parse(market.Question);

            if (symbol is null || targetPrice is null)
            {
                _log.Debug($"Could not parse market question: {market.Question}");
                return 0.5;
            }

            _log.Debug($"Parsed: {symbol} target=${targetPrice:N2}");

            var ticker = await _binanceClient.FetchTickerAsync(symbol, ct);
            if (ticker is null)
            {
                _log.Warn($"Failed to fetch Binance ticker for {symbol}");
                return 0.5;
            }

            var currentPrice = double.Parse(ticker.LastPrice, CultureInfo.InvariantCulture);
            var highPrice = double.Parse(ticker.HighPrice, CultureInfo.InvariantCulture);
            var lowPrice = double.Parse(ticker.LowPrice, CultureInfo.InvariantCulture);

            if (currentPrice <= 0)
            {
                _log.Warn($"Invalid current price for {symbol}: {currentPrice}");
                return 0.5;
            }

            var volatility = (highPrice - lowPrice) / currentPrice;

            // Calculate hours left
            var hoursLeft = market.EndDate.HasValue
                ? Math.Max(0.1, (market.EndDate.Value - DateTime.UtcNow).TotalHours)
                : Math.Max(0.1, (DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).TotalHours);

            var hoursRatio = hoursLeft / 24.0;
            var scaledVol = volatility * Math.Sqrt(hoursRatio);

            if (scaledVol <= 0)
            {
                _log.Warn($"Invalid scaled volatility for {symbol}: {scaledVol}");
                return 0.5;
            }

            var zScore = (targetPrice.Value - currentPrice) / (currentPrice * scaledVol);

            // If target > current, prob of reaching = 1 - Φ(z) (right tail)
            // If target < current, prob of going below = Φ(z) (left tail)
            var rawProbability = targetPrice.Value >= currentPrice
                ? 1.0 - MathHelper.NormCdf(zScore)
                : MathHelper.NormCdf(zScore);

            var adjustedProbability = rawProbability * _config.FadeMultiplier;
            var clampedProbability = Math.Clamp(adjustedProbability, 0.01, 0.99);

            _log.Info($"Model [{symbol}]: price=${currentPrice:N2} target=${targetPrice:N2} " +
                     $"vol={volatility:P1} hours={hoursLeft:N1} z={zScore:N3} " +
                     $"raw={rawProbability:P1} adj={clampedProbability:P1}");

            return clampedProbability;
        }
        catch (Exception ex)
        {
            _log.Error($"Probability calculation failed for: {market.Question}", ex);
            return 0.5;
        }
    }
}
