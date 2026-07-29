using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;

namespace DashboardApi.Tests.Integration;

public sealed class ExcelImportTests
{
    //Endpoint sorunu çözülünce testler yazılacak
    [Fact]
    public async Task ImportProjects_RowWithMissingRequiredField_ReturnsErrorWithoutImportingRow()
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

        const string projectName = "Eksik Kodlu Excel Test Projesi";

        var excelBytes = CreateInvalidExcelFile(projectName);

        using var multipartContent = new MultipartFormDataContent();

        using var fileContent =
            new ByteArrayContent(excelBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        multipartContent.Add(
            fileContent,
            "file",
            "invalid-project-import.xlsx");

        // Act
        using var response = await client.PostAsync(
            "/projects/import",
            multipartContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<ExcelImportResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(0, result.TotalImported);
        Assert.Equal(1, result.TotalFailed);

        var error = Assert.Single(result.Errors);

        Assert.Contains(
            "Satır 2",
            error);

        Assert.Contains(
            "zorunludur",
            error);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var projectWasImported = await db.Projects.AnyAsync(
            project => project.ProjectName == projectName);

        Assert.False(projectWasImported);
    }

    [Fact]
    public async Task ImportProjects_ValidRow_SetsManualHealthAndImportsProject()
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

        const string projectCode = "PRJ-EXCEL-001";
        const string projectName = "Manuel Sağlık Test Projesi";
        const string manualHealth = "Sarı";

        var excelBytes = CreateValidExcelFile(projectCode, projectName, manualHealth);

        using var multipartContent = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(excelBytes);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        multipartContent.Add(
            fileContent,
            "file",
            "valid-project-import.xlsx");

        using var response = await client.PostAsync(
            "/projects/import",
            multipartContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<ExcelImportResponse>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalImported);
        Assert.Equal(0, result.TotalFailed);
        Assert.Empty(result.Errors);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var importedProject = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectCode == projectCode);

        Assert.NotNull(importedProject);
        Assert.Equal(projectName, importedProject.ProjectName);
        Assert.Equal(manualHealth, importedProject.ManualHealth);
        Assert.Equal(1, importedProject.IsActive);
    }

    private static byte[] CreateInvalidExcelFile(
        string projectName)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        var worksheet =
            package.Workbook.Worksheets.Add("Projeler");

        // Başlık satırı
        worksheet.Cells[1, 1].Value = "Proje Kodu";
        worksheet.Cells[1, 2].Value = "Proje Adı";
        worksheet.Cells[1, 3].Value = "Müşteri Adı";
        worksheet.Cells[1, 4].Value = "PM E-posta";
        worksheet.Cells[1, 5].Value = "Başlangıç Tarihi";
        worksheet.Cells[1, 6].Value = "Bitiş Tarihi";
        worksheet.Cells[1, 7].Value = "BAC";
        worksheet.Cells[1, 8].Value = "Para Birimi";
        worksheet.Cells[1, 9].Value = "Gizlilik";
        worksheet.Cells[1, 10].Value = "Raporlama Sıklığı";
        worksheet.Cells[1, 11].Value = "Durum";
        worksheet.Cells[1, 12].Value = "Açıklama";
        worksheet.Cells[1, 13].Value = "Sağlık";
        worksheet.Cells[1, 14].Value = "Planlanan İlerleme";
        worksheet.Cells[1, 15].Value = "Gerçekleşen İlerleme";
        worksheet.Cells[1, 16].Value = "Aktiflik";

        /*
         * Proje kodu özellikle boş bırakılıyor.
         * Bu nedenle satır içe aktarılmamalıdır.
         */
        worksheet.Cells[2, 1].Value = null;
        worksheet.Cells[2, 2].Value = projectName;
        worksheet.Cells[2, 3].Value = "Test Müşterisi";
        worksheet.Cells[2, 4].Value = "pm1@pir.local";
        worksheet.Cells[2, 5].Value = "2026-08-01";
        worksheet.Cells[2, 6].Value = "2027-08-01";
        worksheet.Cells[2, 7].Value = 500000;
        worksheet.Cells[2, 8].Value = "TRY";
        worksheet.Cells[2, 9].Value = "Şirket İçi";
        worksheet.Cells[2, 10].Value = "Aylık";
        worksheet.Cells[2, 11].Value = "Aktif";
        worksheet.Cells[2, 12].Value = "Test proje açıklaması";
        worksheet.Cells[2, 13].Value = "Sarı";
        worksheet.Cells[2, 14].Value = 40;
        worksheet.Cells[2, 15].Value = 30;
        worksheet.Cells[2, 16].Value = 1;

        return package.GetAsByteArray();
    }

    private static byte[] CreateValidExcelFile(
        string projectCode,
        string projectName,
        string manualHealth)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Projeler");

        worksheet.Cells[1, 1].Value = "Proje Kodu";
        worksheet.Cells[1, 2].Value = "Proje Adı";
        worksheet.Cells[1, 3].Value = "Müşteri Adı";
        worksheet.Cells[1, 4].Value = "PM E-posta";
        worksheet.Cells[1, 5].Value = "Başlangıç Tarihi";
        worksheet.Cells[1, 6].Value = "Bitiş Tarihi";
        worksheet.Cells[1, 7].Value = "BAC";
        worksheet.Cells[1, 8].Value = "Para Birimi";
        worksheet.Cells[1, 9].Value = "Gizlilik";
        worksheet.Cells[1, 10].Value = "Raporlama Sıklığı";
        worksheet.Cells[1, 11].Value = "Durum";
        worksheet.Cells[1, 12].Value = "Açıklama";
        worksheet.Cells[1, 13].Value = "Sağlık";
        worksheet.Cells[1, 14].Value = "Planlanan İlerleme";
        worksheet.Cells[1, 15].Value = "Gerçekleşen İlerleme";
        worksheet.Cells[1, 16].Value = "Aktiflik";

        worksheet.Cells[2, 1].Value = projectCode;
        worksheet.Cells[2, 2].Value = projectName;
        worksheet.Cells[2, 3].Value = "Savunma Sanayii Başkanlığı";
        worksheet.Cells[2, 4].Value = "pm1@pir.local";
        worksheet.Cells[2, 5].Value = "2026-08-01";
        worksheet.Cells[2, 6].Value = "2027-08-01";
        worksheet.Cells[2, 7].Value = 500000;
        worksheet.Cells[2, 8].Value = "TRY";
        worksheet.Cells[2, 9].Value = "Şirket İçi";
        worksheet.Cells[2, 10].Value = "Aylık";
        worksheet.Cells[2, 11].Value = "Aktif";
        worksheet.Cells[2, 12].Value = "Excel sağlık dönüşümü test projesi";
        worksheet.Cells[2, 13].Value = manualHealth;
        worksheet.Cells[2, 14].Value = 40;
        worksheet.Cells[2, 15].Value = 30;
        worksheet.Cells[2, 16].Value = 1;

        return package.GetAsByteArray();
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
