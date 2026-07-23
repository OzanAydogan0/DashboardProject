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

public sealed class RiskTests
{
    [Fact]
    public async Task CreateRisk_ValidRequest_CalculatesRiskScoreAutomatically()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Sistem Yöneticisi");

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

        var request = new CreateRiskRequest(
            ProjectId: "PRJ-001",
            RiskTitle: "Test ortamının zamanında hazır olmaması",
            RiskCategory: "Teknik",
            RiskProbability: 4,
            RiskImpact: 5,
            RiskOwnerUserId: credentials.User.UserId,
            RiskMitigation: "Alternatif test ortamı hazırlanacak.",
            RiskDueDate: new DateTime(2026, 12, 15),
            RiskStatus: "Açık");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/risks",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            responseBody.TryGetProperty(
                "riskId",
                out var riskIdProperty));

        var riskId = riskIdProperty.GetString();

        Assert.False(string.IsNullOrWhiteSpace(riskId));

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var createdRisk = await db.Risks
            .AsNoTracking()
            .SingleOrDefaultAsync(risk =>
                risk.RiskId == riskId);

        Assert.NotNull(createdRisk);

        Assert.Equal("PRJ-001", createdRisk.ProjectId);
        Assert.Equal(4, createdRisk.RiskProbability);
        Assert.Equal(5, createdRisk.RiskImpact);

        // 4 × 5 = 20
        Assert.Equal(20, createdRisk.RiskScore);

        Assert.Equal("Açık", createdRisk.RiskStatus);

        Assert.Equal(
            credentials.User.UserId,
            createdRisk.RiskOwnerUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdRisk.CreatedByUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdRisk.UpdatedByUserId);
    }

        [Fact]
    public async Task UpdateRisk_ProbabilityAndImpactChanged_RecalculatesRiskScore()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Sistem Yöneticisi");

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
        Assert.False(
            string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        var request = new UpdateRiskRequest(
            RiskTitle: null,
            RiskCategory: null,
            RiskProbability: 2,
            RiskImpact: 3,
            RiskStatus: null,
            RiskMitigation: null,
            RiskDueDate: null);

        // Act
        using var response = await client.PatchAsJsonAsync(
            "/risks/RSK-001",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var updatedRisk = await db.Risks
            .AsNoTracking()
            .SingleAsync(risk =>
                risk.RiskId == "RSK-001");

        Assert.Equal(2, updatedRisk.RiskProbability);
        Assert.Equal(3, updatedRisk.RiskImpact);

        // 2 × 3 = 6
        Assert.Equal(6, updatedRisk.RiskScore);

        Assert.Equal(
            credentials.User.UserId,
            updatedRisk.UpdatedByUserId);
    }

        [Fact]
    public async Task CreateRisk_UpperManagementViewer_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Üst Yönetim İzleyicisi");

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
        Assert.False(
            string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        var request = new CreateRiskRequest(
            ProjectId: "PRJ-001",
            RiskTitle: "Üst yönetim yetki kontrolü",
            RiskCategory: "Teknik",
            RiskProbability: 3,
            RiskImpact: 4,
            RiskOwnerUserId: credentials.User.UserId,
            RiskMitigation: "Bu kayıt oluşturulmamalıdır.",
            RiskDueDate: new DateTime(2026, 12, 31),
            RiskStatus: "Açık");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/risks",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var riskWasCreated = await db.Risks
            .AsNoTracking()
            .AnyAsync(risk =>
                risk.RiskTitle == "Üst yönetim yetki kontrolü");

        Assert.False(riskWasCreated);
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