using dashboardapi.Data;
using dashboardapi.Models;

namespace DashboardApi.Tests.Builders;

public sealed class TestDataBuilder
{
    private readonly AppDbContext _db;

    public TestDataBuilder(AppDbContext db)
    {
        _db = db;
    }

    public Task<TestUserCredentials> CreateActiveUserAsync(
        string role = "Proje Yöneticisi",
        CancellationToken cancellationToken = default)
    {
        return CreateUserAsync(
            role,
            "Aktif",
            cancellationToken);
    }

    public async Task<TestUserCredentials> CreateUserAsync(
        string role = "Proje Yöneticisi",
        string status = "Aktif",
        CancellationToken cancellationToken = default)
    {
        var uniqueValue = Guid.NewGuid()
            .ToString("N")
            .ToUpperInvariant();

        const string plainTextPassword = "Test123!";

        var user = new User
        {
            UserId = $"TEST-USR-{uniqueValue[..8]}",
            Email = $"test-{uniqueValue}@pir.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                plainTextPassword),
            FullName = "Test Kullanıcısı",
            UserRole = role,
            UserStatus = status,
            LastLoginAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new TestUserCredentials(
            user,
            plainTextPassword);
    }
}

public sealed record TestUserCredentials(
    User User,
    string PlainTextPassword);