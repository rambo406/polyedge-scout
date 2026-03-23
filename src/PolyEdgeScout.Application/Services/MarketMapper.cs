namespace PolyEdgeScout.Application.Services;

using System.Globalization;
using System.Text.Json;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Domain.Entities;

/// <summary>
/// Maps Gamma API DTOs to domain Market entities.
/// Handles token price extraction from both tokens array and outcomePrices string.
/// </summary>
public static class MarketMapper
{
    /// <summary>
    /// Maps a GammaMarketResponse DTO to a Market domain entity.
    /// </summary>
    public static Market ToDomain(GammaMarketResponse response)
    {
        var yesToken = response.Tokens
            .FirstOrDefault(t => t.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase));
        var noToken = response.Tokens
            .FirstOrDefault(t => t.Outcome.Equals("No", StringComparison.OrdinalIgnoreCase));

        double yesPrice = yesToken?.Price ?? 0;
        double noPrice = noToken?.Price ?? 0;

        // Fallback: parse outcomePrices JSON array (e.g. "[\"0.55\",\"0.45\"]")
        if (yesPrice == 0 && noPrice == 0 && !string.IsNullOrEmpty(response.OutcomePrices))
        {
            try
            {
                var prices = JsonSerializer.Deserialize<List<string>>(response.OutcomePrices);
                if (prices is { Count: >= 2 })
                {
                    _ = double.TryParse(prices[0], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out yesPrice);
                    _ = double.TryParse(prices[1], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out noPrice);
                }
            }
            catch
            {
                // Ignore parse errors for fallback
            }
        }

        string tokenId = yesToken?.TokenId ?? response.Tokens.FirstOrDefault()?.TokenId ?? "";

        DateTime createdAt = DateTime.TryParse(
            response.CreatedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var ca)
            ? ca.ToUniversalTime()
            : DateTime.UtcNow;

        DateTime? endDate = DateTime.TryParse(
            response.EndDateIso,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var ed)
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

    /// <summary>
    /// Determines if a resolved market's YES outcome is true based on
    /// explicit outcome field or token settlement prices.
    /// Returns null if the resolution is ambiguous.
    /// </summary>
    public static bool? DetermineResolution(GammaMarketResponse response)
    {
        // Check explicit outcome field
        if (!string.IsNullOrWhiteSpace(response.Outcome))
        {
            if (response.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (response.Outcome.Equals("No", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Infer from token prices — if the YES token settled at ~1.0 it resolved YES
        var yesToken = response.Tokens
            .FirstOrDefault(t => t.Outcome.Equals("Yes", StringComparison.OrdinalIgnoreCase));

        if (yesToken is not null)
        {
            if (yesToken.Price >= 0.95)
                return true;
            if (yesToken.Price <= 0.05)
                return false;
        }

        return null;
    }
}
