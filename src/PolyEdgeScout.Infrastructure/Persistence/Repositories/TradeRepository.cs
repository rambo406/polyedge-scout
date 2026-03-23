namespace PolyEdgeScout.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Enums;

public sealed class TradeRepository(TradingDbContext context) : ITradeRepository
{
    public async Task<Trade?> GetByIdAsync(string id)
        => await context.Trades.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<Trade>> GetOpenTradesAsync()
        => await context.Trades
            .Where(t => t.Status == TradeStatus.Filled)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();

    public async Task<List<Trade>> GetAllTradesAsync()
        => await context.Trades
            .OrderBy(t => t.Timestamp)
            .ToListAsync();

    public async Task AddAsync(Trade trade)
    {
        await context.Trades.AddAsync(trade);
    }

    public Task UpdateAsync(Trade trade)
    {
        context.Trades.Update(trade);
        return Task.CompletedTask;
    }
}
