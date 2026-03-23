namespace PolyEdgeScout.Infrastructure.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Application.Services;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Infrastructure.ApiClients;
using PolyEdgeScout.Infrastructure.Configuration;
using PolyEdgeScout.Infrastructure.Logging;
using PolyEdgeScout.Infrastructure.Persistence;
using PolyEdgeScout.Infrastructure.Persistence.Repositories;
using PolyEdgeScout.Infrastructure.Services;

/// <summary>
/// Extension methods for registering all PolyEdgeScout services into a DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers configuration, logging, HTTP clients, and application services.
    /// </summary>
    public static IServiceCollection AddPolyEdgeScout(this IServiceCollection services)
    {
        // Load configuration
        AppConfig config = ConfigurationLoader.Load();
        services.AddSingleton(config);

        // Logging (singleton — thread-safe, shared across app)
        services.AddSingleton<ILogService>(sp => new FileLogService(config.LogDirectory));

        // EF Core — SQLite database
        services.AddDbContext<TradingDbContext>(options =>
            options.UseSqlite(config.DatabaseConnectionString));

        // Repositories (scoped — one per DbContext lifetime)
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<ITradeResultRepository, TradeResultRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAppStateRepository, AppStateRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Audit service (scoped — uses scoped repository)
        services.AddScoped<IAuditService, AuditService>();

        // HTTP clients (typed client factory pattern — auto-injects HttpClient)
        services.AddHttpClient<IGammaApiClient, GammaApiClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "PolyEdgeScout/1.0");
        });

        services.AddHttpClient<IBinanceApiClient, BinanceApiClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "PolyEdgeScout/1.0");
        });

        services.AddHttpClient<IClobClient, PolymarketClobClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "PolyEdgeScout/1.0");
        });

        // Application services (Transient to avoid captive dependency on HttpClient-backed API clients)
        services.AddTransient<IScannerService, ScannerService>();
        services.AddTransient<IProbabilityModelService, ProbabilityModelService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddTransient<IBacktestService, BacktestService>();

        return services;
    }
}
