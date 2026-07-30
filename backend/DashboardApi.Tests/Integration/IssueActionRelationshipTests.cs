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

public sealed class IssueActionRelationshipTests
{
    [Fact]
    public async Task CreateMultipleActions_ForSameIssue_ListsIssueMetadataAndUsesSetNullOnDelete()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await AuthenticateAsSystemAdminAsync(
            factory,
            client);

        const string issueId = "ISS-001";
        const string projectId = "PRJ-001";

        string issueTitle;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            issueTitle = await db.Issues
                .Where(issue => issue.IssueId == issueId)
                .Select(issue => issue.IssueTitle)
                .SingleAsync();
        }

        var actionIds = new List<string>();

        for (var index = 1; index <= 2; index++)
        {
            using var createResponse = await client.PostAsJsonAsync(
                "/actions",
                new CreateActionRequest(
                    ProjectId: projectId,
                    ActionDescription: $"Soruna bağlı test aksiyonu {index}",
                    SourceType: "Diğer",
                    SourceReference: "istemci-değeri-yok-sayılmalı",
                    ActionOwnerUserId: credentials.User.UserId,
                    ActionDueDate: DateTime.UtcNow.Date.AddDays(30),
                    ActionStatus: "Açık",
                    ActionProgress: 0m,
                    ActionPriority: "Orta",
                    RiskId: null,
                    IssueId: issueId));

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            actionIds.Add(await ReadCreatedIdAsync(createResponse, "actionId"));
        }

        using var projectListResponse = await client.GetAsync(
            $"/projects/{projectId}/actions");

        Assert.Equal(HttpStatusCode.OK, projectListResponse.StatusCode);

        var projectActions = await projectListResponse.Content
            .ReadFromJsonAsync<List<ActionDto>>();

        Assert.NotNull(projectActions);

        var createdProjectActions = projectActions
            .Where(action => actionIds.Contains(action.ActionId))
            .ToList();

        Assert.Equal(2, createdProjectActions.Count);
        Assert.All(createdProjectActions, action =>
        {
            Assert.Equal(issueId, action.IssueId);
            Assert.Equal(issueTitle, action.IssueTitle);
            Assert.Null(action.RiskId);
            Assert.Equal("Sorun", action.SourceType);
            Assert.Equal(issueId, action.SourceReference);
        });

        using var globalListResponse = await client.GetAsync("/actions");

        Assert.Equal(HttpStatusCode.OK, globalListResponse.StatusCode);

        var globalActions = await globalListResponse.Content
            .ReadFromJsonAsync<List<ActionDto>>();

        Assert.NotNull(globalActions);

        var createdGlobalActions = globalActions
            .Where(action => actionIds.Contains(action.ActionId))
            .ToList();

        Assert.Equal(2, createdGlobalActions.Count);
        Assert.All(createdGlobalActions, action =>
        {
            Assert.Equal(issueId, action.IssueId);
            Assert.Equal(issueTitle, action.IssueTitle);
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var issue = await db.Issues
                .Include(item => item.Actions)
                .SingleAsync(item => item.IssueId == issueId);

            Assert.Equal(
                2,
                issue.Actions.Count(action => actionIds.Contains(action.ActionId)));

            await db.Issues
                .Where(item => item.IssueId == issueId)
                .ExecuteDeleteAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var survivingActions = await db.Actions
                .Where(action => actionIds.Contains(action.ActionId))
                .ToListAsync();

            Assert.Equal(2, survivingActions.Count);
            Assert.All(survivingActions, action => Assert.Null(action.IssueId));
        }
    }

    [Theory]
    [InlineData("ISS-NOT-FOUND", null)]
    [InlineData("ISS-002", null)]
    [InlineData("ISS-001", "RSK-001")]
    public async Task CreateAction_WithInvalidOrConflictingIssueLink_ReturnsBadRequest(
        string issueId,
        string? riskId)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await AuthenticateAsSystemAdminAsync(
            factory,
            client);

        var marker = $"issue-action-validation-{Guid.NewGuid():N}";

        using var response = await client.PostAsJsonAsync(
            "/actions",
            new CreateActionRequest(
                ProjectId: "PRJ-001",
                ActionDescription: marker,
                SourceType: "Diğer",
                SourceReference: null,
                ActionOwnerUserId: credentials.User.UserId,
                ActionDueDate: DateTime.UtcNow.Date.AddDays(30),
                ActionStatus: "Açık",
                ActionProgress: 0m,
                ActionPriority: "Orta",
                RiskId: riskId,
                IssueId: issueId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Actions.AnyAsync(
            action => action.ActionDescription == marker));
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
