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

public sealed class PortfolioTests
{
    [Fact]
    public async Task CreateCustomer_GeneratesStandardCustomerIdFormat()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var builder = new TestDataBuilder(db);

            credentials = await builder.CreateActiveUserAsync(role: "Sistem Yöneticisi");
        }

        var loginRequest = new LoginRequest(credentials.User.Email, credentials.PlainTextPassword);

        using var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        var request = new CreateCustomerRequest("Yeni Müşteri", "Kamu", "Aktif");

        using var response = await client.PostAsJsonAsync("/customers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(responseBody);
        Assert.True(responseBody.TryGetValue("customerId", out var customerIdValue));
        var customerId = customerIdValue?.ToString();

        Assert.NotNull(customerId);
        Assert.Matches("^CST-\\d{3}$", customerId);
    }

    [Fact]
    public async Task LegacyExecutive_CannotMutateCustomersOrPrograms()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "PRAGMA ignore_check_constraints = ON;");
            try
            {
                credentials = await new TestDataBuilder(db)
                    .CreateActiveUserAsync("Üst Yönetim");
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync(
                    "PRAGMA ignore_check_constraints = OFF;");
            }
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

        var customerRequest = new CreateCustomerRequest(
            "Yetkisiz Müşteri",
            "Özel",
            "Aktif");
        var programRequest = new CreateProgramRequest(
            "Yetkisiz Program",
            "Oluşturulmamalı",
            "Aktif");

        using var createCustomerResponse =
            await client.PostAsJsonAsync("/customers", customerRequest);
        using var updateCustomerResponse =
            await client.PatchAsJsonAsync(
                "/customers/CST-001",
                customerRequest);
        using var deleteCustomerResponse =
            await client.DeleteAsync("/customers/CST-001");
        using var createProgramResponse =
            await client.PostAsJsonAsync("/programs", programRequest);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            createCustomerResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            updateCustomerResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            deleteCustomerResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            createProgramResponse.StatusCode);
    }

    [Fact]
    public async Task GetPortfolio_WithSelectedProjectIds_ReturnsOnlySelectedProjects()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;
        int projectCountBeforeRequest;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var builder = new TestDataBuilder(db);

            credentials = await builder.CreateActiveUserAsync(
                role: "Sistem Yöneticisi");

            projectCountBeforeRequest =
                await db.Projects.CountAsync();

            Assert.True(
                projectCountBeforeRequest >= 3,
                "Test 33 için test veritabanında en az üç proje bulunmalıdır.");

            Assert.True(
                await db.Projects.AnyAsync(
                    project => project.ProjectId == "PRJ-001"));

            Assert.True(
                await db.Projects.AnyAsync(
                    project => project.ProjectId == "PRJ-002"));

            Assert.True(
                await db.Projects.AnyAsync(
                    project => project.ProjectId == "PRJ-003"));
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

        // Act
        using var response = await client.GetAsync(
            "/dashboard/portfolio" +
            "?projectIds=PRJ-001" +
            "&projectIds=PRJ-003");

        // Assert
        Assert.NotEqual(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        using var jsonDocument =
            JsonDocument.Parse(responseBody);

        Assert.Equal(
            JsonValueKind.Array,
            jsonDocument.RootElement.ValueKind);

        var returnedProjectIds = jsonDocument.RootElement
            .EnumerateArray()
            .Select(ReadProjectId)
            .ToList();

        var expectedProjectIds = new[]
        {
            "PRJ-001",
            "PRJ-003"
        };

        Assert.Equal(
            expectedProjectIds.Length,
            returnedProjectIds.Count);

        Assert.Contains(
            "PRJ-001",
            returnedProjectIds);

        Assert.Contains(
            "PRJ-003",
            returnedProjectIds);

        Assert.DoesNotContain(
            "PRJ-002",
            returnedProjectIds);

        Assert.Equal(
            expectedProjectIds.OrderBy(id => id),
            returnedProjectIds.OrderBy(id => id));

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var projectCountAfterRequest =
            await verificationDb.Projects.CountAsync();

        Assert.Equal(
            projectCountBeforeRequest,
            projectCountAfterRequest);
    }

    private static string ReadProjectId(
        JsonElement projectElement)
    {
        if (projectElement.TryGetProperty(
                "projectId",
                out var camelCaseProperty))
        {
            return camelCaseProperty.GetString()
                ?? throw new Xunit.Sdk.XunitException(
                    "Response içindeki projectId değeri null.");
        }

        if (projectElement.TryGetProperty(
                "ProjectId",
                out var pascalCaseProperty))
        {
            return pascalCaseProperty.GetString()
                ?? throw new Xunit.Sdk.XunitException(
                    "Response içindeki ProjectId değeri null.");
        }

        throw new Xunit.Sdk.XunitException(
            "Portföy response öğesinde projectId alanı bulunamadı.");
    }
}
