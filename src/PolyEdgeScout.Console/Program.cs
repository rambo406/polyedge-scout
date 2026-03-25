using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.App;
using PolyEdgeScout.Console.Commands;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Console.Views;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Infrastructure.DependencyInjection;
using PolyEdgeScout.Infrastructure.Logging;
using PolyEdgeScout.Infrastructure.Persistence;

// Build DI container
var services = new ServiceCollection();
services.AddPolyEdgeScout();

// Register ViewModels (singleton — shared instances ensure VM↔View event wiring works)
services.AddSingleton<DashboardViewModel>();
services.AddSingleton<MarketTableViewModel>();
services.AddSingleton<PortfolioViewModel>();
services.AddSingleton<TradesViewModel>();
services.AddSingleton<LogViewModel>();
services.AddSingleton<BacktestViewModel>();
services.AddSingleton<FullLogViewModel>();
services.AddSingleton<TradesManagementViewModel>();
services.AddSingleton<EdgeBacktestViewModel>();

// Register Views (transient — one instance per dashboard lifecycle)
services.AddTransient<MarketTableView>();
services.AddTransient<PortfolioView>();
services.AddTransient<TradesView>();
services.AddTransient<LogPanelView>();
services.AddTransient<ErrorIndicatorView>();
services.AddTransient<FullLogView>();
services.AddTransient<TradesManagementView>();
services.AddTransient<BacktestView>();
services.AddTransient<EdgeBacktestView>();

// Register App services
services.AddTransient<AppBootstrapper>();
services.AddTransient<BacktestCommand>();

var serviceProvider = services.BuildServiceProvider();

// Resolve logger early for startup messages
var log = serviceProvider.GetRequiredService<ILogService>();
var config = serviceProvider.GetRequiredService<AppConfig>();

log.Info("PolyEdge Scout starting up...");
log.Info($"Mode: {(config.PaperMode ? "PAPER" : "LIVE")}");
log.Info($"Scan interval: {config.ScanIntervalSeconds}s");

// Wire FileLogService events to FullLogViewModel for TUI log display
var fullLogVm = serviceProvider.GetRequiredService<FullLogViewModel>();
if (log is FileLogService fileLogService)
{
    fileLogService.LogEntryWritten += fullLogVm.OnLogEntry;
}

// Ensure database directory exists and apply migrations
try
{
    var dbConnectionString = config.DatabaseConnectionString;
    var dataSourcePrefix = "Data Source=";
    if (dbConnectionString.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
    {
        var dbPath = dbConnectionString[dataSourcePrefix.Length..].Trim();
        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
            log.Info($"Created database directory: {dbDir}");
        }
    }

    using (var scope = serviceProvider.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        await dbContext.Database.MigrateAsync();
        log.Info("Database migrations applied successfully.");
    }
}
catch (Exception ex)
{
    log.Error($"Database initialization failed: {ex.Message}", ex);
    log.Warn("Continuing with in-memory state only.");
}

// Initialize OrderService — recover state from database
var orderService = serviceProvider.GetRequiredService<IOrderService>();
await orderService.InitializeAsync();

// Parse command line args
if (args.Length > 0 && args[0] == "--backtest")
{
    var backtest = serviceProvider.GetRequiredService<BacktestCommand>();
    await backtest.ExecuteAsync(CancellationToken.None);
}
else
{
    // Normal dashboard mode
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var bootstrapper = serviceProvider.GetRequiredService<AppBootstrapper>();

    try
    {
        bootstrapper.Run(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown via Ctrl+C
    }
}

log.Info("PolyEdge Scout shutting down...");

// Dispose resources
if (log is IDisposable disposableLog)
    disposableLog.Dispose();
if (serviceProvider is IDisposable disposableSp)
    disposableSp.Dispose();
