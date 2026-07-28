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
using dashboardapi.Models;

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

        using var listResponse = await client.GetAsync("/projects/PRJ-001/risks");

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var riskList = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, riskList.ValueKind);

        var listedRisk = Assert.Single(
            riskList.EnumerateArray(),
            item => item.GetProperty("riskId").GetString() == riskId);

        Assert.Equal(
            "Alternatif test ortamı hazırlanacak.",
            listedRisk.GetProperty("riskMitigation").GetString());
    }

    [Fact]
    public async Task UpdateRisk_OwnerUserIdChanged_UpdatesResponsibleUser()
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

        var requestBody = new
        {
            riskOwnerUserId = credentials.User.UserId
        };

        // Act
        using var response = await client.PatchAsJsonAsync(
            "/risks/RSK-001",
            requestBody);

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

        Assert.Equal(
            credentials.User.UserId,
            updatedRisk.RiskOwnerUserId);
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
        [Fact]
    public async Task GetProjectRisks_WithDifferentScores_ReturnsCorrectRiskHealth()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Sistem Yöneticisi");

        var now = DateTime.UtcNow;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            db.Risks.AddRange(
                new Risk
                {
                    RiskId = "TEST-RSK-GREEN",
                    ProjectId = "PRJ-003",
                    RiskTitle = "Yeşil sağlık seviyesi testi",
                    RiskCategory = "Teknik",
                    RiskProbability = 2,
                    RiskImpact = 2,
                    RiskScore = 0,
                    RiskOwnerUserId = credentials.User.UserId,
                    RiskMitigation = "Test azaltma planı",
                    RiskDueDate = now.AddDays(30),
                    RiskStatus = "Açık",
                    OpenedDate = now.Date,
                    ClosedDate = null,
                    CreatedByUserId = credentials.User.UserId,
                    UpdatedByUserId = credentials.User.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Risk
                {
                    RiskId = "TEST-RSK-YELLOW",
                    ProjectId = "PRJ-003",
                    RiskTitle = "Sarı sağlık seviyesi testi",
                    RiskCategory = "Takvim",
                    RiskProbability = 3,
                    RiskImpact = 3,
                    RiskScore = 0,
                    RiskOwnerUserId = credentials.User.UserId,
                    RiskMitigation = "Test azaltma planı",
                    RiskDueDate = now.AddDays(30),
                    RiskStatus = "Açık",
                    OpenedDate = now.Date,
                    ClosedDate = null,
                    CreatedByUserId = credentials.User.UserId,
                    UpdatedByUserId = credentials.User.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Risk
                {
                    RiskId = "TEST-RSK-RED",
                    ProjectId = "PRJ-003",
                    RiskTitle = "Kırmızı sağlık seviyesi testi",
                    RiskCategory = "Maliyet",
                    RiskProbability = 4,
                    RiskImpact = 5,
                    RiskScore = 0,
                    RiskOwnerUserId = credentials.User.UserId,
                    RiskMitigation = "Test azaltma planı",
                    RiskDueDate = now.AddDays(30),
                    RiskStatus = "Açık",
                    OpenedDate = now.Date,
                    ClosedDate = null,
                    CreatedByUserId = credentials.User.UserId,
                    UpdatedByUserId = credentials.User.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                });

            await db.SaveChangesAsync();
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

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        // Act
        using var response = await client.GetAsync(
            "/projects/PRJ-003/risks");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var risks =
            await response.Content.ReadFromJsonAsync<List<RiskDto>>();

        Assert.NotNull(risks);

        var greenRisk = Assert.Single(
            risks,
            risk => risk.RiskId == "TEST-RSK-GREEN");

        var yellowRisk = Assert.Single(
            risks,
            risk => risk.RiskId == "TEST-RSK-YELLOW");

        var redRisk = Assert.Single(
            risks,
            risk => risk.RiskId == "TEST-RSK-RED");

        Assert.Equal(4, greenRisk.RiskScore);
        Assert.Equal("Yeşil", greenRisk.RiskHealth);

        Assert.Equal(9, yellowRisk.RiskScore);
        Assert.Equal("Sarı", yellowRisk.RiskHealth);

        Assert.Equal(20, redRisk.RiskScore);
        Assert.Equal("Kırmızı", redRisk.RiskHealth);
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