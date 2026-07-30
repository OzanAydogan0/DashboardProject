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

public sealed class MilestoneTests
{
    [Fact]
    public async Task CreateMilestone_AuthorizedUserWithValidRequest_CreatesMilestone()
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

        var plannedDate = new DateTime(2026, 11, 15);
        var forecastDate = new DateTime(2026, 11, 20);

        var request = new CreateMilestoneRequest(
            MilestoneName: "Entegrasyon kabulünün tamamlanması",
            PlannedDate: plannedDate,
            ForecastDate: forecastDate,
            MilestoneStatus: "Planlandı",
            Critical: 1,
            MilestoneOwnerUserId: credentials.User.UserId,
            AcceptanceCriteria: "Entegrasyon testlerinin başarıyla tamamlanması",
            MilestoneDescription: "Test 25 için oluşturulan kilometre taşı");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/projects/PRJ-003/milestones",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            responseBody.TryGetProperty(
                "milestoneId",
                out var milestoneIdProperty));

        var milestoneId = milestoneIdProperty.GetString();

        Assert.False(string.IsNullOrWhiteSpace(milestoneId));
        Assert.StartsWith("MS-", milestoneId);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var createdMilestone = await db.Milestones
            .AsNoTracking()
            .SingleOrDefaultAsync(milestone =>
                milestone.MilestoneId == milestoneId);

        Assert.NotNull(createdMilestone);

        Assert.Equal(
            "PRJ-003",
            createdMilestone.ProjectId);

        Assert.Equal(
            "Entegrasyon kabulünün tamamlanması",
            createdMilestone.MilestoneName);

        Assert.Equal(
            plannedDate,
            createdMilestone.PlannedDate);

        Assert.Equal(
            forecastDate,
            createdMilestone.ForecastDate);

        Assert.Equal(
            "Planlandı",
            createdMilestone.MilestoneStatus);

        Assert.Equal(
            1,
            createdMilestone.Critical);

        Assert.Equal(
            credentials.User.UserId,
            createdMilestone.MilestoneOwnerUserId);

        Assert.Null(createdMilestone.ActualDate);

        Assert.Equal(
            credentials.User.UserId,
            createdMilestone.CreatedByUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdMilestone.UpdatedByUserId);
    }

    //Mevcut endpoint tarihleri önceden doğrulamıyor ve DbUpdateException yakalamıyor.
    [Fact]
    public async Task CreateMilestone_ForecastDateBeforePlannedDate_ReturnsBadRequest()
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

        const string milestoneName =
            "Geçersiz tarih sıralı kilometre taşı";

        var request = new CreateMilestoneRequest(
            MilestoneName: milestoneName,
            PlannedDate: new DateTime(2026, 12, 20),

            // Tahmini tarih, planlanan tarihten önce.
            ForecastDate: new DateTime(2026, 12, 10),

            MilestoneStatus: "Planlandı",
            Critical: 0,
            MilestoneOwnerUserId: credentials.User.UserId,
            AcceptanceCriteria: "Bu kayıt oluşturulmamalıdır.",
            MilestoneDescription: "Geçersiz tarih sırası testi");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/projects/PRJ-003/milestones",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var milestoneWasCreated = await db.Milestones
            .AsNoTracking()
            .AnyAsync(milestone =>
                milestone.MilestoneName == milestoneName);

        Assert.False(milestoneWasCreated);
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
