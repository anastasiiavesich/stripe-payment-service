using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StripeCheckoutDemo.Infrastructure;
using StripeCheckoutDemo.Services;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Inject test configuration (Stripe keys)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                { "Stripe:SecretKey", "sk_test_dummy" },
                { "Stripe:WebhookSecret", "whsec_dummy" }
            };

            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // Replace real Stripe service with fake
            services.RemoveAll<IStripeService>();
            services.AddScoped<IStripeService, FakeStripeService>();

            // Remove existing DbContext registrations
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            // Create isolated SQLite in-memory database per factory
            _connection = new SqliteConnection(
    $"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure database schema is created
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
