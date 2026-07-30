using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class AuthTests
{
    [Fact]
    public async Task Login_ActiveUserWithCorrectCredentials_ReturnsLoginResponse()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateUserAsync(
            factory,
            status: "Aktif");

        var request = new LoginRequest(
            credentials.User.Email,
            credentials.PlainTextPassword);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResponse);
        Assert.False(
            string.IsNullOrWhiteSpace(loginResponse.Token));

        Assert.Equal(
            credentials.User.UserId,
            loginResponse.UserId);

        Assert.Equal(
            credentials.User.FullName,
            loginResponse.FullName);

        Assert.Equal(
            credentials.User.UserRole,
            loginResponse.Role);
    }

    [Fact]
    public async Task Login_EmailWithDifferentCaseAndWhitespace_ReturnsLoginResponse()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        var credentials = await CreateUserAsync(
            factory,
            status: "Aktif");

        var request = new LoginRequest(
            $"  {credentials.User.Email.ToUpperInvariant()}  ",
            credentials.PlainTextPassword);

        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"email":null,"password":null}""")]
    [InlineData("""{"email":"","password":""}""")]
    [InlineData("null")]
    [InlineData("{")]
    public async Task Login_InvalidPayload_ReturnsBadRequest(
        string json)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(
            "/auth/login",
            content);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_ActiveUserWithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateUserAsync(
            factory,
            status: "Aktif");

        var request = new LoginRequest(
            credentials.User.Email,
            "WrongPassword123!");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            responseBody.TryGetProperty(
                "message",
                out var message));

        Assert.Equal(
            "E-posta veya şifre hatalı!",
            message.GetString());
    }

    [Fact]
    public async Task Login_PassiveUserWithCorrectCredentials_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateUserAsync(
            factory,
            status: "Pasif");

        var request = new LoginRequest(
            credentials.User.Email,
            credentials.PlainTextPassword);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/auth/login",
            request);

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
            "Kullanıcı hesabı aktif değil!",
            message.GetString());
    }

    [Fact]
    public async Task CreateUser_UpperManagementUser_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateUserAsync(
            factory,
            role: "Üst Yönetim İzleyicisi",
            status: "Aktif");

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

        var createUserRequest = new UserCreateDto(
            Email: $"blocked-{Guid.NewGuid():N}@example.invalid",
            FullName: "Yetkisiz Oluşturma Testi",
            Role: "Proje Yöneticisi",
            Password: $"NeverUsed-{Guid.NewGuid():N}-Aa1!");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/users",
            createUserRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private static async Task<TestUserCredentials> CreateUserAsync(
        TestWebApplicationFactory factory,
        string role = "Proje Yöneticisi",
        string status = "Aktif")
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var builder = new TestDataBuilder(db);

        return await builder.CreateUserAsync(
            role,
            status);
    }
}
