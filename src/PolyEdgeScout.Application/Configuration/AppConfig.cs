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
    public double MinEdge { get; set; } = 0.08;
    public double DefaultBetSize { get; set; } = 100;
    public double KellyFraction { get; set; } = 0.5;
    public double MaxBankrollPercent { get; set; } = 0.02;
    public double FadeMultiplier { get; set; } = 0.92;
    public string GammaApiBaseUrl { get; set; } = "https://gamma-api.polymarket.com";
    public string BinanceApiBaseUrl { get; set; } = "https://api.binance.com";
    public string ClobBaseUrl { get; set; } = "https://clob.polymarket.com";
    public string LogDirectory { get; set; } = "logs";
    public string DatabaseConnectionString { get; set; } = "Data Source=data/polyedge-scout.db";
}
