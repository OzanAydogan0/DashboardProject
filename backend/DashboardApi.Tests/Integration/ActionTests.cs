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

public sealed class ActionTests
{
    [Fact]
    public async Task GetActions_IncludesProjectNameInResponse()
    {
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

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        var createRequest = new CreateActionRequest(
            ProjectId: "PRJ-003",
            ActionDescription: "Proje adı test aksiyonu",
            SourceType: "Diğer",
            SourceReference: null,
            ActionOwnerUserId: credentials.User.UserId,
            ActionDueDate: DateTime.UtcNow.Date.AddDays(1),
            ActionStatus: "Devam Ediyor",
            ActionProgress: 25m,
            ActionPriority: "Orta");

        using var createResponse = await client.PostAsJsonAsync(
            "/actions",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResponseBody =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createResponseBody.TryGetProperty("actionId", out var actionIdProperty));
        var actionId = actionIdProperty.GetString();
        Assert.False(string.IsNullOrWhiteSpace(actionId));

        using var listResponse = await client.GetAsync("/actions");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var actions = await listResponse.Content.ReadFromJsonAsync<List<ActionDto>>();

        Assert.NotNull(actions);
        Assert.NotEmpty(actions);

        var createdAction = Assert.Single(actions.Where(a => a.ActionId == actionId));
        Assert.Equal("PRJ-003", createdAction.ProjectId);
        Assert.False(string.IsNullOrWhiteSpace(createdAction.ProjectName));
    }

    [Fact]
    public async Task UpdateAction_ProgressReachesOneHundred_SetsCompletedDateAutomatically()
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

        /*
         * Bitiş tarihi güncelleme anında atanacağı için son tarih
         * geçmişte seçiliyor. Böylece completed_date constraint'i sağlanır.
         */
        var createRequest = new CreateActionRequest(
            ProjectId: "PRJ-003",
            ActionDescription: "Yüzde yüz ilerleme otomatik tamamlanma testi",
            SourceType: "Diğer",
            SourceReference: null,
            ActionOwnerUserId: credentials.User.UserId,
            ActionDueDate: DateTime.UtcNow.Date.AddDays(-1),
            ActionStatus: "Devam Ediyor",
            ActionProgress: 40m,
            ActionPriority: "Orta");

        using var createResponse = await client.PostAsJsonAsync(
            "/actions",
            createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createResponseBody =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            createResponseBody.TryGetProperty(
                "actionId",
                out var actionIdProperty));

        var actionId = actionIdProperty.GetString();

        Assert.False(string.IsNullOrWhiteSpace(actionId));

        var updateRequest = new UpdateActionRequest(
            ActionDescription: null,
            SourceType: null,
            SourceReference: null,
            ActionOwnerUserId: null,
            ActionDueDate: null,
            ActionStatus: null,
            ActionProgress: 100m,
            ActionPriority: null);

        var updateStartedAt = DateTime.UtcNow;

        // Act
        using var updateResponse = await client.PatchAsJsonAsync(
            $"/actions/{actionId}",
            updateRequest);

        var updateFinishedAt = DateTime.UtcNow;

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var updatedAction = await db.Actions
            .AsNoTracking()
            .SingleAsync(action =>
                action.ActionId == actionId);

        Assert.Equal(100m, updatedAction.ActionProgress);
        Assert.NotNull(updatedAction.CompletedDate);

        Assert.InRange(
            updatedAction.CompletedDate.Value,
            updateStartedAt.AddSeconds(-1),
            updateFinishedAt.AddSeconds(1));

        Assert.Equal(
            credentials.User.UserId,
            updatedAction.UpdatedByUserId);
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