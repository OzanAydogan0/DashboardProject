using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class UpperManagementAuthorizationTests
{
    [Theory]
    [InlineData("actions")]
    [InlineData("risks")]
    [InlineData("issues")]
    [InlineData("milestones")]
    [InlineData("pirs")]
    public async Task UpperManagement_WriteEndpoint_ReturnsForbidden(
        string endpointType)
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var builder = new TestDataBuilder(db);

            credentials = await builder.CreateActiveUserAsync(
                role: "Üst Yönetim İzleyicisi");

            /*
             * Kullanıcı projeye özellikle atanıyor.
             * Böylece 403 sonucunun proje erişimsizliğinden değil,
             * salt-okunur rol kuralından gelmesi beklenir.
             */
            db.ProjectUsers.Add(new ProjectUser
            {
                ProjectUserId = $"TEST-PU-{Guid.NewGuid():N}",
                ProjectId = "PRJ-003",
                UserId = credentials.User.UserId,
                AssignedByUserId = "USR-ADMIN",
                AssignmentStatus = "Aktif",
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        var marker = $"AC11-{endpointType}-{Guid.NewGuid():N}";

        var recordsBefore = await CountMatchingRecordsAsync(
            factory,
            endpointType,
            marker);

        var requestData = CreateRequest(
            endpointType,
            marker,
            credentials.User.UserId);

        using var requestContent = JsonContent.Create(
            requestData.Payload,
            requestData.Payload.GetType());

        // Act
        using var response = await client.PostAsync(
            requestData.Route,
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var recordsAfter = await CountMatchingRecordsAsync(
            factory,
            endpointType,
            marker);

        Assert.Equal(
            recordsBefore,
            recordsAfter);
    }

    private static (string Route, object Payload) CreateRequest(
        string endpointType,
        string marker,
        string userId)
    {
        return endpointType switch
        {
            "actions" => (
                "/actions",
                new CreateActionRequest(
                    ProjectId: "PRJ-003",
                    ActionDescription: marker,
                    SourceType: "Diğer",
                    SourceReference: null,
                    ActionOwnerUserId: userId,
                    ActionDueDate: DateTime.UtcNow.Date.AddDays(30),
                    ActionStatus: "Açık",
                    ActionProgress: 0m,
                    ActionPriority: "Orta")),

            "risks" => (
                "/risks",
                new CreateRiskRequest(
                    ProjectId: "PRJ-003",
                    RiskTitle: marker,
                    RiskCategory: "Teknik",
                    RiskProbability: 3,
                    RiskImpact: 3,
                    RiskOwnerUserId: userId,
                    RiskMitigation: "Bu kayıt oluşturulmamalıdır.",
                    RiskDueDate: DateTime.UtcNow.Date.AddDays(30),
                    RiskStatus: "Açık")),

            "issues" => (
                "/issues",
                new CreateIssueRequest(
                    ProjectId: "PRJ-003",
                    IssueTitle: marker,
                    IssuePriority: "Orta",
                    IssueOwnerUserId: userId,
                    IssueDueDate: DateTime.UtcNow.Date.AddDays(30),
                    IssueStatus: "Açık",
                    IssueImpact: "Orta",
                    RootCause: "Yetki testi")),

            "milestones" => (
                "/projects/PRJ-003/milestones",
                new CreateMilestoneRequest(
                    MilestoneName: marker,
                    PlannedDate: DateTime.UtcNow.Date.AddDays(20),
                    ForecastDate: DateTime.UtcNow.Date.AddDays(25),
                    MilestoneStatus: "Planlandı",
                    Critical: 0,
                    MilestoneOwnerUserId: userId,
                    AcceptanceCriteria:
                        "Bu kayıt oluşturulmamalıdır.",
                    MilestoneDescription: "AC-11 yetki testi")),

            "pirs" => (
                "/pirs",
                new CreatePirReportRequest(
                    ProjectId: "PRJ-003",
                    Period: CreateUniquePeriod(marker),
                    ReportDate: new DateTime(2027, 2, 28),
                    ExecutiveSummary: marker,
                    CompletedWork: "Bu kayıt oluşturulmamalıdır.",
                    Delays: null,
                    NextPeriodPlan: "Yetki testi",
                    ManagementExpectations: null,
                    ManualHealth: "Gri",
                    ReportStatus: "Taslak")),

            _ => throw new ArgumentOutOfRangeException(
                nameof(endpointType),
                endpointType,
                "Desteklenmeyen yazma endpoint'i.")
        };
    }

    private static async Task<int> CountMatchingRecordsAsync(
        TestWebApplicationFactory factory,
        string endpointType,
        string marker)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        return endpointType switch
        {
            "actions" => await db.Actions.CountAsync(
                action =>
                    action.ActionDescription == marker),

            "risks" => await db.Risks.CountAsync(
                risk =>
                    risk.RiskTitle == marker),

            "issues" => await db.Issues.CountAsync(
                issue =>
                    issue.IssueTitle == marker),

            "milestones" => await db.Milestones.CountAsync(
                milestone =>
                    milestone.MilestoneName == marker),

            "pirs" => await db.PirReports.CountAsync(
                report =>
                    report.ExecutiveSummary == marker),

            _ => throw new ArgumentOutOfRangeException(
                nameof(endpointType),
                endpointType,
                "Desteklenmeyen yazma endpoint'i.")
        };
    }

    private static string CreateUniquePeriod(string marker)
    {
        var hash = Math.Abs(
            StringComparer.Ordinal.GetHashCode(marker));

        var month = hash % 12 + 1;

        return $"2027-{month:00}";
    }
}
