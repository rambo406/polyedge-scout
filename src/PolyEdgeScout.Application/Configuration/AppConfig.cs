namespace PolyEdgeScout.Application.Configuration;

/// <summary>
/// Strongly-typed configuration mapping the "PolyEdgeScout" section of appsettings.json.
/// </summary>
public sealed class AppConfig
{
    public string PolygonRpc { get; set; } = "https://polygon-rpc.com";
    public bool PaperMode { get; set; } = true;
    public int ScanIntervalSeconds { get; set; } = 60;
    public double MaxVolume { get; set; } = 3000;
    public double MinVolume { get; set; } = 100;
    public double MinEdge { get; set; } = 0.08;
    public double DefaultBetSize { get; set; } = 100;
    public double KellyFraction { get; set; } = 0.5;
    public double MaxBankrollPercent { get; set; } = 0.02;
    public double FadeMultiplier { get; set; } = 0.92;
    public string GammaApiBaseUrl { get; set; } = "https://gamma-api.polymarket.com";
    public string BinanceApiBaseUrl { get; set; } = "https://api.binance.com";
    public string ClobBaseUrl { get; set; } = "https://clob.polymarket.com";

    /// <summary>
    /// Tag ID used in the server-side Gamma API query (single tag per request).
    /// Default: 102127 (Up or Down).
    /// </summary>
    public int ServerEventTagId { get; set; } = 102127;

    /// <summary>
    /// Tag IDs to filter events in-memory after fetching from the server.
    /// Events must contain ALL of these tags to pass the filter.
    /// Default: [21] (Crypto).
    /// </summary>
    public List<int> InMemoryEventTagIds { get; set; } = [21];

    public string LogDirectory { get; set; } = "logs";
    public string DatabaseConnectionString { get; set; } = "Data Source=data/polyedge-scout.db";

    /// <summary>
    /// Configurable market keyword filter settings.
    /// </summary>
    public MarketFilterConfig MarketFilter { get; set; } = new();
}
