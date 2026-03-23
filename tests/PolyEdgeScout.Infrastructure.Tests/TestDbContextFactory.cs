using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PolyEdgeScout.Infrastructure.Persistence;

namespace PolyEdgeScout.Infrastructure.Tests;

/// <summary>
/// Creates an in-memory SQLite TradingDbContext for testing.
/// The connection is kept open for the lifetime of the test to preserve the in-memory database.
/// </summary>
public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public TradingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new TradingDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
