namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

public interface ITradeRepository
{
    Task<Trade?> GetByIdAsync(string id);
    Task<List<Trade>> GetOpenTradesAsync();
    Task<List<Trade>> GetAllTradesAsync();
    Task AddAsync(Trade trade);
    Task UpdateAsync(Trade trade);
}
