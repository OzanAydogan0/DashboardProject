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

public sealed class UserAdminInvariantTests
{
    [Theory]
    [InlineData("Proje Yöneticisi", null)]
    [InlineData(null, "Pasif")]
    public async Task UpdateLastActiveAdministrator_ReturnsConflict(
        string? role,
        string? status)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        var administrator = await PrepareOnlyActiveAdministratorAsync(
            factory,
            client);

        using var response = await client.PatchAsJsonAsync(
            $"/users/{administrator.User.UserId}",
            new UserUpdateDto(Role: role, Status: status));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Users
            .AsNoTracking()
            .SingleAsync(user =>
                user.UserId == administrator.User.UserId);

        Assert.Equal("Sistem Yöneticisi", persisted.UserRole);
        Assert.Equal("Aktif", persisted.UserStatus);
    }

    [Fact]
    public async Task DeleteLastActiveAdministrator_ReturnsConflict()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        var administrator = await PrepareOnlyActiveAdministratorAsync(
            factory,
            client);

        using var response = await client.DeleteAsync(
            $"/users/{administrator.User.UserId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Users.AnyAsync(user =>
            user.UserId == administrator.User.UserId));
    }

    [Fact]
    public async Task DeleteReferencedUser_ReturnsConflictAndKeepsUser()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials administrator;
        TestUserCredentials referencedUser;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var builder = new TestDataBuilder(db);
            administrator = await builder.CreateActiveUserAsync(
                "Sistem Yöneticisi");
            referencedUser = await builder.CreateActiveUserAsync(
                "Proje Yöneticisi");

            var project = await db.Projects.SingleAsync(
                candidate => candidate.ProjectId == "PRJ-001");
            project.ProjectManagerUserId = referencedUser.User.UserId;
            await db.SaveChangesAsync();
        }

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                administrator.User.Email,
                administrator.PlainTextPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        using var response = await client.DeleteAsync(
            $"/users/{referencedUser.User.UserId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var responseBody =
            await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(responseBody);
        Assert.Contains(
            "pasife alın",
            responseBody["message"],
            StringComparison.OrdinalIgnoreCase);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await verificationDb.Users.AnyAsync(
            user => user.UserId == referencedUser.User.UserId));
    }

    private static async Task<TestUserCredentials>
        PrepareOnlyActiveAdministratorAsync(
            TestWebApplicationFactory factory,
            HttpClient client)
    {
        TestUserCredentials administrator;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            administrator = await new TestDataBuilder(db)
                .CreateActiveUserAsync("Sistem Yöneticisi");

            var otherAdministrators = await db.Users
                .Where(user =>
                    user.UserId != administrator.User.UserId &&
                    user.UserRole == "Sistem Yöneticisi")
                .ToListAsync();

            foreach (var user in otherAdministrators)
                user.UserStatus = "Pasif";

            await db.SaveChangesAsync();
        }

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                administrator.User.Email,
                administrator.PlainTextPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        return administrator;
    }
}
