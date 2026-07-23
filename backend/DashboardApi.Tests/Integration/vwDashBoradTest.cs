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
using ActionModel = dashboardapi.Models.Action;

namespace DashboardApi.Tests.Integration;

public sealed class DashboardTests
{
    [Fact]
    public async Task GetDashboard_WithOpenAndClosedRecords_ReturnsCorrectOpenCounts()
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
                role: "Sistem Yöneticisi");

            const string projectId = "PRJ-003";
            var userId = credentials.User.UserId;
            var now = DateTime.UtcNow;

            /*
             * PRJ-003 için kontrollü ve tekrarlanabilir bir senaryo
             * hazırlamak amacıyla mevcut bağlı kayıtlar temizleniyor.
             */
            await db.Risks
                .Where(x => x.ProjectId == projectId)
                .ExecuteDeleteAsync();

            await db.Issues
                .Where(x => x.ProjectId == projectId)
                .ExecuteDeleteAsync();

            await db.Actions
                .Where(x => x.ProjectId == projectId)
                .ExecuteDeleteAsync();

            await db.Milestones
                .Where(x => x.ProjectId == projectId)
                .ExecuteDeleteAsync();

            // 2 açık risk, 1 kapalı risk
            db.Risks.AddRange(
                CreateRisk(
                    "TEST-RSK-OPEN-1",
                    projectId,
                    userId,
                    "Açık",
                    now),
                CreateRisk(
                    "TEST-RSK-OPEN-2",
                    projectId,
                    userId,
                    "İzleniyor",
                    now),
                CreateRisk(
                    "TEST-RSK-CLOSED",
                    projectId,
                    userId,
                    "Kapalı",
                    now,
                    closedDate: now));

            // 1 açık sorun, 1 kapalı sorun
            db.Issues.AddRange(
                CreateIssue(
                    "TEST-ISS-OPEN",
                    projectId,
                    userId,
                    "Devam Ediyor",
                    now),
                CreateIssue(
                    "TEST-ISS-CLOSED",
                    projectId,
                    userId,
                    "Kapalı",
                    now,
                    closedDate: now));

            // 3 açık aksiyon, 1 tamamlanmış aksiyon
            db.Actions.AddRange(
                CreateAction(
                    "TEST-ACT-OPEN-1",
                    projectId,
                    userId,
                    "Açık",
                    0,
                    now),
                CreateAction(
                    "TEST-ACT-OPEN-2",
                    projectId,
                    userId,
                    "Devam Ediyor",
                    40,
                    now),
                CreateAction(
                    "TEST-ACT-OPEN-3",
                    projectId,
                    userId,
                    "Açık",
                    20,
                    now),
                CreateAction(
                    "TEST-ACT-COMPLETED",
                    projectId,
                    userId,
                    "Tamamlandı",
                    100,
                    now,
                    completedDate: now));

            // 2 açık kilometre taşı, 1 tamamlanmış kilometre taşı
            db.Milestones.AddRange(
                CreateMilestone(
                    "TEST-MS-OPEN-1",
                    projectId,
                    userId,
                    "Planlandı",
                    now),
                CreateMilestone(
                    "TEST-MS-OPEN-2",
                    projectId,
                    userId,
                    "Devam Ediyor",
                    now),
                CreateMilestone(
                    "TEST-MS-COMPLETED",
                    projectId,
                    userId,
                    "Tamamlandı",
                    now,
                    actualDate: now));

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
        using var response = await client.GetAsync("/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var dashboard =
            await response.Content.ReadFromJsonAsync<List<DashboardSummaryDto>>();

        Assert.NotNull(dashboard);

        var project = Assert.Single(
            dashboard,
            x => x.ProjectId == "PRJ-003");

        Assert.Equal(2, project.OpenRiskCount);
        Assert.Equal(1, project.OpenIssueCount);
        Assert.Equal(3, project.OpenActionCount);
        Assert.Equal(2, project.OpenMilestoneCount);
    }

    private static Risk CreateRisk(
        string riskId,
        string projectId,
        string userId,
        string status,
        DateTime now,
        DateTime? closedDate = null)
    {
        return new Risk
        {
            RiskId = riskId,
            ProjectId = projectId,
            RiskTitle = $"Dashboard test riski {riskId}",
            RiskCategory = "Teknik",
            RiskProbability = 2,
            RiskImpact = 3,
            RiskScore = 0,
            RiskOwnerUserId = userId,
            RiskMitigation = "Test azaltma planı",
            RiskDueDate = now.AddDays(30),
            RiskStatus = status,
            OpenedDate = now.Date,
            ClosedDate = closedDate,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Issue CreateIssue(
        string issueId,
        string projectId,
        string userId,
        string status,
        DateTime now,
        DateTime? closedDate = null)
    {
        return new Issue
        {
            IssueId = issueId,
            ProjectId = projectId,
            IssueTitle = $"Dashboard test sorunu {issueId}",
            IssuePriority = "Orta",
            IssueOwnerUserId = userId,
            IssueDueDate = now.AddDays(20),
            IssueStatus = status,
            IssueImpact = "Orta",
            RootCause = "Test kök nedeni",
            IssueResolution = closedDate is null
                ? null
                : "Test çözümü",
            OpenedDate = now.Date,
            ClosedDate = closedDate,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static ActionModel CreateAction(
        string actionId,
        string projectId,
        string userId,
        string status,
        decimal progress,
        DateTime now,
        DateTime? completedDate = null)
    {
        return new ActionModel
        {
            ActionId = actionId,
            ProjectId = projectId,
            ActionDescription = $"Dashboard test aksiyonu {actionId}",
            SourceType = "Diğer",
            SourceReference = null,
            ActionOwnerUserId = userId,
            ActionDueDate = now.AddDays(15),
            ActionStatus = status,
            ActionProgress = progress,
            ActionPriority = "Orta",
            CompletedDate = completedDate,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Milestone CreateMilestone(
        string milestoneId,
        string projectId,
        string userId,
        string status,
        DateTime now,
        DateTime? actualDate = null)
    {
        return new Milestone
        {
            MilestoneId = milestoneId,
            ProjectId = projectId,
            MilestoneName = $"Dashboard test kilometre taşı {milestoneId}",
            PlannedDate = now.AddDays(10),
            ForecastDate = now.AddDays(15),
            ActualDate = actualDate,
            MilestoneStatus = status,
            Critical = 1,
            MilestoneOwnerUserId = userId,
            AcceptanceCriteria = "Dashboard sayım testinin tamamlanması",
            MilestoneDescription = "Otomatik test kaydı",
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}