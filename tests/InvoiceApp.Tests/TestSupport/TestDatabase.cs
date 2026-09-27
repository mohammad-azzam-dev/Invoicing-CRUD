using InvoiceApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Tests.TestSupport;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public IDbContextFactory<AppDbContext> Factory { get; }

    private TestDatabase(SqliteConnection connection, IDbContextFactory<AppDbContext> factory)
    {
        _connection = connection;
        Factory = factory;
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        var factory = new TestDbContextFactory(options);

        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        return new TestDatabase(connection, factory);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreateDbContext());
    }
}
