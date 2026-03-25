namespace PolyEdgeScout.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;

public sealed class TradeResultRepository(TradingDbContext context) : ITradeResultRepository
{
    public async Task AddAsync(TradeResult result)
    {
        await context.TradeResults.AddAsync(result);
    }

    public async Task<List<TradeResult>> GetAllAsync()
        => await context.TradeResults
            .OrderBy(r => r.SettledAt)
            .ToListAsync();

    public async Task<TradeResult?> GetByTradeIdAsync(string tradeId)
        => await context.TradeResults.FirstOrDefaultAsync(r => r.TradeId == tradeId);

    public async Task DeleteAllAsync()
    {
        await context.TradeResults.ExecuteDeleteAsync();
    }
}
