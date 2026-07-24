using dashboardapi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DashboardApi.Tests.Fixtures;

public sealed class TestWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly SqliteTestDatabase _database;

    public TestWebApplicationFactory()
    {
        _database = new SqliteTestDatabase();
        _database.InitializeAsync().GetAwaiter().GetResult();
    }

    public SqliteConnection Connection => _database.Connection;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_database.Connection)
                .Options;

            services.AddSingleton(options);

            services.AddScoped<AppDbContext>(_ =>
                new TestAppDbContext(options));
        });
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    public new async ValueTask DisposeAsync()
    {
        Dispose();
        await _database.DisposeAsync();
    }

    private sealed class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            // Production AppDbContext içindeki sabit bağlantı yolunun
            // test bağlantısını ezmesini engeller.
        }
    }
}