using System.Security.Cryptography;
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
    private readonly string _jwtSecret =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly IReadOnlyDictionary<string, string?> _settings;

    public TestWebApplicationFactory(
        IReadOnlyDictionary<string, string?>? settings = null)
    {
        _settings = settings ??
            new Dictionary<string, string?>();
        _database = new SqliteTestDatabase();
        _database.InitializeAsync().GetAwaiter().GetResult();
    }

    public SqliteConnection Connection => _database.Connection;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("JwtSettings:Secret", _jwtSecret);
        foreach (var (key, value) in _settings)
        {
            if (value is not null)
                builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_database.Connection)
                .Options;

            services.AddSingleton(options);

            services.AddScoped<AppDbContext>(_ =>
                new AppDbContext(options));
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
}
