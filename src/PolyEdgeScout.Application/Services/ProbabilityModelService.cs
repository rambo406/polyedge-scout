namespace PolyEdgeScout.Application.Services;

using System.Globalization;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;
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

    public async Task<ModelEvaluation?> CalculateProbabilityAsync(Market market, CancellationToken ct = default)
    {
        try
        {
            var result = QuestionParser.ParseStructured(market.Question);

            return result switch
            {
                ParseResult.PriceTarget ptr => await CalculatePriceTargetProbabilityAsync(ptr, market, ct),
                ParseResult.Directional dir => await CalculateDirectionalProbabilityAsync(dir, market, ct),
                ParseResult.Unrecognised => LogUnrecognisedAndReturn(market),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _log.Error($"Probability calculation failed for: {market.Question}", ex);
            return null;
        }
    }

    /// <summary>
    /// Calculates probability for a price-target market (e.g., "Will BTC hit $100K?").
    /// </summary>
    private async Task<ModelEvaluation?> CalculatePriceTargetProbabilityAsync(
        ParseResult.PriceTarget result, Market market, CancellationToken ct)
    {
        _log.Debug($"Parsed: {result.Symbol} target=${result.TargetPrice:N2}");

        var ticker = await _binanceClient.FetchTickerAsync(result.Symbol, ct);
        if (ticker is null)
        {
            _log.Warn($"Failed to fetch Binance ticker for {result.Symbol}");
            return null;
        }

        var currentPrice = double.Parse(ticker.LastPrice, CultureInfo.InvariantCulture);
        var highPrice = double.Parse(ticker.HighPrice, CultureInfo.InvariantCulture);
        var lowPrice = double.Parse(ticker.LowPrice, CultureInfo.InvariantCulture);

        if (currentPrice <= 0)
        {
            _log.Warn($"Invalid current price for {result.Symbol}: {currentPrice}");
            return new ModelEvaluation(0.5, result.TargetPrice, null);
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
            _log.Warn($"Invalid scaled volatility for {result.Symbol}: {scaledVol}");
            return new ModelEvaluation(0.5, result.TargetPrice, currentPrice);
        }

        var zScore = (result.TargetPrice - currentPrice) / (currentPrice * scaledVol);

        // If target > current, prob of reaching = 1 - Φ(z) (right tail)
        // If target < current, prob of going below = Φ(z) (left tail)
        var rawProbability = result.TargetPrice >= currentPrice
            ? 1.0 - MathHelper.NormCdf(zScore)
            : MathHelper.NormCdf(zScore);

        var adjustedProbability = rawProbability * _config.FadeMultiplier;
        var clampedProbability = Math.Clamp(adjustedProbability, 0.01, 0.99);

        _log.Info($"Model [{result.Symbol}]: price=${currentPrice:N2} target=${result.TargetPrice:N2} " +
                 $"vol={volatility:P1} hours={hoursLeft:N1} z={zScore:N3} " +
                 $"raw={rawProbability:P1} adj={clampedProbability:P1}");

        return new ModelEvaluation(clampedProbability, result.TargetPrice, currentPrice);
    }

    /// <summary>
    /// Calculates probability for a directional market (Up/Down) using momentum and volatility.
    /// </summary>
    private async Task<ModelEvaluation?> CalculateDirectionalProbabilityAsync(
        ParseResult.Directional result, Market market, CancellationToken ct)
    {
        // Fetch kline data for momentum
        var klines = await _binanceClient.FetchKlinesAsync(result.Symbol, "5m", 12, ct);

        double drift = 0.0;
        if (klines is not null && klines.Count >= 2)
        {
            var closePrices = klines.Select(k => k.Close).ToList();
            drift = MathHelper.ClampDrift(MathHelper.CalculateDrift(closePrices));
        }
        else
        {
            _log.Debug($"Kline fetch failed for {result.Symbol}; using zero drift");
        }

        // Get current ticker for volatility
        var ticker = await _binanceClient.FetchTickerAsync(result.Symbol, ct);
        if (ticker is null)
        {
            _log.Warn($"Failed to fetch Binance ticker for directional model: {result.Symbol}");
            return null;
        }

        var currentPrice = double.Parse(ticker.LastPrice, CultureInfo.InvariantCulture);
        var highPrice = double.Parse(ticker.HighPrice, CultureInfo.InvariantCulture);
        var lowPrice = double.Parse(ticker.LowPrice, CultureInfo.InvariantCulture);

        if (currentPrice <= 0) return null;

        var volatility = (highPrice - lowPrice) / currentPrice;
        // Capture currentPrice for ModelEvaluation before calculations
        var capturedCurrentPrice = currentPrice;

        // Calculate hours for the window
        double hoursLeft;
        if (result.WindowEnd is not null)
        {
            // Use time window
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            hoursLeft = Math.Max(0.1, (result.WindowEnd.Value - now).TotalHours);
        }
        else if (market.EndDate.HasValue)
        {
            hoursLeft = Math.Max(0.1, (market.EndDate.Value - DateTime.UtcNow).TotalHours);
        }
        else
        {
            hoursLeft = Math.Max(0.1, (DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).TotalHours);
        }

        var hoursRatio = hoursLeft / 24.0;
        var scaledVol = volatility * Math.Sqrt(hoursRatio);

        if (scaledVol <= 0) return new ModelEvaluation(0.5, null, capturedCurrentPrice); // No volatility info

        // z_adjusted = drift / scaledVol
        var zAdjusted = drift / scaledVol;

        // P(up) = Φ(z_adjusted)  — positive drift → P(up) > 0.5
        var pUp = MathHelper.NormCdf(zAdjusted);

        // Apply fade multiplier
        var rawProb = result.Direction == MarketDirection.Up ? pUp : 1.0 - pUp;
        var adjusted = rawProb * _config.FadeMultiplier;
        var clamped = Math.Clamp(adjusted, 0.01, 0.99);

        _log.Info($"Directional [{result.Symbol}] {result.Direction}: " +
                 $"drift={drift:P2} vol={volatility:P1} hours={hoursLeft:N1} " +
                 $"z={zAdjusted:N3} pUp={pUp:P1} adj={clamped:P1}");

        return new ModelEvaluation(clamped, null, capturedCurrentPrice);
    }

    private ModelEvaluation? LogUnrecognisedAndReturn(Market market)
    {
        _log.Debug($"Skipping market (no target price): {market.Question}");
        return null;
    }
}
