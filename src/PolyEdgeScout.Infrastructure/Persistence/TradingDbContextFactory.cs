namespace PolyEdgeScout.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// Used by `dotnet ef migrations` commands.
/// </summary>
public sealed class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
        optionsBuilder.UseSqlite("Data Source=data/polyedge-scout.db");

        return new TradingDbContext(optionsBuilder.Options);
    }
}
