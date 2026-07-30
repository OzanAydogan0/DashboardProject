using System.Globalization;
using System.Security.Claims;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace dashboardapi.Endpoints;

public static class ProjectRecordExcelImportEndpoints
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static void MapProjectRecordExcelImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("projects/{projectId}/issues/import", ImportIssuesAsync)
            .DisableAntiforgery();

        app.MapPost("projects/{projectId}/actions/import", ImportActionsAsync)
            .DisableAntiforgery();
    }

    private static async Task<IResult> ImportIssuesAsync(
        string projectId,
        [FromForm] IFormFile file,
        ClaimsPrincipal userClaims,
        AppDbContext db)
    {
        var userRole = PermissionHelper.GetUserRole(userClaims);
        var userId = PermissionHelper.GetUserId(userClaims);

        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        if (PermissionHelper.IsExecutive(userRole))
            return Results.Json(new { message = "Üst Yönetim rolü sorun içe aktaramaz." }, statusCode: 403);

        if (!await PermissionHelper.CanWriteProjectAsync(db, projectId, userId, userRole))
            return Results.Json(new { message = "Bu projeye sorun içe aktarma yetkiniz yok." }, statusCode: 403);

        var fileValidationResult = ValidateExcelFile(file, "Sorun");
        if (fileValidationResult is not null)
            return fileValidationResult;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet?.Dimension == null || worksheet.Dimension.End.Row < 2)
                return Results.BadRequest(new { message = "Excel dosyasında başlık ve en az bir veri satırı bulunmalıdır." });

            var columns = ReadHeaderColumns(worksheet);
            var titleColumn = FindColumn(
                columns,
                "Sorun Tanımı",
                "Sorun Tanimi",
                "Sorun Başlığı",
                "Sorun Basligi",
                "Başlık",
                "Baslik");
            var priorityColumn = FindColumn(columns, "Öncelik", "Oncelik", "Sorun Önceliği", "Sorun Onceligi");
            var impactColumn = FindColumn(columns, "Etki", "Sorun Etkisi");
            var statusColumn = FindColumn(columns, "Durum", "Sorun Durumu");
            var ownerColumn = FindColumn(columns, "Sorumlu", "Sorumlu Kullanıcı", "Sorun Sahibi");
            var dueDateColumn = FindColumn(columns, "Hedef Tarihi", "Bitiş Tarihi", "Sorun Hedef Tarihi");
            var rootCauseColumn = FindColumn(columns, "Kök Neden", "Kok Neden");
            var resolutionColumn = FindColumn(columns, "Çözüm", "Cozum", "Çözüm Açıklaması");
            var riskIdColumn = FindColumn(columns, "Bağlı Risk ID", "Bagli Risk ID", "Risk ID");

            var missingColumns = new (int Column, string Name)[]
                {
                    (titleColumn, "Sorun Tanımı"),
                    (priorityColumn, "Öncelik"),
                    (impactColumn, "Etki"),
                    (statusColumn, "Durum"),
                    (ownerColumn, "Sorumlu"),
                    (dueDateColumn, "Hedef Tarihi")
                }
                .Where(column => column.Column == 0)
                .Select(column => column.Name)
                .ToList();

            if (missingColumns.Count > 0)
            {
                return Results.BadRequest(new
                {
                    message = $"Excel şablonunda eksik sütunlar var: {string.Join(", ", missingColumns)}."
                });
            }

            var assignableUsers = await GetAssignableUsersAsync(db);
            if (assignableUsers.Count == 0)
                return Results.BadRequest(new { message = "Sorunlara atanabilecek uygun kullanıcı bulunamadı." });

            var projectRisks = (await db.Risks
                    .AsNoTracking()
                    .Where(risk => risk.ProjectId == projectId)
                    .ToListAsync())
                .ToDictionary(risk => risk.RiskId, StringComparer.OrdinalIgnoreCase);

            var importedCount = 0;
            var errors = new List<string>();

            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                if (IsEmptyRow(worksheet, row))
                    continue;

                var title = worksheet.Cells[row, titleColumn].Text.Trim();
                if (string.IsNullOrWhiteSpace(title))
                {
                    errors.Add($"Satır {row}: Sorun Tanımı zorunludur.");
                    continue;
                }

                var priority = NormalizeLevel(worksheet.Cells[row, priorityColumn].Text);
                if (priority is null)
                {
                    errors.Add($"Satır {row}: Öncelik Düşük, Orta, Yüksek veya Kritik olmalıdır.");
                    continue;
                }

                var impact = NormalizeLevel(worksheet.Cells[row, impactColumn].Text);
                if (impact is null)
                {
                    errors.Add($"Satır {row}: Etki Düşük, Orta, Yüksek veya Kritik olmalıdır.");
                    continue;
                }

                var status = NormalizeIssueStatus(worksheet.Cells[row, statusColumn].Text);
                if (status is null)
                {
                    errors.Add($"Satır {row}: Durum Açık, Devam Ediyor, Çözüldü veya Kapalı olmalıdır.");
                    continue;
                }

                if (!TryReadDate(worksheet.Cells[row, dueDateColumn], out var dueDate))
                {
                    errors.Add($"Satır {row}: Hedef Tarihi geçerli bir tarih olmalıdır.");
                    continue;
                }

                var ownerValue = worksheet.Cells[row, ownerColumn].Text.Trim();
                var ownerMatch = FindOwner(assignableUsers, ownerValue);
                if (ownerMatch.Ambiguous)
                {
                    errors.Add($"Satır {row}: '{ownerValue}' birden fazla kullanıcıyla eşleşiyor. Kullanıcı ID veya e-posta kullanın.");
                    continue;
                }

                if (ownerMatch.User is null)
                {
                    errors.Add($"Satır {row}: '{ownerValue}' için atanabilir bir sorumlu kullanıcı bulunamadı.");
                    continue;
                }

                string? riskId = null;
                if (riskIdColumn > 0)
                {
                    var riskIdValue = worksheet.Cells[row, riskIdColumn].Text.Trim();
                    if (!string.IsNullOrWhiteSpace(riskIdValue))
                    {
                        if (!projectRisks.TryGetValue(riskIdValue, out var linkedRisk))
                        {
                            errors.Add($"Satır {row}: '{riskIdValue}' bu projeye ait geçerli bir risk değildir.");
                            continue;
                        }

                        riskId = linkedRisk.RiskId;
                    }
                }

                var importedAt = DateTime.UtcNow;
                var rootCause = ReadOptionalText(worksheet, row, rootCauseColumn);
                var resolution = ReadOptionalText(worksheet, row, resolutionColumn);

                db.Issues.Add(new Issue
                {
                    IssueId = await IdentifierGenerator.GenerateAsync(db.Issues, issue => issue.IssueId, "ISS-"),
                    ProjectId = projectId,
                    RiskId = riskId,
                    IssueTitle = title,
                    IssuePriority = priority,
                    IssueOwnerUserId = ownerMatch.User.UserId,
                    IssueDueDate = dueDate.Date,
                    IssueStatus = status,
                    IssueImpact = impact,
                    RootCause = rootCause,
                    IssueResolution = resolution,
                    OpenedDate = importedAt,
                    ClosedDate = status is "Çözüldü" or "Kapalı" ? importedAt : null,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId,
                    CreatedAt = importedAt,
                    UpdatedAt = importedAt
                });

                importedCount++;
            }

            if (importedCount > 0)
                await db.SaveChangesAsync();

            return Results.Ok(new ExcelImportResponse(
                Success: errors.Count == 0,
                TotalImported: importedCount,
                TotalFailed: errors.Count,
                Errors: errors));
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest(new { message = "Excel dosyası okunamadı. Geçerli bir .xlsx dosyası yükleyin." });
        }
        catch (Exception exception)
        {
            return Results.Json(
                new { message = "Sorunlar Excel dosyasından içe aktarılırken hata oluştu.", detail = exception.Message },
                statusCode: 500);
        }
    }

    private static async Task<IResult> ImportActionsAsync(
        string projectId,
        [FromForm] IFormFile file,
        ClaimsPrincipal userClaims,
        AppDbContext db)
    {
        var userRole = PermissionHelper.GetUserRole(userClaims);
        var userId = PermissionHelper.GetUserId(userClaims);

        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        if (PermissionHelper.IsExecutive(userRole))
            return Results.Json(new { message = "Üst Yönetim rolü aksiyon içe aktaramaz." }, statusCode: 403);

        if (!await PermissionHelper.CanWriteProjectAsync(db, projectId, userId, userRole))
            return Results.Json(new { message = "Bu projeye aksiyon içe aktarma yetkiniz yok." }, statusCode: 403);

        var fileValidationResult = ValidateExcelFile(file, "Aksiyon");
        if (fileValidationResult is not null)
            return fileValidationResult;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet?.Dimension == null || worksheet.Dimension.End.Row < 2)
                return Results.BadRequest(new { message = "Excel dosyasında başlık ve en az bir veri satırı bulunmalıdır." });

            var columns = ReadHeaderColumns(worksheet);
            var descriptionColumn = FindColumn(
                columns,
                "Aksiyon Tanımı",
                "Aksiyon Tanimi",
                "Aksiyon Açıklaması",
                "Aksiyon Aciklamasi",
                "Açıklama",
                "Aciklama");
            var sourceTypeColumn = FindColumn(columns, "Kaynak Türü", "Kaynak Turu", "Kaynak Tipi");
            var sourceReferenceColumn = FindColumn(columns, "Kaynak Referans", "Kaynak Referansı", "Referans");
            var priorityColumn = FindColumn(columns, "Öncelik", "Oncelik", "Aksiyon Önceliği", "Aksiyon Onceligi");
            var statusColumn = FindColumn(columns, "Durum", "Aksiyon Durumu");
            var progressColumn = FindColumn(columns, "İlerleme", "Ilerleme", "İlerleme %", "Ilerleme %", "Tamamlanma %");
            var ownerColumn = FindColumn(columns, "Sorumlu", "Sorumlu Kullanıcı", "Aksiyon Sahibi");
            var dueDateColumn = FindColumn(columns, "Hedef Tarihi", "Bitiş Tarihi", "Aksiyon Hedef Tarihi");
            var riskIdColumn = FindColumn(columns, "Bağlı Risk ID", "Bagli Risk ID", "Risk ID");
            var issueIdColumn = FindColumn(columns, "Bağlı Sorun ID", "Bagli Sorun ID", "Sorun ID", "Issue ID");

            var missingColumns = new (int Column, string Name)[]
                {
                    (descriptionColumn, "Aksiyon Tanımı"),
                    (sourceTypeColumn, "Kaynak Türü"),
                    (priorityColumn, "Öncelik"),
                    (statusColumn, "Durum"),
                    (progressColumn, "İlerleme %"),
                    (ownerColumn, "Sorumlu"),
                    (dueDateColumn, "Hedef Tarihi")
                }
                .Where(column => column.Column == 0)
                .Select(column => column.Name)
                .ToList();

            if (missingColumns.Count > 0)
            {
                return Results.BadRequest(new
                {
                    message = $"Excel şablonunda eksik sütunlar var: {string.Join(", ", missingColumns)}."
                });
            }

            var assignableUsers = await GetAssignableUsersAsync(db);
            if (assignableUsers.Count == 0)
                return Results.BadRequest(new { message = "Aksiyonlara atanabilecek uygun kullanıcı bulunamadı." });

            var projectRisks = (await db.Risks
                    .AsNoTracking()
                    .Where(risk => risk.ProjectId == projectId)
                    .ToListAsync())
                .ToDictionary(risk => risk.RiskId, StringComparer.OrdinalIgnoreCase);

            var projectIssues = (await db.Issues
                    .AsNoTracking()
                    .Where(issue => issue.ProjectId == projectId)
                    .ToListAsync())
                .ToDictionary(issue => issue.IssueId, StringComparer.OrdinalIgnoreCase);

            var importedCount = 0;
            var errors = new List<string>();

            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                if (IsEmptyRow(worksheet, row))
                    continue;

                var description = worksheet.Cells[row, descriptionColumn].Text.Trim();
                if (string.IsNullOrWhiteSpace(description))
                {
                    errors.Add($"Satır {row}: Aksiyon Tanımı zorunludur.");
                    continue;
                }

                var sourceType = NormalizeSourceType(worksheet.Cells[row, sourceTypeColumn].Text);
                if (sourceType is null)
                {
                    errors.Add($"Satır {row}: Kaynak Türü Risk, Sorun, Kilometre Taşı, PIR, Yönetim Kararı veya Diğer olmalıdır.");
                    continue;
                }

                var priority = NormalizeLevel(worksheet.Cells[row, priorityColumn].Text);
                if (priority is null)
                {
                    errors.Add($"Satır {row}: Öncelik Düşük, Orta, Yüksek veya Kritik olmalıdır.");
                    continue;
                }

                var status = NormalizeActionStatus(worksheet.Cells[row, statusColumn].Text);
                if (status is null)
                {
                    errors.Add($"Satır {row}: Durum Açık, Devam Ediyor, Tamamlandı veya İptal olmalıdır.");
                    continue;
                }

                if (!TryReadProgress(worksheet.Cells[row, progressColumn], out var progress))
                {
                    errors.Add($"Satır {row}: İlerleme 0 ile 100 arasında bir sayı olmalıdır.");
                    continue;
                }

                if ((status == "Tamamlandı") != (progress == 100m))
                {
                    errors.Add($"Satır {row}: Tamamlandı durumundaki aksiyonun ilerlemesi 100 olmalıdır; ilerleme 100 ise durum Tamamlandı olmalıdır.");
                    continue;
                }

                if (!TryReadDate(worksheet.Cells[row, dueDateColumn], out var dueDate))
                {
                    errors.Add($"Satır {row}: Hedef Tarihi geçerli bir tarih olmalıdır.");
                    continue;
                }

                var ownerValue = worksheet.Cells[row, ownerColumn].Text.Trim();
                var ownerMatch = FindOwner(assignableUsers, ownerValue);
                if (ownerMatch.Ambiguous)
                {
                    errors.Add($"Satır {row}: '{ownerValue}' birden fazla kullanıcıyla eşleşiyor. Kullanıcı ID veya e-posta kullanın.");
                    continue;
                }

                if (ownerMatch.User is null)
                {
                    errors.Add($"Satır {row}: '{ownerValue}' için atanabilir bir sorumlu kullanıcı bulunamadı.");
                    continue;
                }

                var riskIdValue = ReadOptionalText(worksheet, row, riskIdColumn);
                var issueIdValue = ReadOptionalText(worksheet, row, issueIdColumn);

                if (riskIdValue is not null && issueIdValue is not null)
                {
                    errors.Add($"Satır {row}: Bağlı Risk ID ve Bağlı Sorun ID aynı anda kullanılamaz.");
                    continue;
                }

                string? riskId = null;
                string? issueId = null;

                if (riskIdValue is not null)
                {
                    if (!projectRisks.TryGetValue(riskIdValue, out var linkedRisk))
                    {
                        errors.Add($"Satır {row}: '{riskIdValue}' bu projeye ait geçerli bir risk değildir.");
                        continue;
                    }

                    riskId = linkedRisk.RiskId;
                    sourceType = "Risk";
                }

                if (issueIdValue is not null)
                {
                    if (!projectIssues.TryGetValue(issueIdValue, out var linkedIssue))
                    {
                        errors.Add($"Satır {row}: '{issueIdValue}' bu projeye ait geçerli bir sorun değildir.");
                        continue;
                    }

                    issueId = linkedIssue.IssueId;
                    sourceType = "Sorun";
                }

                var sourceReference =
                    issueId ??
                    riskId ??
                    ReadOptionalText(worksheet, row, sourceReferenceColumn);

                var importedAt = DateTime.UtcNow;
                db.Actions.Add(new dashboardapi.Models.Action
                {
                    ActionId = await IdentifierGenerator.GenerateAsync(db.Actions, action => action.ActionId, "ACT-"),
                    ProjectId = projectId,
                    RiskId = riskId,
                    IssueId = issueId,
                    ActionDescription = description,
                    SourceType = sourceType,
                    SourceReference = sourceReference,
                    ActionOwnerUserId = ownerMatch.User.UserId,
                    ActionDueDate = dueDate.Date,
                    ActionStatus = status,
                    ActionProgress = progress,
                    ActionPriority = priority,
                    CompletedDate = status == "Tamamlandı" ? importedAt : null,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId,
                    CreatedAt = importedAt,
                    UpdatedAt = importedAt
                });

                importedCount++;
            }

            if (importedCount > 0)
                await db.SaveChangesAsync();

            return Results.Ok(new ExcelImportResponse(
                Success: errors.Count == 0,
                TotalImported: importedCount,
                TotalFailed: errors.Count,
                Errors: errors));
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest(new { message = "Excel dosyası okunamadı. Geçerli bir .xlsx dosyası yükleyin." });
        }
        catch (Exception exception)
        {
            return Results.Json(
                new { message = "Aksiyonlar Excel dosyasından içe aktarılırken hata oluştu.", detail = exception.Message },
                statusCode: 500);
        }
    }

    private static IResult? ValidateExcelFile(IFormFile? file, string recordName)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "Lütfen geçerli bir Excel dosyası yükleyin." });

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                message = $"{recordName} içe aktarma işlemi yalnızca .xlsx dosyalarını destekler."
            });
        }

        return null;
    }

    private static async Task<List<User>> GetAssignableUsersAsync(AppDbContext db) =>
        (await db.Users.AsNoTracking().ToListAsync())
        .Where(user =>
            !PermissionHelper.IsSystemAdmin(user.UserRole) &&
            !PermissionHelper.IsExecutive(user.UserRole))
        .ToList();

    private static Dictionary<string, int> ReadHeaderColumns(ExcelWorksheet worksheet)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var column = 1; column <= worksheet.Dimension.End.Column; column++)
        {
            var header = NormalizeHeader(worksheet.Cells[1, column].Text);
            if (!string.IsNullOrEmpty(header))
                columns.TryAdd(header, column);
        }

        return columns;
    }

    private static int FindColumn(IReadOnlyDictionary<string, int> columns, params string[] names)
    {
        foreach (var name in names)
        {
            if (columns.TryGetValue(NormalizeHeader(name), out var column))
                return column;
        }

        return 0;
    }

    private static string NormalizeHeader(string value) =>
        string.Concat((value ?? string.Empty)
            .Trim()
            .ToLower(TurkishCulture)
            .Where(char.IsLetterOrDigit));

    private static bool IsEmptyRow(ExcelWorksheet worksheet, int row) =>
        Enumerable.Range(1, worksheet.Dimension.End.Column)
            .All(column => string.IsNullOrWhiteSpace(worksheet.Cells[row, column].Text));

    private static string? ReadOptionalText(ExcelWorksheet worksheet, int row, int column)
    {
        if (column <= 0)
            return null;

        var value = worksheet.Cells[row, column].Text.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeLevel(string value) =>
        value.Trim().ToLower(TurkishCulture) switch
        {
            "düşük" or "dusuk" => "Düşük",
            "orta" => "Orta",
            "yüksek" or "yuksek" => "Yüksek",
            "kritik" => "Kritik",
            _ => null
        };

    private static string? NormalizeIssueStatus(string value) =>
        value.Trim().ToLower(TurkishCulture) switch
        {
            "açık" or "acik" => "Açık",
            "devam ediyor" => "Devam Ediyor",
            "çözüldü" or "cozuldu" => "Çözüldü",
            "kapalı" or "kapali" => "Kapalı",
            _ => null
        };

    private static string? NormalizeActionStatus(string value) =>
        value.Trim().ToLower(TurkishCulture) switch
        {
            "açık" or "acik" => "Açık",
            "devam ediyor" => "Devam Ediyor",
            "tamamlandı" or "tamamlandi" => "Tamamlandı",
            "iptal" => "İptal",
            _ => null
        };

    private static string? NormalizeSourceType(string value) =>
        value.Trim().ToLower(TurkishCulture) switch
        {
            "risk" => "Risk",
            "sorun" or "issue" => "Sorun",
            "kilometre taşı" or "kilometre tasi" or "milestone" => "Kilometre Taşı",
            "pir" => "PIR",
            "yönetim kararı" or "yonetim karari" => "Yönetim Kararı",
            "diğer" or "diger" => "Diğer",
            _ => null
        };

    private static bool TryReadProgress(ExcelRangeBase cell, out decimal progress)
    {
        var text = cell.Text.Trim().TrimEnd('%').Trim();
        var parsed =
            decimal.TryParse(text, NumberStyles.Number, TurkishCulture, out progress) ||
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out progress);

        return parsed && progress is >= 0m and <= 100m;
    }

    private static bool TryReadDate(ExcelRangeBase cell, out DateTime date)
    {
        if (cell.Value is DateTime dateValue)
        {
            date = dateValue.Date;
            return true;
        }

        if (cell.Value is double serialDate)
        {
            try
            {
                date = DateTime.FromOADate(serialDate).Date;
                return true;
            }
            catch (ArgumentException)
            {
                // Metin olarak tarih ayrıştırmayı dene.
            }
        }

        var text = cell.Text.Trim();
        if (DateTime.TryParse(text, TurkishCulture, DateTimeStyles.AllowWhiteSpaces, out date) ||
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
        {
            date = date.Date;
            return true;
        }

        date = default;
        return false;
    }

    private static (User? User, bool Ambiguous) FindOwner(
        IReadOnlyCollection<User> users,
        string ownerValue)
    {
        if (string.IsNullOrWhiteSpace(ownerValue))
            return (null, false);

        var identityMatches = users
            .Where(user =>
                string.Equals(user.UserId, ownerValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.Email, ownerValue, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (identityMatches.Count == 1)
            return (identityMatches[0], false);

        if (identityMatches.Count > 1)
            return (null, true);

        var fullNameMatches = users
            .Where(user =>
                TurkishCulture.CompareInfo.Compare(
                    user.FullName,
                    ownerValue,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0)
            .ToList();

        return fullNameMatches.Count switch
        {
            1 => (fullNameMatches[0], false),
            > 1 => (null, true),
            _ => (null, false)
        };
    }
}
