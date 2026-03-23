namespace PolyEdgeScout.Application.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// Model for the Binance 24hr ticker price change statistics response.
/// See: https://binance-docs.github.io/apidocs/spot/en/#24hr-ticker-price-change-statistics
/// </summary>
public record BinanceTickerResponse
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = "";

    [JsonPropertyName("lastPrice")]
    public string LastPrice { get; init; } = "";

    [JsonPropertyName("priceChangePercent")]
    public string PriceChangePercent { get; init; } = "";

    [JsonPropertyName("highPrice")]
    public string HighPrice { get; init; } = "";

    [JsonPropertyName("lowPrice")]
    public string LowPrice { get; init; } = "";

    [JsonPropertyName("volume")]
    public string Volume { get; init; } = "";

    [JsonPropertyName("quoteVolume")]
    public string QuoteVolume { get; init; } = "";
}
