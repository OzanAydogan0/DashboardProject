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

public sealed class RiskChildRelationshipTests
{
    [Fact]
    public async Task CreateMultipleIssuesAndActions_ForSameRisk_ListsRiskMetadataAndUsesSetNullOnDelete()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await AuthenticateAsSystemAdminAsync(
            factory,
            client);

        const string riskId = "RSK-001";
        const string projectId = "PRJ-001";

        string riskTitle;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            riskTitle = await db.Risks
                .Where(risk => risk.RiskId == riskId)
                .Select(risk => risk.RiskTitle)
                .SingleAsync();
        }

        var issueIds = new List<string>();
        var actionIds = new List<string>();

        for (var index = 1; index <= 2; index++)
        {
            using var issueResponse = await client.PostAsJsonAsync(
                "/issues",
                new CreateIssueRequest(
                    ProjectId: projectId,
                    IssueTitle: $"Riske bağlı test sorunu {index}",
                    IssuePriority: "Orta",
                    IssueOwnerUserId: credentials.User.UserId,
                    IssueDueDate: DateTime.UtcNow.Date.AddDays(30),
                    IssueStatus: index == 1 ? "Açık" : "Devam Ediyor",
                    IssueImpact: "Orta",
                    RootCause: "Risk gerçekleşti.",
                    RiskId: riskId));

            Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);
            issueIds.Add(await ReadCreatedIdAsync(issueResponse, "issueId"));

            using var actionResponse = await client.PostAsJsonAsync(
                "/actions",
                new CreateActionRequest(
                    ProjectId: projectId,
                    ActionDescription: $"Riske bağlı test aksiyonu {index}",
                    SourceType: "Risk",
                    SourceReference: riskId,
                    ActionOwnerUserId: credentials.User.UserId,
                    ActionDueDate: DateTime.UtcNow.Date.AddDays(30),
                    ActionStatus: "Açık",
                    ActionProgress: 0m,
                    ActionPriority: "Orta",
                    RiskId: riskId));

            Assert.Equal(HttpStatusCode.Created, actionResponse.StatusCode);
            actionIds.Add(await ReadCreatedIdAsync(actionResponse, "actionId"));
        }

        using var issueListResponse = await client.GetAsync(
            $"/projects/{projectId}/issues");

        Assert.Equal(HttpStatusCode.OK, issueListResponse.StatusCode);

        var issues = await issueListResponse.Content
            .ReadFromJsonAsync<List<IssueDto>>();

        Assert.NotNull(issues);

        var createdIssues = issues
            .Where(issue => issueIds.Contains(issue.IssueId))
            .ToList();

        Assert.Equal(2, createdIssues.Count);
        Assert.All(createdIssues, issue =>
        {
            Assert.Equal(riskId, issue.RiskId);
            Assert.Equal(riskTitle, issue.RiskTitle);
        });

        using var projectActionListResponse = await client.GetAsync(
            $"/projects/{projectId}/actions");

        Assert.Equal(HttpStatusCode.OK, projectActionListResponse.StatusCode);

        var projectActions = await projectActionListResponse.Content
            .ReadFromJsonAsync<List<ActionDto>>();

        Assert.NotNull(projectActions);

        var createdProjectActions = projectActions
            .Where(action => actionIds.Contains(action.ActionId))
            .ToList();

        Assert.Equal(2, createdProjectActions.Count);
        Assert.All(createdProjectActions, action =>
        {
            Assert.Equal(riskId, action.RiskId);
            Assert.Equal(riskTitle, action.RiskTitle);
        });

        using var actionListResponse = await client.GetAsync("/actions");

        Assert.Equal(HttpStatusCode.OK, actionListResponse.StatusCode);

        var actions = await actionListResponse.Content
            .ReadFromJsonAsync<List<ActionDto>>();

        Assert.NotNull(actions);

        var createdActions = actions
            .Where(action => actionIds.Contains(action.ActionId))
            .ToList();

        Assert.Equal(2, createdActions.Count);
        Assert.All(createdActions, action =>
        {
            Assert.Equal(riskId, action.RiskId);
            Assert.Equal(riskTitle, action.RiskTitle);
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var risk = await db.Risks
                .Include(item => item.Issues)
                .Include(item => item.Actions)
                .SingleAsync(item => item.RiskId == riskId);

            Assert.Equal(
                2,
                risk.Issues.Count(issue => issueIds.Contains(issue.IssueId)));

            Assert.Equal(
                2,
                risk.Actions.Count(action => actionIds.Contains(action.ActionId)));

            await db.Risks
                .Where(item => item.RiskId == riskId)
                .ExecuteDeleteAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var issueRiskIds = await db.Issues
                .Where(issue => issueIds.Contains(issue.IssueId))
                .Select(issue => issue.RiskId)
                .ToListAsync();

            var actionRiskIds = await db.Actions
                .Where(action => actionIds.Contains(action.ActionId))
                .Select(action => action.RiskId)
                .ToListAsync();

            Assert.Equal(2, issueRiskIds.Count);
            Assert.All(issueRiskIds, Assert.Null);

            Assert.Equal(2, actionRiskIds.Count);
            Assert.All(actionRiskIds, Assert.Null);
        }
    }

    [Theory]
    [InlineData("issues", "RSK-NOT-FOUND")]
    [InlineData("actions", "RSK-NOT-FOUND")]
    [InlineData("issues", "RSK-002")]
    [InlineData("actions", "RSK-002")]
    public async Task CreateChild_WithMissingOrDifferentProjectRisk_ReturnsBadRequest(
        string endpointType,
        string riskId)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await AuthenticateAsSystemAdminAsync(
            factory,
            client);

        var marker = $"risk-validation-{endpointType}-{riskId}-{Guid.NewGuid():N}";

        object request = endpointType switch
        {
            "issues" => new CreateIssueRequest(
                ProjectId: "PRJ-001",
                IssueTitle: marker,
                IssuePriority: "Orta",
                IssueOwnerUserId: credentials.User.UserId,
                IssueDueDate: DateTime.UtcNow.Date.AddDays(30),
                IssueStatus: "Açık",
                IssueImpact: "Orta",
                RootCause: "Risk doğrulama testi.",
                RiskId: riskId),

            "actions" => new CreateActionRequest(
                ProjectId: "PRJ-001",
                ActionDescription: marker,
                SourceType: "Risk",
                SourceReference: riskId,
                ActionOwnerUserId: credentials.User.UserId,
                ActionDueDate: DateTime.UtcNow.Date.AddDays(30),
                ActionStatus: "Açık",
                ActionProgress: 0m,
                ActionPriority: "Orta",
                RiskId: riskId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(endpointType),
                endpointType,
                "Desteklenmeyen endpoint türü.")
        };

        using var response = await client.PostAsJsonAsync(
            $"/{endpointType}",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var recordWasCreated = endpointType switch
        {
            "issues" => await db.Issues.AnyAsync(
                issue => issue.IssueTitle == marker),
            "actions" => await db.Actions.AnyAsync(
                action => action.ActionDescription == marker),
            _ => true
        };

        Assert.False(recordWasCreated);
    }

    [Fact]
    public async Task CreateLinkedIssue_WithUnsupportedStatus_ReturnsBadRequest()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await AuthenticateAsSystemAdminAsync(
            factory,
            client);

        var marker = $"invalid-issue-status-{Guid.NewGuid():N}";

        using var response = await client.PostAsJsonAsync(
            "/issues",
            new CreateIssueRequest(
                ProjectId: "PRJ-001",
                IssueTitle: marker,
                IssuePriority: "Orta",
                IssueOwnerUserId: credentials.User.UserId,
                IssueDueDate: DateTime.UtcNow.Date.AddDays(30),
                IssueStatus: "İzleniyor",
                IssueImpact: "Orta",
                RootCause: "Geçersiz durum doğrulama testi.",
                RiskId: "RSK-001"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Issues.AnyAsync(
            issue => issue.IssueTitle == marker));
    }

    private static async Task<TestUserCredentials> AuthenticateAsSystemAdminAsync(
        TestWebApplicationFactory factory,
        HttpClient client)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var builder = new TestDataBuilder(db);
        var credentials = await builder.CreateActiveUserAsync(
            role: "Sistem Yöneticisi");

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                credentials.User.Email,
                credentials.PlainTextPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.Token);

        return credentials;
    }

    private static async Task<string> ReadCreatedIdAsync(
        HttpResponseMessage response,
        string propertyName)
    {
        var responseBody = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.True(responseBody.TryGetProperty(propertyName, out var idProperty));

        var id = idProperty.GetString();
        Assert.False(string.IsNullOrWhiteSpace(id));

        return id;
    }
}
