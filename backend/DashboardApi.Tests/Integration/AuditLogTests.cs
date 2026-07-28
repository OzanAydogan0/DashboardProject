using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class AuditLogTests
{
    [Fact]
    public async Task GetAuditLogs_SystemAdministratorAndProjectManager_EnforcesRoleAccess()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();

        var systemAdministrator = await CreateActiveUserAsync(
            factory,
            role: "Sistem Yöneticisi");

        var projectManager = await CreateActiveUserAsync(
            factory,
            role: "Proje Yöneticisi");

        using var administratorClient = factory.CreateHttpsClient();
        using var projectManagerClient = factory.CreateHttpsClient();

        var administratorToken = await LoginAsync(
            administratorClient,
            systemAdministrator);

        var projectManagerToken = await LoginAsync(
            projectManagerClient,
            projectManager);

        administratorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                administratorToken);

        projectManagerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                projectManagerToken);

        // Act
        using var administratorResponse =
            await administratorClient.GetAsync("/audit-logs");

        using var projectManagerResponse =
            await projectManagerClient.GetAsync("/audit-logs");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            administratorResponse.StatusCode);

        var logs = await administratorResponse.Content
            .ReadFromJsonAsync<List<AuditLogDto>>();

        Assert.NotNull(logs);
        Assert.NotEmpty(logs);

        Assert.Contains(
            logs,
            log => log.AuditLogId == "LOG-001");

        Assert.All(
            logs,
            log =>
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(log.AuditLogId));

                Assert.False(
                    string.IsNullOrWhiteSpace(log.EntityName));

                Assert.False(
                    string.IsNullOrWhiteSpace(log.EntityId));

                Assert.Contains(
                    log.ActionType,
                    new[] { "INSERT", "UPDATE", "DELETE" });
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            projectManagerResponse.StatusCode);
    }

    private static async Task<string> LoginAsync(
        HttpClient client,
        TestUserCredentials credentials)
    {
        var request = new LoginRequest(
            credentials.User.Email,
            credentials.PlainTextPassword);

        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        return result.Token;
    }

    private static async Task<TestUserCredentials> CreateActiveUserAsync(
        TestWebApplicationFactory factory,
        string role)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var builder = new TestDataBuilder(db);

        return await builder.CreateActiveUserAsync(role);
    }
}