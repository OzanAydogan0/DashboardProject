using System.Net;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using OfficeOpenXml;


namespace DashboardApi.Tests.Integration;

public sealed class ExportSecurityTests
{
        [Fact]
    public async Task GenerateHashEndpoint_AnonymousRequest_ReturnsNotFound()
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
            HttpStatusCode.NotFound,
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
            using var package = new ExcelPackage(stream);

            Assert.NotEmpty(package.Workbook.Worksheets);

            var worksheet =
                package.Workbook.Worksheets["PIR Raporu"];

            Assert.NotNull(worksheet);

            var title =
                worksheet.Cells["A1"].Text;

            Assert.False(string.IsNullOrWhiteSpace(title));

            Assert.Contains(
                expectedProjectCode,
                title,
                StringComparison.Ordinal);

            Assert.Equal(
                expectedProjectCode,
                worksheet.Cells["B3"].Text);

            Assert.Equal(
                expectedPeriod,
                worksheet.Cells["B5"].Text);

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