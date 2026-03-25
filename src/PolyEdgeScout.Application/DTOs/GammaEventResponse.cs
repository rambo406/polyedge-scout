namespace PolyEdgeScout.Application.DTOs;

/// <summary>
/// Raw event response from the Polymarket Gamma API events endpoint.
/// Each event contains one or more nested markets.
/// All properties rely on <c>PropertyNameCaseInsensitive = true</c> to match
/// the Gamma API's camelCase field names.
/// </summary>
public record GammaEventResponse
{
    public string Id { get; init; } = "";

    public string Title { get; init; } = "";

    public string Slug { get; init; } = "";

    public bool Active { get; init; }

    public bool Closed { get; init; }

    public List<GammaMarketResponse> Markets { get; init; } = [];

    /// <summary>
    /// Tags associated with this event (e.g., Crypto, Up or Down).
    /// Used for in-memory filtering after the server-side tag query.
    /// </summary>
    public List<GammaEventTagResponse> Tags { get; init; } = [];
}
