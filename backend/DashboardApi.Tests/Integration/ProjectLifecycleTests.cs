using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class ProjectLifecycleTests
{
    [Theory]
    [InlineData("Sistem Yöneticisi")]
    [InlineData("Proje Yöneticisi")]
    public async Task InactiveProject_AuthorizedManagerCanViewAndReactivate(
        string role)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            credentials = await new TestDataBuilder(db)
                .CreateActiveUserAsync(role);

            var project = await db.Projects.SingleAsync(
                candidate => candidate.ProjectId == "PRJ-001");
            project.IsActive = 0;
            if (role == "Proje Yöneticisi")
                project.ProjectManagerUserId = credentials.User.UserId;

            await db.SaveChangesAsync();
        }

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                credentials.User.Email,
                credentials.PlainTextPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        using var detailResponse = await client.GetAsync(
            "/projects/PRJ-001");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var detail =
            await detailResponse.Content.ReadFromJsonAsync<ProjectDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(0, detail.IsActive);

        using var updateResponse = await client.PatchAsJsonAsync(
            "/projects/PRJ-001",
            new { IsActive = 1 });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(
            1,
            await verificationDb.Projects
                .Where(project => project.ProjectId == "PRJ-001")
                .Select(project => project.IsActive)
                .SingleAsync());
    }
}
