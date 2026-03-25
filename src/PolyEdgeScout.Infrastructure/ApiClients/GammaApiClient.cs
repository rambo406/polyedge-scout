namespace PolyEdgeScout.Infrastructure.ApiClients;

using System.Net;
using System.Text.Json;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// HTTP client for the Polymarket Gamma API with exponential-backoff retry on 429 responses.
/// </summary>
public sealed class GammaApiClient : IGammaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly ILogService _log;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public GammaApiClient(HttpClient httpClient, AppConfig config, ILogService log)
    {
        _httpClient = httpClient;
        _config = config;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<List<GammaMarketResponse>> FetchActiveMarketsAsync(CancellationToken ct = default)
    {
        var url = $"{_config.GammaApiBaseUrl}/markets"
                + $"?active=true&closed=false&volume_num_min={_config.MinVolume}&volume_num_max={_config.MaxVolume}"
                + "&order=createdAt&ascending=false&limit=50";

        return await FetchWithRetryAsync<List<GammaMarketResponse>>(url, ct);
    }

    /// <inheritdoc />
    public async Task<List<GammaMarketResponse>> FetchResolvedMarketsAsync(int limit = 100, CancellationToken ct = default)
    {
        var url = $"{_config.GammaApiBaseUrl}/markets"
                + $"?active=false&closed=true&order=endDateIso&ascending=false&limit={limit}";

        return await FetchWithRetryAsync<List<GammaMarketResponse>>(url, ct);
    }

    /// <inheritdoc />
    public async Task<List<GammaEventResponse>> FetchActiveEventsAsync(CancellationToken ct = default)
    {
        int tagId = _config.ServerEventTagId;
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var url = $"{_config.GammaApiBaseUrl}/events"
                + $"?tag_id={tagId}&active=true&closed=false"
                + $"&end_date_min={today}"
                + $"&volume_num_min={_config.MinVolume}&volume_num_max={_config.MaxVolume}"
                + "&limit=500";

        var events = await FetchWithRetryAsync<List<GammaEventResponse>>(url, ct);
        _log.Info($"Fetched {events.Count} events for server tag {tagId} (end_date_min={today})");
        return events;
    }

    /// <summary>
    /// Fetches a URL with exponential-backoff retry on HTTP 429 responses (max 3 retries).
    /// Returns a default instance of <typeparamref name="T"/> on total failure.
    /// </summary>
    private async Task<T> FetchWithRetryAsync<T>(string url, CancellationToken ct) where T : new()
    {
        const int maxRetries = 3;
        int delayMs = 1000;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                _log.Debug($"Gamma API request (attempt {attempt + 1}): {url}");

                using HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt == maxRetries)
                    {
                        _log.Error($"Rate-limited after {maxRetries} retries: {url}");
                        return new T();
                    }

                    _log.Warn($"429 Too Many Requests — retrying in {delayMs}ms (attempt {attempt + 1}/{maxRetries})");
                    await Task.Delay(delayMs, ct);
                    delayMs *= 2;
                    continue;
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync(ct);

                T? result = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return result ?? new T();
            }
            catch (OperationCanceledException)
            {
                throw; // propagate cancellation
            }
            catch (HttpRequestException ex) when (
                ex.StatusCode.HasValue
                && (int)ex.StatusCode.Value >= 400
                && (int)ex.StatusCode.Value < 500
                && ex.StatusCode.Value != HttpStatusCode.TooManyRequests)
            {
                _log.Error($"Gamma API client error ({ex.StatusCode}): {url}", ex);
                throw; // Client error — won't resolve with retry
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    _log.Error($"Gamma API request failed after {maxRetries + 1} attempts: {url}", ex);
                    return new T();
                }

                _log.Warn($"Gamma API request error (attempt {attempt + 1}/{maxRetries}): {ex.Message}. Retrying in {delayMs}ms");
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }

        // Unreachable, but satisfies the compiler
        return new T();
    }
}
