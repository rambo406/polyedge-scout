namespace PolyEdgeScout.Application.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// Raw token entry from the Gamma API response.
/// </summary>
public record GammaToken
{
    [JsonPropertyName("token_id")]
    public string TokenId { get; init; } = "";

    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = "";

    [JsonPropertyName("price")]
    public double Price { get; init; }
}

/// <summary>
/// Raw market response from the Polymarket Gamma API.
/// </summary>
public record GammaMarketResponse
{
    [JsonPropertyName("condition_id")]
    public string ConditionId { get; init; } = "";

    [JsonPropertyName("question_id")]
    public string QuestionId { get; init; } = "";

    [JsonPropertyName("tokens")]
    public List<GammaToken> Tokens { get; init; } = [];

    [JsonPropertyName("question")]
    public string Question { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("market_slug")]
    public string MarketSlug { get; init; } = "";

    [JsonPropertyName("end_date_iso")]
    public string? EndDateIso { get; init; }

    [JsonPropertyName("game_start_time")]
    public string? GameStartTime { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("closed")]
    public bool Closed { get; init; }

    [JsonPropertyName("volume")]
    public string? Volume { get; init; }

    [JsonPropertyName("volume_num")]
    public double VolumeNum { get; init; }

    [JsonPropertyName("liquidity")]
    public string? Liquidity { get; init; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("outcomePrices")]
    public string? OutcomePrices { get; init; }

    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    [JsonPropertyName("resolution_source")]
    public string? ResolutionSource { get; init; }

    [JsonPropertyName("resolved_by")]
    public string? ResolvedBy { get; init; }
}
