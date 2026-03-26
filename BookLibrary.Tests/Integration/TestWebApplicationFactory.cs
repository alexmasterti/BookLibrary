using BookLibrary.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BookLibrary.Tests.Integration;

/// <summary>
/// CONCEPT: WebApplicationFactory
///   Spins up the REAL application in memory for testing.
///   No actual HTTP server — tests call through the pipeline directly.
///   We swap the real SQLite file with a shared in-memory SQLite connection
///   so tests are isolated and fast, and avoid the EF Core 9 two-provider conflict.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep the connection open for the lifetime of the factory so the
    // in-memory SQLite database persists across requests.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            // Remove the real SQLite DbContext registrations
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor != null) services.Remove(optionsDescriptor);

            var contextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppDbContext));
            if (contextDescriptor != null) services.Remove(contextDescriptor);

            // Use the same SQLite provider but with an in-memory connection —
            // no two-provider conflict, and the schema works the same as production.
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create schema after the host is built with the correct (in-memory) options.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}
