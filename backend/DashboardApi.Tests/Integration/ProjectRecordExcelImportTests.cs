using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class ProjectRecordExcelImportTests
{
    [Fact]
    public async Task ImportIssues_ValidAndInvalidRows_ReturnsPartialResultAndPersistsRiskRelationship()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await AuthenticateAsSystemAdministratorAsync(factory, client);

        const string importedIssueTitle = "Excel risk ilişkili entegrasyon sorunu";

        using var multipartContent = CreateMultipartContent(
            CreateIssueExcelFile(importedIssueTitle),
            "issue-import.xlsx");

        using var response = await client.PostAsync(
            "/projects/PRJ-001/issues/import",
            multipartContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExcelImportResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(1, result.TotalImported);
        Assert.Equal(1, result.TotalFailed);
        Assert.Contains(
            result.Errors,
            error =>
                error.Contains("Satır 3", StringComparison.Ordinal) &&
                error.Contains("RSK-002", StringComparison.Ordinal));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importedIssue = await db.Issues
            .AsNoTracking()
            .SingleAsync(issue => issue.IssueTitle == importedIssueTitle);

        Assert.StartsWith("ISS-", importedIssue.IssueId, StringComparison.Ordinal);
        Assert.Equal("PRJ-001", importedIssue.ProjectId);
        Assert.Equal("RSK-001", importedIssue.RiskId);
        Assert.Equal("Yüksek", importedIssue.IssuePriority);
        Assert.Equal("Kritik", importedIssue.IssueImpact);
        Assert.Equal("Devam Ediyor", importedIssue.IssueStatus);
        Assert.Equal("USR-PM1", importedIssue.IssueOwnerUserId);
        Assert.Equal(new DateTime(2027, 2, 15), importedIssue.IssueDueDate);
        Assert.Equal("Entegrasyon ortamındaki kapasite yetersizliği.", importedIssue.RootCause);
        Assert.Equal("Ek kapasite devreye alınacak.", importedIssue.IssueResolution);
        Assert.Null(importedIssue.ClosedDate);
    }

    [Fact]
    public async Task ImportActions_IssueLinkedAndConflictingRows_ReturnsPartialResultAndNormalizesSource()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await AuthenticateAsSystemAdministratorAsync(factory, client);

        const string importedActionDescription = "Excel sorun ilişkili entegrasyon aksiyonu";

        using var multipartContent = CreateMultipartContent(
            CreateActionExcelFile(importedActionDescription),
            "action-import.xlsx");

        using var response = await client.PostAsync(
            "/projects/PRJ-001/actions/import",
            multipartContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExcelImportResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(1, result.TotalImported);
        Assert.Equal(1, result.TotalFailed);
        Assert.Contains(
            result.Errors,
            error =>
                error.Contains("Satır 3", StringComparison.Ordinal) &&
                error.Contains("Bağlı Risk ID", StringComparison.Ordinal) &&
                error.Contains("Bağlı Sorun ID", StringComparison.Ordinal));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importedAction = await db.Actions
            .AsNoTracking()
            .SingleAsync(action => action.ActionDescription == importedActionDescription);

        Assert.StartsWith("ACT-", importedAction.ActionId, StringComparison.Ordinal);
        Assert.Equal("PRJ-001", importedAction.ProjectId);
        Assert.Null(importedAction.RiskId);
        Assert.Equal("ISS-001", importedAction.IssueId);
        Assert.Equal("Sorun", importedAction.SourceType);
        Assert.Equal("ISS-001", importedAction.SourceReference);
        Assert.Equal("USR-PM1", importedAction.ActionOwnerUserId);
        Assert.Equal(new DateTime(2027, 3, 20), importedAction.ActionDueDate);
        Assert.Equal("Devam Ediyor", importedAction.ActionStatus);
        Assert.Equal(35m, importedAction.ActionProgress);
        Assert.Equal("Kritik", importedAction.ActionPriority);
        Assert.Null(importedAction.CompletedDate);
    }

    private static byte[] CreateIssueExcelFile(string importedIssueTitle)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sorunlar");
        var headers = new[]
        {
            "Sorun Tanımı",
            "Öncelik",
            "Etki",
            "Durum",
            "Sorumlu",
            "Hedef Tarihi",
            "Kök Neden",
            "Çözüm",
            "Bağlı Risk ID"
        };

        WriteHeaders(worksheet, headers);

        worksheet.Cell(2, 1).Value = importedIssueTitle;
        worksheet.Cell(2, 2).Value = "yuksek";
        worksheet.Cell(2, 3).Value = "Kritik";
        worksheet.Cell(2, 4).Value = "devam ediyor";
        worksheet.Cell(2, 5).Value = "USR-PM1";
        worksheet.Cell(2, 6).Value = new DateTime(2027, 2, 15);
        worksheet.Cell(2, 7).Value =
            "Entegrasyon ortamındaki kapasite yetersizliği.";
        worksheet.Cell(2, 8).Value = "Ek kapasite devreye alınacak.";
        worksheet.Cell(2, 9).Value = "RSK-001";

        worksheet.Cell(3, 1).Value =
            "Başka projeye ait riskli geçersiz sorun";
        worksheet.Cell(3, 2).Value = "Orta";
        worksheet.Cell(3, 3).Value = "Yüksek";
        worksheet.Cell(3, 4).Value = "Açık";
        worksheet.Cell(3, 5).Value = "USR-PM1";
        worksheet.Cell(3, 6).Value = new DateTime(2027, 2, 20);
        worksheet.Cell(3, 7).Value = "Test kök nedeni.";
        worksheet.Cell(3, 9).Value = "RSK-002";

        return SaveWorkbook(workbook);
    }

    private static byte[] CreateActionExcelFile(string importedActionDescription)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Aksiyonlar");
        var headers = new[]
        {
            "Aksiyon Tanımı",
            "Kaynak Türü",
            "Kaynak Referans",
            "Öncelik",
            "Durum",
            "İlerleme %",
            "Sorumlu",
            "Hedef Tarihi",
            "Bağlı Risk ID",
            "Bağlı Sorun ID"
        };

        WriteHeaders(worksheet, headers);

        worksheet.Cell(2, 1).Value = importedActionDescription;
        worksheet.Cell(2, 2).Value = "Risk";
        worksheet.Cell(2, 3).Value = "YOK-SAYILMALI";
        worksheet.Cell(2, 4).Value = "Kritik";
        worksheet.Cell(2, 5).Value = "devam ediyor";
        worksheet.Cell(2, 6).Value = 35;
        worksheet.Cell(2, 7).Value = "USR-PM1";
        worksheet.Cell(2, 8).Value = new DateTime(2027, 3, 20);
        worksheet.Cell(2, 10).Value = "ISS-001";

        worksheet.Cell(3, 1).Value =
            "Risk ve sorun ilişkisi çakışan aksiyon";
        worksheet.Cell(3, 2).Value = "Diğer";
        worksheet.Cell(3, 3).Value = "ÇAKIŞMA";
        worksheet.Cell(3, 4).Value = "Orta";
        worksheet.Cell(3, 5).Value = "Açık";
        worksheet.Cell(3, 6).Value = 0;
        worksheet.Cell(3, 7).Value = "USR-PM1";
        worksheet.Cell(3, 8).Value = new DateTime(2027, 3, 25);
        worksheet.Cell(3, 9).Value = "RSK-001";
        worksheet.Cell(3, 10).Value = "ISS-001";

        return SaveWorkbook(workbook);
    }

    private static MultipartFormDataContent CreateMultipartContent(
        byte[] fileBytes,
        string fileName)
    {
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipartContent.Add(fileContent, "file", fileName);
        return multipartContent;
    }

    private static void WriteHeaders(
        IXLWorksheet worksheet,
        IReadOnlyList<string> headers)
    {
        for (var column = 0; column < headers.Count; column++)
            worksheet.Cell(1, column + 1).Value = headers[column];
    }

    private static byte[] SaveWorkbook(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static async Task AuthenticateAsSystemAdministratorAsync(
        TestWebApplicationFactory factory,
        HttpClient client)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var credentials = await new TestDataBuilder(db)
            .CreateActiveUserAsync("Sistem Yöneticisi");

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(credentials.User.Email, credentials.PlainTextPassword));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.Token);
    }
}
