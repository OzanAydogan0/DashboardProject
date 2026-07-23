using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Tests.Integration;

public sealed class PirReportTests
{
    //test verisi yok 
    [Fact]
    public async Task GetProjectPirs_ExistingProject_ReturnsProjectPirReports()
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

        // Act
        using var response = await client.GetAsync(
            "/projects/PRJ-001/pirs");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var reports =
            await response.Content.ReadFromJsonAsync<List<PirReportDto>>();

        Assert.NotNull(reports);

        var report = Assert.Single(reports);

        Assert.Equal("PIR-01", report.PirReportId);
        Assert.Equal("PRJ-001", report.ProjectId);
        Assert.Equal("PRJ-001", report.ProjectCode);
        Assert.Equal("2026-06", report.Period);

        Assert.Equal(
            "Kırmızı",
            report.ManualHealth);

        Assert.Equal(
            "Yayımlandı",
            report.ReportStatus);

        Assert.False(
            string.IsNullOrWhiteSpace(report.ExecutiveSummary));

        Assert.False(
            string.IsNullOrWhiteSpace(report.CompletedWork));

        Assert.NotNull(report.PublishedAt);
    }
    [Fact]
    public async Task CreatePirReport_ValidDraftRequest_CreatesPirReport()
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

        const string period = "2026-08";
        const string executiveSummary =
            "Ağustos dönemi entegrasyon çalışmaları devam etmektedir.";

        var request = new CreatePirReportRequest(
            ProjectId: "PRJ-001",
            Period: period,
            ReportDate: new DateTime(2026, 8, 31),
            ExecutiveSummary: executiveSummary,
            CompletedWork: "Test ve entegrasyon hazırlıkları tamamlandı.",
            Delays: null,
            NextPeriodPlan: "Kabul testleri gerçekleştirilecek.",
            ManagementExpectations: "Test ortamı desteği beklenmektedir.",
            ManualHealth: "Sarı",
            ReportStatus: "Taslak");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/pirs",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            responseBody.TryGetProperty(
                "pirId",
                out var pirIdProperty));

        var pirId = pirIdProperty.GetString();

        Assert.False(string.IsNullOrWhiteSpace(pirId));

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var createdReport = await db.PirReports
            .AsNoTracking()
            .SingleOrDefaultAsync(report =>
                report.PirReportId == pirId);

        Assert.NotNull(createdReport);

        Assert.Equal(
            "PRJ-001",
            createdReport.ProjectId);

        Assert.Equal(
            period,
            createdReport.Period);

        Assert.Equal(
            executiveSummary,
            createdReport.ExecutiveSummary);

        Assert.Equal(
            "Taslak",
            createdReport.ReportStatus);

        Assert.Equal(
            "Sarı",
            createdReport.ManualHealth);

        Assert.Null(createdReport.PublishedAt);
        Assert.Null(createdReport.PublishedByUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdReport.CreatedByUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdReport.UpdatedByUserId);
    }

    /*GET projects/{id}/pirs endpoint’i korumasız kalmış.*/
        [Fact]
    public async Task GetProjectPirs_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        // Authorization header özellikle eklenmiyor.

        // Act
        using var response = await client.GetAsync(
            "/projects/PRJ-001/pirs");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
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