using Microsoft.Extensions.Configuration;
using PolyEdgeScout.Models;
using PolyEdgeScout.Services;

// ─────────────────────────────────────────────────────────────
//  Entry point — top-level statements
// ─────────────────────────────────────────────────────────────

if (args.Length > 0 && args[0] == "--backtest")
{
    await RunBacktest();
    return;
}

AppConfig config = LoadConfiguration();

var logService = new LogService(config.LogDirectory);
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "PolyEdgeScout/1.0");

logService.Info("PolyEdge Scout starting up...");
logService.Info($"Mode: {(config.PaperMode ? "PAPER" : "LIVE")}");
logService.Info($"Scan interval: {config.ScanIntervalSeconds}s");
logService.Info($"Min edge: {config.MinEdge:P0}  Max volume: ${config.MaxVolume:N0}");

// Wire up services
var scanner = new ScannerService(config, httpClient, logService);
var probModel = new ProbabilityModelService(config, httpClient, logService);
var orders = new OrderService(config, httpClient, logService);
var dashboard = new DashboardService(config, scanner, probModel, orders, logService);

// Graceful shutdown on Ctrl+C
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    logService.Info("Shutdown signal received (Ctrl+C).");
};

try
{
    await dashboard.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Normal shutdown — no action needed
}
finally
{
    logService.Info("PolyEdge Scout shutting down...");
    logService.Dispose();
    httpClient.Dispose();
}

// ─────────────────────────────────────────────────────────────
//  Backtest entry point
// ─────────────────────────────────────────────────────────────

static async Task RunBacktest()
{
    AppConfig config = LoadConfiguration();

    var logService = new LogService(config.LogDirectory);
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("User-Agent", "PolyEdgeScout/1.0");

    var probModel = new ProbabilityModelService(config, httpClient, logService);
    var backtest = new BacktestService(config, httpClient, probModel, logService);

    logService.Info("Starting backtest mode...");

    try
    {
        await backtest.RunBacktestAsync(CancellationToken.None);
    }
    finally
    {
        logService.Dispose();
        httpClient.Dispose();
    }
}

// ─────────────────────────────────────────────────────────────
//  Configuration loader
// ─────────────────────────────────────────────────────────────

static AppConfig LoadConfiguration()
{
    // Load .env file if it exists (simple KEY=VALUE parsing)
    string envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envFile))
    {
        foreach (string line in File.ReadAllLines(envFile))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            int eqIndex = trimmed.IndexOf('=');
            if (eqIndex > 0)
            {
                string key = trimmed[..eqIndex].Trim();
                string value = trimmed[(eqIndex + 1)..].Trim();
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    IConfigurationRoot configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();

    IConfigurationSection section = configuration.GetSection("PolyEdgeScout");

    var config = new AppConfig
    {
        PolygonRpc = section[nameof(AppConfig.PolygonRpc)] ?? new AppConfig().PolygonRpc,
        PaperMode = bool.TryParse(section[nameof(AppConfig.PaperMode)], out bool pm) ? pm : true,
        ScanIntervalSeconds = int.TryParse(section[nameof(AppConfig.ScanIntervalSeconds)], out int si) ? si : 60,
        MaxVolume = double.TryParse(section[nameof(AppConfig.MaxVolume)], out double mv) ? mv : 3000,
        MinEdge = double.TryParse(section[nameof(AppConfig.MinEdge)], out double me) ? me : 0.08,
        DefaultBetSize = double.TryParse(section[nameof(AppConfig.DefaultBetSize)], out double db) ? db : 100,
        KellyFraction = double.TryParse(section[nameof(AppConfig.KellyFraction)], out double kf) ? kf : 0.5,
        MaxBankrollPercent = double.TryParse(section[nameof(AppConfig.MaxBankrollPercent)], out double mb) ? mb : 0.02,
        FadeMultiplier = double.TryParse(section[nameof(AppConfig.FadeMultiplier)], out double fm) ? fm : 0.92,
        GammaApiBaseUrl = section[nameof(AppConfig.GammaApiBaseUrl)] ?? new AppConfig().GammaApiBaseUrl,
        BinanceApiBaseUrl = section[nameof(AppConfig.BinanceApiBaseUrl)] ?? new AppConfig().BinanceApiBaseUrl,
        ClobBaseUrl = section[nameof(AppConfig.ClobBaseUrl)] ?? new AppConfig().ClobBaseUrl,
        LogDirectory = section[nameof(AppConfig.LogDirectory)] ?? new AppConfig().LogDirectory,
    };

    // Override from environment variables
    if (Environment.GetEnvironmentVariable("PAPER_MODE") is string paperEnv)
        config.PaperMode = paperEnv.Equals("true", StringComparison.OrdinalIgnoreCase);

    if (Environment.GetEnvironmentVariable("POLYGON_RPC") is string rpcEnv)
        config.PolygonRpc = rpcEnv;

    return config;
}
