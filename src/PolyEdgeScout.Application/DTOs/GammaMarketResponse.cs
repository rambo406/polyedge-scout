namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Raw token entry from the Gamma API response.
/// </summary>
public record GammaToken
{
    public string TokenId { get; init; } = "";

    public string Outcome { get; init; } = "";

    public double Price { get; init; }
}

/// <summary>
/// Raw market response from the Polymarket Gamma API.
/// All properties rely on <c>PropertyNameCaseInsensitive = true</c> to match
/// the Gamma API's camelCase field names (e.g., conditionId → ConditionId).
/// </summary>
public record GammaMarketResponse
{
    public string ConditionId { get; init; } = "";

    /// <summary>
    /// Gamma API uses <c>questionID</c> (uppercase ID) — matches case-insensitively.
    /// </summary>
    public string QuestionId { get; init; } = "";

    public List<GammaToken> Tokens { get; init; } = [];

    public string Question { get; init; } = "";

    public string Description { get; init; } = "";

    /// <summary>
    /// Market slug — mapped from <c>slug</c> (both endpoints use this field name).
    /// </summary>
    public string Slug { get; init; } = "";

    /// <summary>
    /// Date-only end date (e.g., "2026-03-25").
    /// </summary>
    public string? EndDateIso { get; init; }

    /// <summary>
    /// Full ISO 8601 end date/time (e.g., "2026-03-25T13:00:00Z").
    /// Provides hour-level precision unlike <see cref="EndDateIso"/>.
    /// </summary>
    public string? EndDate { get; init; }

    public string? GameStartTime { get; init; }

    public bool Active { get; init; }

    public bool Closed { get; init; }

    public string? Volume { get; init; }

    public double VolumeNum { get; init; }

    public string? Liquidity { get; init; }

    public string? CreatedAt { get; init; }

    public string? OutcomePrices { get; init; }

    public string? Outcome { get; init; }

    public string? ResolutionSource { get; init; }

    public string? ResolvedBy { get; init; }

    /// <summary>
    /// Serialized JSON array of CLOB token IDs (e.g., '["id1", "id2"]').
    /// Used by markets fetched from the events endpoint where the <see cref="Tokens"/> array is absent.
    /// </summary>
    public string? ClobTokenIds { get; init; }
}
