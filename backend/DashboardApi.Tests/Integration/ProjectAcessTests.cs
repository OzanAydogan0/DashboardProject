using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class ProjectAccessTests
{
    [Fact]
    public async Task GetDashboard_ProjectManagerAssignedToProject_ReturnsOnlyAuthorizedProject()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Proje Yöneticisi");

        await AssignUserToProjectAsync(
            factory,
            credentials.User.UserId,
            projectId: "PRJ-001");

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
            "/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var projects =
            await response.Content.ReadFromJsonAsync<List<DashboardSummaryDto>>();

        Assert.NotNull(projects);

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-001");

        Assert.DoesNotContain(
            projects,
            project => project.ProjectId == "PRJ-002");

        Assert.DoesNotContain(
            projects,
            project => project.ProjectId == "PRJ-003");
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

    // endpoint assignmentStatus == "Aktif" koşulunu kontrol etmiyor.
    [Fact]
    public async Task GetDashboard_ProjectManagerWithPassiveAssignment_DoesNotReturnProject()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Proje Yöneticisi");

        await AssignUserToProjectAsync(
            factory,
            credentials.User.UserId,
            projectId: "PRJ-001",
            assignmentStatus: "Pasif");

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
        using var response = await client.GetAsync("/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var projects =
            await response.Content.ReadFromJsonAsync<List<DashboardSummaryDto>>();

        Assert.NotNull(projects);

        Assert.DoesNotContain(
            projects,
            project => project.ProjectId == "PRJ-001");
    }
        [Fact]
    public async Task GetDashboard_SystemAdministrator_ReturnsAllActiveProjects()
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

        // Act
        using var response = await client.GetAsync(
            "/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var projects =
            await response.Content.ReadFromJsonAsync<List<DashboardSummaryDto>>();

        Assert.NotNull(projects);

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-001");

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-002");

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-003");
    }

    //database.sql dosyasındaki Üst Yönetim İzleyicisi rolü ile giriş yapıldığında dashboard endpointi tüm projeleri döndürür.
        [Fact]
    public async Task GetDashboard_UpperManagementViewer_ReturnsAllProjects()
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

        // Act
        using var response = await client.GetAsync(
            "/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var projects =
            await response.Content.ReadFromJsonAsync<List<DashboardSummaryDto>>();

        Assert.NotNull(projects);

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-001");

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-002");

        Assert.Contains(
            projects,
            project => project.ProjectId == "PRJ-003");
    }
        [Fact]
    public async Task GetDashboard_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        // Authorization header özellikle eklenmiyor.

        // Act
        using var response = await client.GetAsync(
            "/dashboard");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    private static async Task AssignUserToProjectAsync(
        TestWebApplicationFactory factory,
        string userId,
        string projectId,
        string assignmentStatus = "Aktif")
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        var assignment = new ProjectUser
        {
            ProjectUserId = $"TEST-PU-{Guid.NewGuid():N}",
            ProjectId = projectId,
            UserId = userId,

            // Seed SQL'deki sistem yöneticisi kullanıcı kullanılıyor.
            AssignedByUserId = "USR-ADMIN",

            AssignmentStatus = assignmentStatus,
            AssignedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.ProjectUsers.Add(assignment);
        await db.SaveChangesAsync();
    }
}