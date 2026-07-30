using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class JwtRevocationTests
{
    [Theory]
    [InlineData("deactivate")]
    [InlineData("change-role")]
    [InlineData("change-password")]
    [InlineData("delete")]
    public async Task ExistingToken_WhenUserSecurityStateChanges_ReturnsUnauthorized(
        string mutation)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            credentials = await new TestDataBuilder(db)
                .CreateActiveUserAsync("Proje Yöneticisi");
        }

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                credentials.User.Email,
                credentials.PlainTextPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(item =>
                item.UserId == credentials.User.UserId);

            switch (mutation)
            {
                case "deactivate":
                    user.UserStatus = "Pasif";
                    break;
                case "change-role":
                    user.UserRole = "Üst Yönetim İzleyicisi";
                    break;
                case "change-password":
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                        "Different-Password-42!");
                    user.UpdatedAt = DateTime.UtcNow.AddMinutes(1);
                    break;
                case "delete":
                    db.Users.Remove(user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }

            await db.SaveChangesAsync();
        }

        using var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
