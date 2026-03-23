using System.Text.Json.Serialization;

namespace PolyEdgeScout.Models;

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
}

/// <summary>
/// Cleaned domain model representing a Polymarket market.
/// </summary>
public record Market
{
    public string ConditionId { get; init; } = "";
    public string QuestionId { get; init; } = "";
    public string TokenId { get; init; } = "";
    public string Question { get; init; } = "";
    public double YesPrice { get; init; }
    public double NoPrice { get; init; }
    public double Volume { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EndDate { get; init; }
    public string MarketSlug { get; init; } = "";
    public bool Active { get; init; }
    public bool Closed { get; init; }

    /// <summary>
    /// Creates a clean <see cref="Market"/> from a raw Gamma API response.
    /// Extracts Yes/No prices from tokens list or falls back to outcomePrices.
    /// </summary>
    public static Market FromGammaResponse(GammaMarketResponse response)
    {
        var yesToken = response.Tokens.FirstOrDefault(t =>
            t.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase));
        var noToken = response.Tokens.FirstOrDefault(t =>
            t.Outcome.Equals("No", StringComparison.OrdinalIgnoreCase));

        double yesPrice = yesToken?.Price ?? 0;
        double noPrice = noToken?.Price ?? 0;

        // Fallback: parse outcomePrices JSON array (e.g. "[\"0.55\",\"0.45\"]")
        if (yesPrice == 0 && noPrice == 0 && !string.IsNullOrEmpty(response.OutcomePrices))
        {
            var prices = System.Text.Json.JsonSerializer.Deserialize<List<string>>(response.OutcomePrices);
            if (prices is { Count: >= 2 })
            {
                _ = double.TryParse(prices[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out yesPrice);
                _ = double.TryParse(prices[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out noPrice);
            }
        }

        string tokenId = yesToken?.TokenId ?? response.Tokens.FirstOrDefault()?.TokenId ?? "";

        DateTime createdAt = DateTime.TryParse(response.CreatedAt,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var ca)
            ? ca.ToUniversalTime()
            : DateTime.UtcNow;

        DateTime? endDate = DateTime.TryParse(response.EndDateIso,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var ed)
            ? ed.ToUniversalTime()
            : null;

        return new Market
        {
            ConditionId = response.ConditionId,
            QuestionId = response.QuestionId,
            TokenId = tokenId,
            Question = response.Question,
            YesPrice = yesPrice,
            NoPrice = noPrice,
            Volume = response.VolumeNum,
            CreatedAt = createdAt,
            EndDate = endDate,
            MarketSlug = response.MarketSlug,
            Active = response.Active,
            Closed = response.Closed,
        };
    }
}
