using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class ReleaseSecurityTests
{
    [Fact]
    public async Task Health_WithoutAuthentication_ReturnsHealthy()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginRateLimit_TwoTrustedProxyHops_UsesForwardedClientIp()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownProxies"] =
                "127.0.0.1;::1;203.0.113.10",
            ["ReverseProxy:ForwardLimit"] = "2"
        };

        await using var factory =
            new TestWebApplicationFactory(settings);
        using var firstClient = factory.CreateHttpsClient();
        using var secondClient = factory.CreateHttpsClient();
        firstClient.DefaultRequestHeaders.Add(
            "X-Forwarded-For",
            "198.51.100.10, 203.0.113.10");
        secondClient.DefaultRequestHeaders.Add(
            "X-Forwarded-For",
            "198.51.100.11, 203.0.113.10");

        TestUserCredentials credentials;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            credentials = await new TestDataBuilder(db)
                .CreateActiveUserAsync("Proje Yöneticisi");
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failedResponse = await firstClient.PostAsJsonAsync(
                "/auth/login",
                new LoginRequest(
                    credentials.User.Email,
                    "Definitely-Wrong-42!"));
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                failedResponse.StatusCode);
        }

        using var limitedResponse = await firstClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                credentials.User.Email,
                "Definitely-Wrong-42!"));
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            limitedResponse.StatusCode);

        using var independentResponse = await secondClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                credentials.User.Email,
                credentials.PlainTextPassword));
        Assert.Equal(HttpStatusCode.OK, independentResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidForwardLimit_FailsApplicationStartup()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ReverseProxy:ForwardLimit"] = "0"
        };

        await using var factory =
            new TestWebApplicationFactory(settings);

        var exception = Assert.ThrowsAny<Exception>(
            () => factory.CreateHttpsClient());

        Assert.Contains(
            "ReverseProxy:ForwardLimit",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task FreshProductionDatabase_CreatesSchemaAndFirstAdministrator()
    {
        const string email = "first-admin@example.test";
        const string fullName = "İlk Sistem Yöneticisi";
        const string password = "Strong-Admin-42!";

        await using var factory =
            new FreshProductionDatabaseFactory(
                email,
                fullName,
                password);
        using var client = factory.CreateHttpsClient();

        using var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                Email = email,
                Password = password
            });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var administrator = await db.Users
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(email, administrator.Email);
        Assert.Equal(fullName, administrator.FullName);
        Assert.Equal("Sistem Yöneticisi", administrator.UserRole);
        Assert.Equal("Aktif", administrator.UserStatus);
        Assert.NotEqual(password, administrator.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(
            password,
            administrator.PasswordHash));
    }

    private sealed class FreshProductionDatabaseFactory
        : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"dashboard-production-smoke-{Guid.NewGuid():N}.db");
        private readonly string _email;
        private readonly string _fullName;
        private readonly string _password;

        public FreshProductionDatabaseFactory(
            string email,
            string fullName,
            string password)
        {
            _email = email;
            _fullName = fullName;
            _password = password;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"Data Source={_databasePath};Pooling=False");
            builder.UseSetting(
                "JwtSettings:Secret",
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(48)));
            builder.UseSetting(
                "Licensing:QuestPDF:LicenseType",
                "Community");
            builder.UseSetting("BootstrapAdmin:Email", _email);
            builder.UseSetting("BootstrapAdmin:FullName", _fullName);
            builder.UseSetting("BootstrapAdmin:Password", _password);
        }

        public HttpClient CreateHttpsClient() =>
            CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        public new async ValueTask DisposeAsync()
        {
            Dispose();
            await Task.Yield();

            foreach (var path in new[]
                     {
                         _databasePath,
                         _databasePath + "-shm",
                         _databasePath + "-wal"
                     })
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    private sealed record HealthResponse(string Status);
}
