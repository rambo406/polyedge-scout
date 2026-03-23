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
                + "?active=true&closed=false&volume_num_max=5000"
                + "&order=created_at&ascending=false&limit=50";

        return await FetchWithRetryAsync(url, ct);
    }

    /// <inheritdoc />
    public async Task<List<GammaMarketResponse>> FetchResolvedMarketsAsync(int limit = 100, CancellationToken ct = default)
    {
        var url = $"{_config.GammaApiBaseUrl}/markets"
                + $"?active=false&closed=true&order=end_date_iso&ascending=false&limit={limit}";

        return await FetchWithRetryAsync(url, ct);
    }

    /// <summary>
    /// Fetches a URL with exponential-backoff retry on HTTP 429 responses (max 3 retries).
    /// Returns an empty list on total failure.
    /// </summary>
    private async Task<List<GammaMarketResponse>> FetchWithRetryAsync(string url, CancellationToken ct)
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
                        return [];
                    }

                    _log.Warn($"429 Too Many Requests — retrying in {delayMs}ms (attempt {attempt + 1}/{maxRetries})");
                    await Task.Delay(delayMs, ct);
                    delayMs *= 2;
                    continue;
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync(ct);

                List<GammaMarketResponse>? result = JsonSerializer.Deserialize<List<GammaMarketResponse>>(json, JsonOptions);
                return result ?? [];
            }
            catch (OperationCanceledException)
            {
                throw; // propagate cancellation
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    _log.Error($"Gamma API request failed after {maxRetries + 1} attempts: {url}", ex);
                    return [];
                }

                _log.Warn($"Gamma API request error (attempt {attempt + 1}/{maxRetries}): {ex.Message}. Retrying in {delayMs}ms");
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }

        // Unreachable, but satisfies the compiler
        return [];
    }
}
