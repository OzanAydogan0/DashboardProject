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

    [Fact]
    public async Task CreateEvmRecord_ValidValues_CalculatesEacAndVacCorrectly()
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
        * CPI = EV / AC = 400 / 500 = 0,80
        * EAC = BAC / CPI = 1000 / 0,80 = 1250
        * VAC = BAC - EAC = 1000 - 1250 = -250
        */
        var request = new CreateEvmRecordRequest(
            ProjectId: "PRJ-003",
            Period: "2026-11",
            Bac: 1000m,
            Pv: 500m,
            Ev: 400m,
            Ac: 500m);

        // Act
        using var createResponse = await client.PostAsJsonAsync(
            "/evm-records",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        using var getResponse = await client.GetAsync(
            "/projects/PRJ-003/evm-records");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var records =
            await getResponse.Content.ReadFromJsonAsync<List<EvmRecordDto>>();

        Assert.NotNull(records);

        var createdRecord = Assert.Single(
            records,
            record => record.Period == "2026-11");

        Assert.Equal(0.80m, createdRecord.Cpi);
        Assert.Equal(1250m, createdRecord.Eac);
        Assert.Equal(-250m, createdRecord.Vac);
    }

        [Fact]
    public async Task CreateEvmRecord_ZeroPvOrAc_HandlesUndefinedMetricsCorrectly()
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

        var zeroPvRequest = new CreateEvmRecordRequest(
            ProjectId: "PRJ-003",
            Period: "2026-10",
            Bac: 1000m,
            Pv: 0m,
            Ev: 400m,
            Ac: 500m);

        var zeroAcRequest = new CreateEvmRecordRequest(
            ProjectId: "PRJ-003",
            Period: "2026-12",
            Bac: 1000m,
            Pv: 500m,
            Ev: 400m,
            Ac: 0m);

        // Act
        using var zeroPvResponse = await client.PostAsJsonAsync(
            "/evm-records",
            zeroPvRequest);

        using var zeroAcResponse = await client.PostAsJsonAsync(
            "/evm-records",
            zeroAcRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            zeroPvResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            zeroAcResponse.StatusCode);

        using var getResponse = await client.GetAsync(
            "/projects/PRJ-003/evm-records");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var records =
            await getResponse.Content.ReadFromJsonAsync<List<EvmRecordDto>>();

        Assert.NotNull(records);

        var zeroPvRecord = Assert.Single(
            records,
            record => record.Period == "2026-10");

        var zeroAcRecord = Assert.Single(
            records,
            record => record.Period == "2026-12");

        // PV = 0: SPI tanımsızdır.
        Assert.Null(zeroPvRecord.Spi);

        // AC sıfır olmadığı için diğer maliyet metrikleri hesaplanabilir.
        Assert.Equal(0.80m, zeroPvRecord.Cpi);
        Assert.Equal(1250m, zeroPvRecord.Eac);
        Assert.Equal(-250m, zeroPvRecord.Vac);

        // AC = 0: CPI tanımsızdır.
        Assert.Equal(0.80m, zeroAcRecord.Spi);
        Assert.Null(zeroAcRecord.Cpi);

        // CPI hesaplanamadığı için EAC ve VAC da hesaplanamaz.
        Assert.Null(zeroAcRecord.Eac);
        Assert.Null(zeroAcRecord.Vac);
    }

    [Fact]
    public async Task UpdateEvmRecord_ValidValues_UpdatesMetricsAndPersistsChanges()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(factory, role: "Sistem Yöneticisi");
        var loginRequest = new LoginRequest(credentials.User.Email, credentials.PlainTextPassword);

        using var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        var createRequest = new CreateEvmRecordRequest(
            ProjectId: "PRJ-003",
            Period: "2026-09",
            Bac: 1000m,
            Pv: 400m,
            Ev: 300m,
            Ac: 500m);

        using var createResponse = await client.PostAsJsonAsync("/evm-records", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var evmRecordId = createdPayload.GetProperty("evmRecordId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(evmRecordId));

        var updateRequest = new UpdateEvmRecordRequest(
            ProjectId: "PRJ-003",
            Period: "2026-09",
            Bac: 2000m,
            Pv: 800m,
            Ev: 700m,
            Ac: 600m);

        using var updateResponse = await client.PutAsJsonAsync($"/evm-records/{evmRecordId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var getResponse = await client.GetAsync("/projects/PRJ-003/evm-records");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var records = await getResponse.Content.ReadFromJsonAsync<List<EvmRecordDto>>();
        Assert.NotNull(records);

        var updatedRecord = Assert.Single(records!, record => record.EvmRecordId == evmRecordId);
        Assert.Equal(2000m, updatedRecord.Bac);
        Assert.Equal(1.17m, updatedRecord.Cpi);
        Assert.Equal(1709.40m, updatedRecord.Eac);
        Assert.Equal(290.60m, updatedRecord.Vac);
    }

    [Fact]
    public async Task DeleteEvmRecord_ValidValues_RemovesRecord()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(factory, role: "Sistem Yöneticisi");
        var loginRequest = new LoginRequest(credentials.User.Email, credentials.PlainTextPassword);

        using var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        var createRequest = new CreateEvmRecordRequest(
            ProjectId: "PRJ-003",
            Period: "2026-08",
            Bac: 500m,
            Pv: 250m,
            Ev: 200m,
            Ac: 300m);

        using var createResponse = await client.PostAsJsonAsync("/evm-records", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var evmRecordId = createdPayload.GetProperty("evmRecordId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(evmRecordId));

        using var deleteResponse = await client.DeleteAsync($"/evm-records/{evmRecordId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var getResponse = await client.GetAsync("/projects/PRJ-003/evm-records");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var records = await getResponse.Content.ReadFromJsonAsync<List<EvmRecordDto>>();
        Assert.NotNull(records);
        Assert.DoesNotContain(records!, record => record.EvmRecordId == evmRecordId);
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
