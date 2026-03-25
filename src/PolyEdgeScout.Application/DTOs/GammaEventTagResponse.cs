namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Tag metadata returned by the Polymarket Gamma API on each event.
/// Used for in-memory filtering after server-side tag queries.
/// All properties rely on <c>PropertyNameCaseInsensitive = true</c> to match
/// the Gamma API's camelCase field names.
/// </summary>
public record GammaEventTagResponse
{
    /// <summary>Tag identifier (e.g., "21" for Crypto, "102127" for Up or Down).</summary>
    public string Id { get; init; } = "";

    /// <summary>Human-readable tag label (e.g., "Crypto", "Up or Down").</summary>
    public string Label { get; init; } = "";

    /// <summary>URL-friendly slug for the tag.</summary>
    public string Slug { get; init; } = "";
}
