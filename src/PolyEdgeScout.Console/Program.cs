using Microsoft.Extensions.DependencyInjection;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.Commands;
using PolyEdgeScout.Console.UI;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Infrastructure.DependencyInjection;
using PolyEdgeScout.Application.Configuration;

// Build DI container
var services = new ServiceCollection();
services.AddPolyEdgeScout();

// Register console-specific services
services.AddTransient<DashboardService>();
services.AddTransient<BacktestCommand>();

var serviceProvider = services.BuildServiceProvider();

// Resolve logger early for startup messages
var log = serviceProvider.GetRequiredService<ILogService>();
var config = serviceProvider.GetRequiredService<AppConfig>();

log.Info("PolyEdge Scout starting up...");
log.Info($"Mode: {(config.PaperMode ? "PAPER" : "LIVE")}");
log.Info($"Scan interval: {config.ScanIntervalSeconds}s");

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

    var dashboard = serviceProvider.GetRequiredService<DashboardService>();

    try
    {
        await dashboard.RunAsync(cts.Token);
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
