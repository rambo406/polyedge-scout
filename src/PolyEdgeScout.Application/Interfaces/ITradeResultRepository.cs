namespace PolyEdgeScout.Application.Interfaces;

using PolyEdgeScout.Domain.Entities;

public interface ITradeResultRepository
{
    Task AddAsync(TradeResult result);
    Task<List<TradeResult>> GetAllAsync();
    Task<TradeResult?> GetByTradeIdAsync(string tradeId);
}
