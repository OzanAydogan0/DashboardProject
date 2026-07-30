using System.Net;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;


namespace DashboardApi.Tests.Integration;

public sealed class ExportSecurityTests
{
        [Fact]
    public async Task GenerateHashEndpoint_AnonymousRequest_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        int userCountBeforeRequest;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            userCountBeforeRequest =
                await db.Users.CountAsync();
        }


        // Act
        using var response = await client.GetAsync(
            "/auth/generate-hash?password=Test123%21");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            Assert.DoesNotContain(
                "PlainText",
                responseBody,
                StringComparison.OrdinalIgnoreCase);

            Assert.DoesNotContain(
                "BcryptHash",
                responseBody,
                StringComparison.OrdinalIgnoreCase);
        }

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var userCountAfterRequest =
            await verificationDb.Users.CountAsync();

        Assert.Equal(
            userCountBeforeRequest,
            userCountAfterRequest);
    }
    
        // Başarısız GET isteği veritabanını değiştirmemelidir.
    [Fact]
    public async Task GetPirPdf_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        string pirId;
        int pirCountBeforeRequest;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var publishedPir = await db.PirReports
                .AsNoTracking()
                .FirstOrDefaultAsync(report =>
                    report.ReportStatus == "Yayımlandı");

            Assert.NotNull(publishedPir);

            pirId = publishedPir.PirReportId;
            pirCountBeforeRequest = await db.PirReports.CountAsync();
        }

        // Act
        using var response = await client.GetAsync(
            $"/pirs/{pirId}/export/pdf");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.NotEqual(
            "application/pdf",
            response.Content.Headers.ContentType?.MediaType);

        var responseBytes =
            await response.Content.ReadAsByteArrayAsync();

        if (responseBytes.Length >= 4)
        {
            var signature = Encoding.ASCII.GetString(
                responseBytes,
                0,
                4);

            Assert.NotEqual("%PDF", signature);
        }

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var pirCountAfterRequest =
            await verificationDb.PirReports.CountAsync();

        Assert.Equal(
            pirCountBeforeRequest,
            pirCountAfterRequest);
    }

    [Theory]
    [InlineData("/pirs/PIR-01/export/pdf")]
    [InlineData("/reports/PIR-01/export/pdf")]
    [InlineData("/pirs/PIR-01/export/excel")]
    [InlineData("/reports/PIR-01/export/excel")]
    public async Task ExportPir_FromAnotherManagersProject_ReturnsForbidden(
        string endpoint)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        TestUserCredentials credentials;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            credentials = await new TestDataBuilder(db)
                .CreateActiveUserAsync("Proje Yöneticisi");

            var ownProject = await db.Projects
                .SingleAsync(project => project.ProjectId == "PRJ-002");
            ownProject.ProjectManagerUserId = credentials.User.UserId;
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

        using var response = await client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(
            "application/pdf",
            response.Content.Headers.ContentType?.MediaType);
        Assert.NotEqual(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
    }

        [Fact]
        public async Task ExportPirExcel_WithValidReport_ReturnsValidXlsx()
        {
            // Arrange
            await using var factory = new TestWebApplicationFactory();
            using var client = factory.CreateHttpsClient();

            TestUserCredentials credentials;
            string pirId;
            string expectedProjectCode;
            string expectedPeriod;
            int pirCountBeforeRequest;

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var builder = new TestDataBuilder(db);

                credentials = await builder.CreateActiveUserAsync(
                    role: "Sistem Yöneticisi");

                var report = await db.PirReports
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.ReportStatus == "Yayımlandı");

                Assert.NotNull(report);

                var project = await db.Projects
                    .AsNoTracking()
                    .SingleAsync(item =>
                        item.ProjectId == report.ProjectId);

                pirId = report.PirReportId;
                expectedProjectCode = project.ProjectCode;
                expectedPeriod = report.Period;

                pirCountBeforeRequest =
                    await db.PirReports.CountAsync();
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
                $"/pirs/{pirId}/export/excel");

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            Assert.Equal(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                response.Content.Headers.ContentType?.MediaType);

            var excelBytes =
                await response.Content.ReadAsByteArrayAsync();

            Assert.NotEmpty(excelBytes);

            Assert.True(
                excelBytes.Length > 500,
                $"Üretilen XLSX beklenenden küçük: {excelBytes.Length} byte.");

            Assert.True(
                excelBytes.Length >= 2,
                "ZIP imzasını kontrol etmek için yeterli byte dönmedi.");

            Assert.Equal((byte)'P', excelBytes[0]);
            Assert.Equal((byte)'K', excelBytes[1]);

            var contentDisposition =
                response.Content.Headers.ContentDisposition;

            if (contentDisposition is not null)
            {
                var fileName =
                    contentDisposition.FileNameStar ??
                    contentDisposition.FileName;

                Assert.False(string.IsNullOrWhiteSpace(fileName));

                fileName = fileName.Trim('"');

                Assert.EndsWith(
                    ".xlsx",
                    fileName,
                    StringComparison.OrdinalIgnoreCase);
            }

            using var stream = new MemoryStream(excelBytes);
            using var workbook = new XLWorkbook(stream);

            Assert.NotEmpty(workbook.Worksheets);

            Assert.True(
                workbook.TryGetWorksheet(
                    "PIR Raporu",
                    out var worksheet));

            var title =
                worksheet.Cell("A1").GetFormattedString();

            Assert.False(string.IsNullOrWhiteSpace(title));

            Assert.Contains(
                expectedProjectCode,
                title,
                StringComparison.Ordinal);

            Assert.Equal(
                expectedProjectCode,
                worksheet.Cell("B3").GetFormattedString());

            Assert.Equal(
                expectedPeriod,
                worksheet.Cell("B5").GetFormattedString());

            using var verificationScope =
                factory.Services.CreateScope();

            var verificationDb = verificationScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var pirCountAfterRequest =
                await verificationDb.PirReports.CountAsync();

            // Export amaçlı GET isteği DB üzerinde değişiklik yapmamalıdır.
            Assert.Equal(
                pirCountBeforeRequest,
                pirCountAfterRequest);
        }

}
