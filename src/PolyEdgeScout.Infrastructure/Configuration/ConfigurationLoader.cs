namespace PolyEdgeScout.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;
using PolyEdgeScout.Application.Configuration;

/// <summary>
/// Static helper that loads <see cref="AppConfig"/> from appsettings.json, .env file,
/// and environment variables with sensible fallback defaults.
/// </summary>
public static class ConfigurationLoader
{
    /// <summary>
    /// Loads AppConfig from appsettings.json + .env file + environment variables.
    /// </summary>
    public static AppConfig Load()
    {
        // 1. Parse .env file if it exists (sets env vars for subsequent reads)
        LoadEnvFile();

        // 2. Build IConfiguration from appsettings.json + env vars
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // 3. Map to AppConfig from the "PolyEdgeScout" section
        var config = new AppConfig();
        IConfigurationSection section = configuration.GetSection("PolyEdgeScout");

        if (section["PolygonRpc"] is string polygonRpc)
            config.PolygonRpc = polygonRpc;

        if (bool.TryParse(section["PaperMode"], out bool paperMode))
            config.PaperMode = paperMode;

        if (int.TryParse(section["ScanIntervalSeconds"], out int scanInterval))
            config.ScanIntervalSeconds = scanInterval;

        if (double.TryParse(section["MaxVolume"], out double maxVolume))
            config.MaxVolume = maxVolume;

        if (double.TryParse(section["MinVolume"], out double minVolume))
            config.MinVolume = minVolume;

        if (double.TryParse(section["MinEdge"], out double minEdge))
            config.MinEdge = minEdge;

        if (double.TryParse(section["DefaultBetSize"], out double defaultBetSize))
            config.DefaultBetSize = defaultBetSize;

        if (double.TryParse(section["KellyFraction"], out double kellyFraction))
            config.KellyFraction = kellyFraction;

        if (double.TryParse(section["MaxBankrollPercent"], out double maxBankrollPercent))
            config.MaxBankrollPercent = maxBankrollPercent;

        if (double.TryParse(section["FadeMultiplier"], out double fadeMultiplier))
            config.FadeMultiplier = fadeMultiplier;

        if (section["GammaApiBaseUrl"] is string gammaApiBaseUrl)
            config.GammaApiBaseUrl = gammaApiBaseUrl;

        if (section["BinanceApiBaseUrl"] is string binanceApiBaseUrl)
            config.BinanceApiBaseUrl = binanceApiBaseUrl;

        if (section["ClobBaseUrl"] is string clobBaseUrl)
            config.ClobBaseUrl = clobBaseUrl;

        // Bind ServerEventTagId from configuration
        if (int.TryParse(section["ServerEventTagId"], out int serverEventTagId))
            config.ServerEventTagId = serverEventTagId;

        // Bind InMemoryEventTagIds array from configuration
        var inMemoryTagIdsSection = section.GetSection("InMemoryEventTagIds");
        if (inMemoryTagIdsSection.Exists())
        {
            var tagIds = inMemoryTagIdsSection.GetChildren()
                .Select(c => int.TryParse(c.Value, out int v) ? v : (int?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (tagIds.Count > 0)
                config.InMemoryEventTagIds = tagIds;
        }

        if (section["LogDirectory"] is string logDirectory)
            config.LogDirectory = logDirectory;

        if (section["DatabaseConnectionString"] is string dbConnectionString)
            config.DatabaseConnectionString = dbConnectionString;

        // 4. Override from environment variables (highest priority)
        if (Environment.GetEnvironmentVariable("POLYGON_RPC") is string envPolygonRpc)
            config.PolygonRpc = envPolygonRpc;

        if (Environment.GetEnvironmentVariable("PAPER_MODE") is string envPaperMode)
            config.PaperMode = envPaperMode.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (Environment.GetEnvironmentVariable("SCAN_INTERVAL_SECONDS") is string envScanInterval
            && int.TryParse(envScanInterval, out int envScanIntervalVal))
            config.ScanIntervalSeconds = envScanIntervalVal;

        if (Environment.GetEnvironmentVariable("MAX_VOLUME") is string envMaxVolume
            && double.TryParse(envMaxVolume, out double envMaxVolumeVal))
            config.MaxVolume = envMaxVolumeVal;

        if (Environment.GetEnvironmentVariable("MIN_VOLUME") is string envMinVolume
            && double.TryParse(envMinVolume, out double envMinVolumeVal))
            config.MinVolume = envMinVolumeVal;

        if (Environment.GetEnvironmentVariable("MIN_EDGE") is string envMinEdge
            && double.TryParse(envMinEdge, out double envMinEdgeVal))
            config.MinEdge = envMinEdgeVal;

        if (Environment.GetEnvironmentVariable("DEFAULT_BET_SIZE") is string envDefaultBetSize
            && double.TryParse(envDefaultBetSize, out double envDefaultBetSizeVal))
            config.DefaultBetSize = envDefaultBetSizeVal;

        if (Environment.GetEnvironmentVariable("KELLY_FRACTION") is string envKellyFraction
            && double.TryParse(envKellyFraction, out double envKellyFractionVal))
            config.KellyFraction = envKellyFractionVal;

        if (Environment.GetEnvironmentVariable("MAX_BANKROLL_PERCENT") is string envMaxBankrollPercent
            && double.TryParse(envMaxBankrollPercent, out double envMaxBankrollPercentVal))
            config.MaxBankrollPercent = envMaxBankrollPercentVal;

        if (Environment.GetEnvironmentVariable("FADE_MULTIPLIER") is string envFadeMultiplier
            && double.TryParse(envFadeMultiplier, out double envFadeMultiplierVal))
            config.FadeMultiplier = envFadeMultiplierVal;

        if (Environment.GetEnvironmentVariable("GAMMA_API_BASE_URL") is string envGammaApiBaseUrl)
            config.GammaApiBaseUrl = envGammaApiBaseUrl;

        if (Environment.GetEnvironmentVariable("BINANCE_API_BASE_URL") is string envBinanceApiBaseUrl)
            config.BinanceApiBaseUrl = envBinanceApiBaseUrl;

        if (Environment.GetEnvironmentVariable("CLOB_BASE_URL") is string envClobBaseUrl)
            config.ClobBaseUrl = envClobBaseUrl;

        // SERVER_EVENT_TAG_ID env var: single tag ID for server-side filtering
        if (Environment.GetEnvironmentVariable("SERVER_EVENT_TAG_ID") is string envServerEventTagId
            && int.TryParse(envServerEventTagId, out int envServerEventTagIdVal))
            config.ServerEventTagId = envServerEventTagIdVal;

        // IN_MEMORY_EVENT_TAG_IDS env var: comma-separated list of tag IDs (e.g., "21")
        if (Environment.GetEnvironmentVariable("IN_MEMORY_EVENT_TAG_IDS") is string envInMemoryTagIds)
        {
            var tagIds = envInMemoryTagIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int v) ? v : (int?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (tagIds.Count > 0)
                config.InMemoryEventTagIds = tagIds;
        }

        if (Environment.GetEnvironmentVariable("LOG_DIRECTORY") is string envLogDirectory)
            config.LogDirectory = envLogDirectory;

        if (Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") is string envDbConnectionString)
            config.DatabaseConnectionString = envDbConnectionString;

        return config;
    }

    /// <summary>
    /// Loads environment variables from a .env file in the current directory (if present).
    /// Lines starting with '#' are treated as comments. Format: KEY=VALUE.
    /// </summary>
    private static void LoadEnvFile()
    {
        string envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(envFile))
            return;

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
}
