using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DashboardApi.Tests.Integration;

public sealed class EvmTests
{
    [Fact]
    public async Task GetProjectEvm_AuthorizedUser_ReturnsCalculatedEvmValues()
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
            "/dashboard/projects/PRJ-001/evm");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<List<EvmPerformanceDto>>();

        Assert.NotNull(result);

        var evm = Assert.Single(result);

        Assert.Equal("EVM-001", evm.EvmRecordId);
        Assert.Equal("PRJ-001", evm.ProjectId);
        Assert.Equal("2026-06", evm.Period);

        Assert.Equal(250000m, evm.Bac);
        Assert.Equal(125000m, evm.Pv);
        Assert.Equal(95000m, evm.Ev);
        Assert.Equal(110000m, evm.Ac);

        Assert.Equal(-30000m, evm.Sv);
        Assert.Equal(-15000m, evm.Cv);
        Assert.Equal(76m, evm.Spi);
        Assert.Equal(8636m, evm.Cpi);
        Assert.Equal(28947368m, evm.Eac);
        Assert.Equal(-3947368m, evm.Vac);
    }

        [Fact]
    public async Task GetProjectEvm_ProjectManagerWithoutAccess_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        /*
        * Bu kullanıcı yeni oluşturulduğu için herhangi bir projeye
        * proje yöneticisi veya proje kullanıcısı olarak atanmış değildir.
        */
        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Proje Yöneticisi");

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
            "/dashboard/projects/PRJ-002/evm");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            responseBody.TryGetProperty(
                "message",
                out var message));

        Assert.Equal(
            "Bu projenin finansal analiz verilerini görmeye yetkiniz yok!",
            message.GetString());
    }

    private static async Task<TestUserCredentials> CreateActiveUserAsync(
        TestWebApplicationFactory factory,
        string role)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<dashboardapi.Data.AppDbContext>();

        var builder = new TestDataBuilder(db);

        return await builder.CreateActiveUserAsync(role);
    }
}