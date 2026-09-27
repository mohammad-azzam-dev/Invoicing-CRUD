using InvoiceApp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceApp.Tests.TestSupport;

public sealed class InvoiceAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = default!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContextFactory registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IDbContextFactory<AppDbContext>)
            );
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Add test DbContextFactory using the open in-memory connection
            services.AddSingleton<IDbContextFactory<AppDbContext>>(sp =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connection)
                    .Options;

                return new TestDbContextFactory(options);
            });

            // Ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<AppDbContext>
            >();
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
        });
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
