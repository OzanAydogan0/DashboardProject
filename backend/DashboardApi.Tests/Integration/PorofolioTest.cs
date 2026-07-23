using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class PortfolioTests
{
    [Fact]
    public async Task GetPortfolio_WithSelectedProjectIds_ReturnsOnlySelectedProjects()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;
        int projectCountBeforeRequest;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var builder = new TestDataBuilder(db);

            credentials = await builder.CreateActiveUserAsync(
                role: "Sistem Yöneticisi");

            projectCountBeforeRequest =
                await db.Projects.CountAsync();

            Assert.True(
                projectCountBeforeRequest >= 3,
                "Test 33 için test veritabanında en az üç proje bulunmalıdır.");

            Assert.True(
                await db.Projects.AnyAsync(
                    project => project.ProjectId == "PRJ-001"));

            Assert.True(
                await db.Projects.AnyAsync(
                    project => project.ProjectId == "PRJ-002"));

            Assert.True(
                await db.Projects.AnyAsync(
                    project => project.ProjectId == "PRJ-003"));
        }

        var loginRequest = new LoginRequest(
            credentials.User.Email,
            credentials.PlainTextPassword);

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        // Act
        using var response = await client.GetAsync(
            "/dashboard/portfolio" +
            "?projectIds=PRJ-001" +
            "&projectIds=PRJ-003");

        // Assert
        Assert.NotEqual(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        using var jsonDocument =
            JsonDocument.Parse(responseBody);

        Assert.Equal(
            JsonValueKind.Array,
            jsonDocument.RootElement.ValueKind);

        var returnedProjectIds = jsonDocument.RootElement
            .EnumerateArray()
            .Select(ReadProjectId)
            .ToList();

        var expectedProjectIds = new[]
        {
            "PRJ-001",
            "PRJ-003"
        };

        Assert.Equal(
            expectedProjectIds.Length,
            returnedProjectIds.Count);

        Assert.Contains(
            "PRJ-001",
            returnedProjectIds);

        Assert.Contains(
            "PRJ-003",
            returnedProjectIds);

        Assert.DoesNotContain(
            "PRJ-002",
            returnedProjectIds);

        Assert.Equal(
            expectedProjectIds.OrderBy(id => id),
            returnedProjectIds.OrderBy(id => id));

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var projectCountAfterRequest =
            await verificationDb.Projects.CountAsync();

        Assert.Equal(
            projectCountBeforeRequest,
            projectCountAfterRequest);
    }

    private static string ReadProjectId(
        JsonElement projectElement)
    {
        if (projectElement.TryGetProperty(
                "projectId",
                out var camelCaseProperty))
        {
            return camelCaseProperty.GetString()
                ?? throw new Xunit.Sdk.XunitException(
                    "Response içindeki projectId değeri null.");
        }

        if (projectElement.TryGetProperty(
                "ProjectId",
                out var pascalCaseProperty))
        {
            return pascalCaseProperty.GetString()
                ?? throw new Xunit.Sdk.XunitException(
                    "Response içindeki ProjectId değeri null.");
        }

        throw new Xunit.Sdk.XunitException(
            "Portföy response öğesinde projectId alanı bulunamadı.");
    }
}