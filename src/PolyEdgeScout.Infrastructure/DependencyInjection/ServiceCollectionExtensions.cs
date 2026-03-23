namespace PolyEdgeScout.Infrastructure.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Application.Services;
using PolyEdgeScout.Domain.Interfaces;
using PolyEdgeScout.Infrastructure.ApiClients;
using PolyEdgeScout.Infrastructure.Configuration;
using PolyEdgeScout.Infrastructure.Logging;

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
