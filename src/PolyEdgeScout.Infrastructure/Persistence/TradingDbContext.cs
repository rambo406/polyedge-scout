namespace PolyEdgeScout.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using PolyEdgeScout.Domain.Entities;

public sealed class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<TradeResult> TradeResults => Set<TradeResult>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<AppStateEntry> AppState => Set<AppStateEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }
}
