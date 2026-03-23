namespace PolyEdgeScout.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;

public sealed class AppStateRepository(TradingDbContext context) : IAppStateRepository
{
    private const string BankrollKey = "Bankroll";

    public async Task<string?> GetValueAsync(string key)
    {
        var entry = await context.AppState.FirstOrDefaultAsync(s => s.Key == key);
        return entry?.Value;
    }

    public async Task SetValueAsync(string key, string value)
    {
        var existing = await context.AppState.FirstOrDefaultAsync(s => s.Key == key);
        if (existing is not null)
        {
            // Remove and re-add because AppStateEntry is a record with init-only props
            context.AppState.Remove(existing);
            await context.AppState.AddAsync(new AppStateEntry
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow,
            });
        }
        else
        {
            await context.AppState.AddAsync(new AppStateEntry
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow,
            });
        }
    }

    public async Task<double> GetBankrollAsync()
    {
        var value = await GetValueAsync(BankrollKey);
        return value is not null && double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var bankroll)
            ? bankroll
            : 10_000.0;
    }

    public async Task SetBankrollAsync(double bankroll)
    {
        await SetValueAsync(BankrollKey, bankroll.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
