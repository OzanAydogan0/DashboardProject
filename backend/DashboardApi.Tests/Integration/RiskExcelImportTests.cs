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

public sealed class RiskExcelImportTests
{
    [Fact]
    public async Task ImportProjectRisks_TableTemplate_ImportsValidRowsAndReportsInvalidRows()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(factory, "Sistem Yöneticisi");
        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(credentials.User.Email, credentials.PlainTextPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.Token);

        using var multipartContent = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(CreateRiskExcelFile());
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipartContent.Add(fileContent, "file", "risk-import-template.xlsx");

        using var response = await client.PostAsync(
            "/projects/PRJ-003/risks/import",
            multipartContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExcelImportResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(2, result.TotalImported);
        Assert.Equal(1, result.TotalFailed);
        Assert.Contains(result.Errors, error => error.Contains("Satır 4", StringComparison.Ordinal));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importedRisks = await db.Risks
            .AsNoTracking()
            .Where(risk =>
                risk.RiskTitle == "Excel ile içe aktarılan risk" ||
                risk.RiskTitle == "İkinci Excel riski")
            .OrderBy(risk => risk.RiskTitle)
            .ToListAsync();

        Assert.Equal(2, importedRisks.Count);
        Assert.Equal(2, importedRisks.Select(risk => risk.RiskId).Distinct().Count());

        var importedRisk = importedRisks.Single(risk => risk.RiskTitle == "Excel ile içe aktarılan risk");
        Assert.NotEqual("RSK-SABLON", importedRisk.RiskId);
        Assert.Equal("PRJ-003", importedRisk.ProjectId);
        Assert.Equal(4, importedRisk.RiskProbability);
        Assert.Equal(5, importedRisk.RiskImpact);
        Assert.Equal(20, importedRisk.RiskScore);
        Assert.Equal("USR-PM1", importedRisk.RiskOwnerUserId);
        Assert.Equal("İzleniyor", importedRisk.RiskStatus);
    }

    private static byte[] CreateRiskExcelFile()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Riskler");
        var headers = new[]
        {
            "ID",
            "Risk Başlığı",
            "Kategori",
            "Olasılık",
            "Etki",
            "Durum",
            "Azaltım / Müdahale",
            "Sorumlu",
            "Bitiş Tarihi",
            "İşlem"
        };

        for (var column = 0; column < headers.Length; column++)
            worksheet.Cells[1, column + 1].Value = headers[column];

        worksheet.Cells[2, 1].Value = "RSK-SABLON";
        worksheet.Cells[2, 2].Value = "Excel ile içe aktarılan risk";
        worksheet.Cells[2, 3].Value = "Teknik";
        worksheet.Cells[2, 4].Value = 4;
        worksheet.Cells[2, 5].Value = 5;
        worksheet.Cells[2, 6].Value = "İzleniyor";
        worksheet.Cells[2, 7].Value = "Alternatif teknik çözüm hazırlanacak.";
        worksheet.Cells[2, 8].Value = "Ahmet Yılmaz";
        worksheet.Cells[2, 9].Value = new DateTime(2027, 3, 15);

        worksheet.Cells[3, 2].Value = "İkinci Excel riski";
        worksheet.Cells[3, 3].Value = "Teknik";
        worksheet.Cells[3, 4].Value = 2;
        worksheet.Cells[3, 5].Value = 3;
        worksheet.Cells[3, 6].Value = "Kapalı";
        worksheet.Cells[3, 7].Value = "Kontrol tamamlandı.";
        worksheet.Cells[3, 8].Value = "pm1@pir.local";
        worksheet.Cells[3, 9].Value = new DateTime(2027, 4, 1);

        worksheet.Cells[4, 2].Value = "Geçersiz olasılıklı risk";
        worksheet.Cells[4, 3].Value = "Teknik";
        worksheet.Cells[4, 4].Value = 8;
        worksheet.Cells[4, 5].Value = 2;
        worksheet.Cells[4, 6].Value = "Açık";
        worksheet.Cells[4, 7].Value = "Takip edilecek.";
        worksheet.Cells[4, 8].Value = "USR-PM1";
        worksheet.Cells[4, 9].Value = new DateTime(2027, 5, 1);

        return package.GetAsByteArray();
    }

    private static async Task<TestUserCredentials> CreateActiveUserAsync(
        TestWebApplicationFactory factory,
        string role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await new TestDataBuilder(db).CreateActiveUserAsync(role);
    }
}
