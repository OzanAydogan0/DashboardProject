using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Models;

namespace DashboardApi.Tests.Integration;

public sealed class PirReportTests
{
    //test verisi yok 
    [Fact]
    public async Task GetProjectPirs_ExistingProject_ReturnsProjectPirReports()
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
            "/projects/PRJ-001/pirs");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var reports =
            await response.Content.ReadFromJsonAsync<List<PirReportDto>>();

        Assert.NotNull(reports);

        var report = Assert.Single(reports);

        Assert.Equal("PIR-01", report.PirReportId);
        Assert.Equal("PRJ-001", report.ProjectId);
        Assert.Equal("PRJ-001", report.ProjectCode);
        Assert.Equal("2026-06", report.Period);

        Assert.Equal(
            "Kritik",
            report.ManualHealth);

        Assert.Equal(
            "Yayımlandı",
            report.ReportStatus);

        Assert.False(
            string.IsNullOrWhiteSpace(report.ExecutiveSummary));

        Assert.False(
            string.IsNullOrWhiteSpace(report.CompletedWork));

        Assert.NotNull(report.PublishedAt);
    }
    [Fact]
    public async Task CreatePirReport_ValidDraftRequest_CreatesPirReport()
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

        const string period = "2026-08";
        const string executiveSummary =
            "Ağustos dönemi entegrasyon çalışmaları devam etmektedir.";

        var request = new CreatePirReportRequest(
            ProjectId: "PRJ-001",
            Period: period,
            ReportDate: new DateTime(2026, 8, 31),
            ExecutiveSummary: executiveSummary,
            CompletedWork: "Test ve entegrasyon hazırlıkları tamamlandı.",
            Delays: null,
            NextPeriodPlan: "Kabul testleri gerçekleştirilecek.",
            ManagementExpectations: "Test ortamı desteği beklenmektedir.",
            ManualHealth: "Orta",
            ReportStatus: "Taslak");

        // Act
        using var response = await client.PostAsJsonAsync(
            "/pirs",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            responseBody.TryGetProperty(
                "pirId",
                out var pirIdProperty));

        var pirId = pirIdProperty.GetString();

        Assert.False(string.IsNullOrWhiteSpace(pirId));

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var createdReport = await db.PirReports
            .AsNoTracking()
            .SingleOrDefaultAsync(report =>
                report.PirReportId == pirId);

        Assert.NotNull(createdReport);

        Assert.Equal(
            "PRJ-001",
            createdReport.ProjectId);

        Assert.Equal(
            period,
            createdReport.Period);

        Assert.Equal(
            executiveSummary,
            createdReport.ExecutiveSummary);

        Assert.Equal(
            "Taslak",
            createdReport.ReportStatus);

        Assert.Equal(
            "Sarı",
            createdReport.ManualHealth);

        Assert.Null(createdReport.PublishedAt);
        Assert.Null(createdReport.PublishedByUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdReport.CreatedByUserId);

        Assert.Equal(
            credentials.User.UserId,
            createdReport.UpdatedByUserId);
    }

    [Fact]
    public async Task UpdatePirReport_WithWritePermission_UpdatesExistingReport()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(factory, role: "Proje Yöneticisi");

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(credentials.User.Email, credentials.PlainTextPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        var updatePayload = new
        {
            period = "2026-09",
            executiveSummary = "Dönem özeti güncellendi.",
            reportStatus = "Taslak",
            completedWork = "Test adımları tamamlandı.",
            nextPeriodPlan = "Sonraki dönem test planı hazırlanacak."
        };

        using var response = await client.PatchAsJsonAsync("/pirs/PIR-01", updatePayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedReport = await db.PirReports.SingleAsync(report => report.PirReportId == "PIR-01");
        Assert.Equal("2026-09", updatedReport.Period);
        Assert.Equal("Dönem özeti güncellendi.", updatedReport.ExecutiveSummary);
        Assert.Equal("Taslak", updatedReport.ReportStatus);
    }

    [Fact]
    public async Task DeletePirReport_WithWritePermission_DeletesExistingReport()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(factory, role: "Proje Yöneticisi");

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(credentials.User.Email, credentials.PlainTextPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        using var response = await client.DeleteAsync("/pirs/PIR-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var deletedReport = await db.PirReports.SingleOrDefaultAsync(report => report.PirReportId == "PIR-01");
        Assert.Null(deletedReport);
    }

    /*GET projects/{id}/pirs endpoint’i korumasız kalmış.*/
        [Fact]
    public async Task GetProjectPirs_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        // Authorization header özellikle eklenmiyor.

        // Act
        using var response = await client.GetAsync(
            "/projects/PRJ-001/pirs");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

        [Fact]
    public async Task GetPirReports_ForAnotherManagersProject_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials pm1Credentials;
        TestUserCredentials pm2Credentials;
        string pm2ProjectId;
        string protectedSummary;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var builder = new TestDataBuilder(db);

            pm1Credentials = await builder.CreateActiveUserAsync(
                role: "Proje Yöneticisi");

            pm2Credentials = await builder.CreateActiveUserAsync(
                role: "Proje Yöneticisi");

            const string pm1ProjectId = "PRJ-001";
            pm2ProjectId = "PRJ-002";

            var pm1Project = await db.Projects.SingleAsync(
                project => project.ProjectId == pm1ProjectId);

            var pm2Project = await db.Projects.SingleAsync(
                project => project.ProjectId == pm2ProjectId);

            pm1Project.ProjectManagerUserId =
                pm1Credentials.User.UserId;

            pm2Project.ProjectManagerUserId =
                pm2Credentials.User.UserId;

            protectedSummary =
                $"PM2 özel PİR içeriği {Guid.NewGuid():N}";

            var now = DateTime.UtcNow;

            db.PirReports.Add(new PirReport
            {
                PirReportId =
                    $"TEST-PIR-{Guid.NewGuid():N}"[..17].ToUpperInvariant(),

                ProjectId = pm2ProjectId,
                Period = "2027-03",
                ReportDate = new DateTime(2027, 3, 31),

                ExecutiveSummary = protectedSummary,
                CompletedWork = "PM2 projesine ait özel çalışma bilgisi.",
                Delays = null,
                NextPeriodPlan = "PM2 projesinin sonraki dönem planı.",
                ManagementExpectations = null,

                ManualHealth = "Sarı",
                ReportStatus = "Yayımlandı",

                PublishedByUserId = pm2Credentials.User.UserId,
                PublishedAt = now,

                CreatedByUserId = pm2Credentials.User.UserId,
                UpdatedByUserId = pm2Credentials.User.UserId,
                CreatedAt = now,
                UpdatedAt = now
            });

            await db.SaveChangesAsync();
        }

        var loginRequest = new LoginRequest(
            pm1Credentials.User.Email,
            pm1Credentials.PlainTextPassword);

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
            $"/projects/{pm2ProjectId}/pirs");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            protectedSummary,
            responseBody,
            StringComparison.Ordinal);

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var protectedReportStillExists =
            await verificationDb.PirReports.AnyAsync(
                report =>
                    report.ProjectId == pm2ProjectId &&
                    report.ExecutiveSummary == protectedSummary);

        // Reddedilen GET isteği veritabanındaki raporu değiştirmemeli.
        Assert.True(protectedReportStillExists);
    }

    /*
        * AC-05 ve API yönergesi mevcut PİR kaydının güncellenip
        * yayımlanabilmesi için PATCH /reports/{id} bekliyor.
        *
        * Backend'de henüz UpdatePirReportRequest DTO'su olmadığı için
        * payload JsonContent ile sözleşme adayı olarak gönderiliyor.
        */
        [Fact]
    public async Task PirReport_CreateUpdateAndPublish_CompletesFullWorkflow()
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

        const string period = "2027-01";

        var createRequest = new CreatePirReportRequest(
            ProjectId: "PRJ-003",
            Period: period,
            ReportDate: new DateTime(2027, 1, 31),
            ExecutiveSummary: "İlk taslak yönetici özeti.",
            CompletedWork: "İlk taslak tamamlanan işler.",
            Delays: null,
            NextPeriodPlan: "İlk taslak dönem planı.",
            ManagementExpectations: null,
            ManualHealth: "Sarı",
            ReportStatus: "Taslak");

        using var createResponse = await client.PostAsJsonAsync(
            "/pirs",
            createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createBody =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(
            createBody.TryGetProperty(
                "pirId",
                out var pirIdProperty));

        var pirId = pirIdProperty.GetString();

        Assert.False(string.IsNullOrWhiteSpace(pirId));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var draft = await db.PirReports
                .AsNoTracking()
                .SingleAsync(report =>
                    report.PirReportId == pirId);

            Assert.Equal("Taslak", draft.ReportStatus);
            Assert.Null(draft.PublishedAt);
            Assert.Null(draft.PublishedByUserId);
        }
        var updateAndPublishRequest = new
        {
            executiveSummary = "Güncellenmiş ve yayımlanmış yönetici özeti.",
            completedWork = "Güncellenmiş tamamlanan işler.",
            delays = "Kritik olmayan kısa gecikme.",
            nextPeriodPlan = "Sonraki dönem kabul faaliyetleri.",
            managementExpectations = "Yönetim onayı beklenmektedir.",
            manualHealth = "İyi",
            reportStatus = "Yayımlandı"
        };

        // Act
        using var publishResponse = await client.PatchAsJsonAsync(
            $"/reports/{pirId}",
            updateAndPublishRequest);

        // Assert
        Assert.True(
            publishResponse.StatusCode != HttpStatusCode.NotFound,
            "AC-05 karşılanmıyor: PATCH /reports/{id} PİR güncelleme ve yayımlama endpoint'i uygulanmamış.");

        Assert.Equal(
            HttpStatusCode.OK,
            publishResponse.StatusCode);

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var publishedReport = await verificationDb.PirReports
            .AsNoTracking()
            .SingleAsync(report =>
                report.PirReportId == pirId);

        Assert.Equal(
            "Güncellenmiş ve yayımlanmış yönetici özeti.",
            publishedReport.ExecutiveSummary);

        Assert.Equal(
            "Güncellenmiş tamamlanan işler.",
            publishedReport.CompletedWork);

        Assert.Equal(
            "Yayımlandı",
            publishedReport.ReportStatus);

        Assert.Equal(
            "Yeşil",
            publishedReport.ManualHealth);

        Assert.Equal(
            credentials.User.UserId,
            publishedReport.PublishedByUserId);

        Assert.NotNull(publishedReport.PublishedAt);

        Assert.Equal(
            credentials.User.UserId,
            publishedReport.UpdatedByUserId);
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
