namespace PolyEdgeScout.Infrastructure.ApiClients;

using System.Text.Json;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// HTTP client for the Binance 24-hour ticker API.
/// Returns <c>null</c> on any failure to allow callers to fall back gracefully.
/// </summary>
public sealed class BinanceApiClient : IBinanceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly ILogService _log;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public BinanceApiClient(HttpClient httpClient, AppConfig config, ILogService log)
    {
        _httpClient = httpClient;
        _config = config;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<BinanceTickerResponse?> FetchTickerAsync(string symbol, CancellationToken ct = default)
    {
        string url = $"{_config.BinanceApiBaseUrl}/api/v3/ticker/24hr?symbol={symbol}USDT";

        try
        {
            _log.Debug($"Fetching Binance ticker: {url}");

            using HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                _log.Warn($"Binance API returned {(int)response.StatusCode} for {symbol}USDT: {body}");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            BinanceTickerResponse? ticker = JsonSerializer.Deserialize<BinanceTickerResponse>(json, JsonOptions);

            _log.Debug($"Binance ticker for {symbol}USDT: price={ticker?.LastPrice}, change={ticker?.PriceChangePercent}%");
            return ticker;
        }
        catch (OperationCanceledException)
        {
            throw; // propagate cancellation
        }
        catch (Exception ex)
        {
            _log.Error($"Binance API call failed for {symbol}USDT", ex);
            return null;
        }
    }
}
