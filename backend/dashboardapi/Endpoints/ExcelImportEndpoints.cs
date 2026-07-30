using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using dashboardapi.Data;
using dashboardapi.Models;
using dashboardapi.DTOs;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class ExcelImportEndpoints
{
    public static void MapExcelImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("projects/import", async (
            [FromForm] IFormFile file, // 👈 1. [FromForm] özniteliği eklendi (415 hatasını çözer)
            ClaimsPrincipal userClaims, 
            AppDbContext db,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ProjectExcelImport");
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (userRole != "Sistem Yöneticisi")
                return Results.Json(new { message = "Bu operasyon için Sistem Yöneticisi yetkiniz olmalıdır!" }, statusCode: 403);

            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "Lütfen geçerli bir Excel dosyası yükleyin." });


            var importedCount = 0;
            var errorLogs = new List<string>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null) return Results.BadRequest(new { message = "Çalışma sayfası bulunamadı." });

                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                if (rowCount < 2) return Results.BadRequest(new { message = "İşlenecek veri bulunamadı." });

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var projectCode = ReadCellText(worksheet.Cell(row, 1));
                        var projectName = ReadCellText(worksheet.Cell(row, 2));
                        var customerName = ReadCellText(worksheet.Cell(row, 3));
                        var pmIdentifier = ReadCellText(worksheet.Cell(row, 4)); // PM ID veya E-posta
                        var startDateStr = ReadCellText(worksheet.Cell(row, 5));
                        var endDateStr = ReadCellText(worksheet.Cell(row, 6));
                        var bacStr = ReadCellText(worksheet.Cell(row, 7));
                        var currency = ReadCellText(worksheet.Cell(row, 8)) ?? "TRY";
                        var confidentiality = ReadCellText(worksheet.Cell(row, 9)) ?? "Şirket İçi";
                        var reportingFreq = ReadCellText(worksheet.Cell(row, 10)) ?? "Aylık";
                        var status = ReadCellText(worksheet.Cell(row, 11)) ?? "Planlandı";
                        var projectDescription = ReadCellText(worksheet.Cell(row, 12));
                        var manualHealth = ReadCellText(worksheet.Cell(row, 13)) ?? HealthStatusHelper.Good;
                        var plannedProgressStr = ReadCellText(worksheet.Cell(row, 14));
                        var actualProgressStr = ReadCellText(worksheet.Cell(row, 15));
                        var isActiveStr = ReadCellText(worksheet.Cell(row, 16));

                        if (string.IsNullOrEmpty(projectCode) || string.IsNullOrEmpty(projectName) || 
                            string.IsNullOrEmpty(pmIdentifier) || string.IsNullOrEmpty(customerName))
                        {
                            errorLogs.Add($"Satır {row}: Proje Kodu, Adı, Müşteri Adı ve PM ID/E-posta alanları zorunludur.");
                            continue;
                        }

                        // 🔍 Validasyon 1: Mükerrer Proje Kontrolü
                        if (await db.Projects.AnyAsync(p => p.ProjectCode == projectCode))
                        {
                            errorLogs.Add($"Satır {row}: '{projectCode}' kodlu proje zaten mevcut.");
                            continue;
                        }

                        // 🧠 AKILLI EŞLEŞTİRME & OTOMATİK OLUŞTURMA (Get or Create)
                        
                        // 1. Varsayılan bir Programı Bul ya da Oluştur
                        var program = db.Programs.Local.FirstOrDefault(p => p.ProgramStatus == "Aktif")
                                      ?? await db.Programs.Where(p => p.ProgramStatus == "Aktif").OrderBy(p => p.CreatedAt).FirstOrDefaultAsync();

                        if (program == null)
                        {
                            program = new dashboardapi.Models.Program
                            {
                                ProgramId = await IdentifierGenerator.GenerateAsync(db.Set<dashboardapi.Models.Program>(), p => p.ProgramId, "PRG-"),
                                ProgramName = "Genel Program",
                                ProgramDescription = "Excel içe aktarımı ile otomatik oluşturuldu.",
                                ProgramStatus = "Aktif",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            db.Programs.Add(program);
                        }

                        // 2. Müşteriyi Bul veya Otomatik Oluştur
                        var customer = db.Customers.Local.FirstOrDefault(c => c.CustomerName == customerName) 
                                       ?? await db.Customers.FirstOrDefaultAsync(c => c.CustomerName == customerName);

                        if (customer == null)
                        {
                            customer = new Customer
                            {
                                CustomerId = await CustomerIdGenerator.GenerateAsync(db),
                                CustomerName = customerName,
                                CustomerType = "Genel",
                                CustomerStatus = "Aktif",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            db.Customers.Add(customer);
                        }

                        // 3. Proje Yöneticisini Bul (Hem E-posta hem de UserId desteği)
                        var pmUser = await db.Users.FirstOrDefaultAsync(u => u.Email == pmIdentifier || u.UserId == pmIdentifier);
                        if (pmUser == null)
                        {
                            errorLogs.Add($"Satır {row}: '{pmIdentifier}' bilgisine sahip bir kullanıcı (PM) bulunamadı.");
                            continue;
                        }

                        // Tarih ve Bütçe Dönüşümleri
                        DateTime.TryParse(startDateStr, out DateTime startDate);
                        DateTime.TryParse(endDateStr, out DateTime endDate);
                        if (startDate == DateTime.MinValue) startDate = DateTime.UtcNow;
                        if (endDate == DateTime.MinValue) endDate = DateTime.UtcNow.AddMonths(6);

                        decimal.TryParse(bacStr, out decimal bacValue);
                        decimal.TryParse(plannedProgressStr, out decimal plannedProgressValue);
                        decimal.TryParse(actualProgressStr, out decimal actualProgressValue);
                        int isActiveValue = 1;
                        if (!string.IsNullOrEmpty(isActiveStr))
                        {
                            if (!int.TryParse(isActiveStr, out isActiveValue))
                            {
                                isActiveValue = isActiveStr.Trim().Equals("Pasif", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                            }
                        }

                        // Projeyi Oluştur
                        var newProject = new Project
                        {
                            ProjectId = await IdentifierGenerator.GenerateAsync(db.Projects, p => p.ProjectId, "PRJ-"),
                            ProjectCode = projectCode,
                            ProjectName = projectName,
                            ProgramId = program.ProgramId,
                            CustomerId = customer.CustomerId,
                            ProjectManagerUserId = pmUser.UserId, 
                            StartDate = startDate,
                            BaselineFinishDate = endDate,
                            ForecastFinishDate = endDate,
                            ActualFinishDate = null,
                            ProjectStatus = status,
                            ManualHealth = HealthStatusHelper.ToStorageValue(manualHealth, HealthStatusHelper.Good),
                            PlannedProgress = plannedProgressValue,
                            ActualProgress = actualProgressValue,
                            Bac = bacValue,
                            Currency = currency,
                            ReportingFrequency = reportingFreq,
                            Confidentiality = confidentiality,
                            ProjectDescription = projectDescription,
                            IsActive = isActiveValue == 0 ? 0 : 1,
                            CreatedByUserId = userId,
                            UpdatedByUserId = userId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        db.Projects.Add(newProject);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Proje Excel içe aktarımında {RowNumber}. satır işlenemedi.",
                            row);
                        errorLogs.Add($"Satır {row}: Beklenmeyen bir hata oluştu.");
                    }
                }

                if (importedCount > 0)
                {
                    await db.SaveChangesAsync();
                }
            }

            return Results.Ok(new ExcelImportResponse(
                Success: errorLogs.Count == 0,
                TotalImported: importedCount,
                TotalFailed: errorLogs.Count,
                Errors: errorLogs
            ));
        })
        .DisableAntiforgery(); // 👈 2. Form yüklemelerinde .NET 8 Anti-Forgery doğrulaması kapatıldı

        app.MapPost("projects/{projectId}/risks/import", async (
            string projectId,
            [FromForm] IFormFile file,
            ClaimsPrincipal userClaims,
            AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü risk içe aktaramaz." }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, projectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye risk içe aktarma yetkiniz yok." }, statusCode: 403);

            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "Lütfen geçerli bir Excel dosyası yükleyin." });

            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Risk içe aktarma işlemi yalnızca .xlsx dosyalarını destekler." });


            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                var lastRow = worksheet?.LastRowUsed()?.RowNumber() ?? 0;
                if (worksheet == null || lastRow < 2)
                    return Results.BadRequest(new { message = "Excel dosyasında başlık ve en az bir veri satırı bulunmalıdır." });

                var headerColumns = ReadHeaderColumns(worksheet);
                var riskTitleColumn = FindColumn(headerColumns, "Risk Başlığı", "Risk Basligi", "Başlık");
                var categoryColumn = FindColumn(headerColumns, "Kategori", "Risk Kategorisi");
                var probabilityColumn = FindColumn(headerColumns, "Olasılık", "Olasilik");
                var impactColumn = FindColumn(headerColumns, "Etki");
                var statusColumn = FindColumn(headerColumns, "Durum", "Risk Durumu");
                var mitigationColumn = FindColumn(
                    headerColumns,
                    "Azaltım / Müdahale",
                    "Azaltim / Mudahale",
                    "Risk Azaltım",
                    "Müdahale");
                var ownerColumn = FindColumn(headerColumns, "Sorumlu", "Sorumlu Kullanıcı", "Risk Sahibi");
                var dueDateColumn = FindColumn(headerColumns, "Bitiş Tarihi", "Bitis Tarihi", "Risk Bitiş Tarihi");

                var requiredColumns = new (int Column, string Name)[]
                {
                    (riskTitleColumn, "Risk Başlığı"),
                    (categoryColumn, "Kategori"),
                    (probabilityColumn, "Olasılık"),
                    (impactColumn, "Etki"),
                    (statusColumn, "Durum"),
                    (mitigationColumn, "Azaltım / Müdahale"),
                    (ownerColumn, "Sorumlu"),
                    (dueDateColumn, "Bitiş Tarihi")
                };

                var missingColumns = requiredColumns
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

                var assignableUsers = (await db.Users.AsNoTracking().ToListAsync())
                    .Where(user =>
                        !PermissionHelper.IsSystemAdmin(user.UserRole) &&
                        !PermissionHelper.IsExecutive(user.UserRole))
                    .ToList();

                if (assignableUsers.Count == 0)
                    return Results.BadRequest(new { message = "Risklere atanabilecek uygun kullanıcı bulunamadı." });

                var importedCount = 0;
                var errorLogs = new List<string>();

                for (var row = 2; row <= lastRow; row++)
                {
                    if (IsEmptyRow(worksheet, row))
                        continue;

                    var riskTitle = GetCellText(worksheet.Cell(row, riskTitleColumn));
                    if (string.IsNullOrWhiteSpace(riskTitle))
                    {
                        errorLogs.Add($"Satır {row}: Risk Başlığı zorunludur.");
                        continue;
                    }

                    var category = GetCellText(worksheet.Cell(row, categoryColumn));
                    if (string.IsNullOrWhiteSpace(category))
                    {
                        errorLogs.Add($"Satır {row}: Kategori zorunludur.");
                        continue;
                    }

                    if (!int.TryParse(
                            GetCellText(worksheet.Cell(row, probabilityColumn)),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var probability) ||
                        probability is < 1 or > 5)
                    {
                        errorLogs.Add($"Satır {row}: Olasılık 1 ile 5 arasında bir tam sayı olmalıdır.");
                        continue;
                    }

                    if (!int.TryParse(
                            GetCellText(worksheet.Cell(row, impactColumn)),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var impact) ||
                        impact is < 1 or > 5)
                    {
                        errorLogs.Add($"Satır {row}: Etki 1 ile 5 arasında bir tam sayı olmalıdır.");
                        continue;
                    }

                    var riskStatus = NormalizeRiskStatus(GetCellText(worksheet.Cell(row, statusColumn)));
                    if (riskStatus == null)
                    {
                        errorLogs.Add($"Satır {row}: Durum Açık, İzleniyor, Azaltıldı veya Kapalı olmalıdır.");
                        continue;
                    }

                    if (!TryReadDate(worksheet.Cell(row, dueDateColumn), out var dueDate))
                    {
                        errorLogs.Add($"Satır {row}: Bitiş Tarihi geçerli bir tarih olmalıdır.");
                        continue;
                    }

                    var ownerValue = GetCellText(worksheet.Cell(row, ownerColumn));
                    var ownerMatch = FindRiskOwner(assignableUsers, ownerValue);
                    if (ownerMatch.Ambiguous)
                    {
                        errorLogs.Add($"Satır {row}: '{ownerValue}' birden fazla kullanıcıyla eşleşiyor. Kullanıcı ID veya e-posta kullanın.");
                        continue;
                    }

                    if (ownerMatch.User == null)
                    {
                        errorLogs.Add($"Satır {row}: '{ownerValue}' için atanabilir bir sorumlu kullanıcı bulunamadı.");
                        continue;
                    }

                    var importedAt = DateTime.UtcNow;
                    var mitigation = GetCellText(worksheet.Cell(row, mitigationColumn));
                    var risk = new Risk
                    {
                        RiskId = await IdentifierGenerator.GenerateAsync(db.Risks, item => item.RiskId, "RSK-"),
                        ProjectId = projectId,
                        RiskTitle = riskTitle,
                        RiskCategory = category,
                        RiskProbability = probability,
                        RiskImpact = impact,
                        RiskScore = probability * impact,
                        RiskOwnerUserId = ownerMatch.User.UserId,
                        RiskMitigation = string.IsNullOrWhiteSpace(mitigation) ? "Belirtilmedi" : mitigation,
                        RiskDueDate = dueDate.Date,
                        RiskStatus = riskStatus,
                        OpenedDate = importedAt,
                        ClosedDate = riskStatus == "Kapalı" ? importedAt : null,
                        CreatedByUserId = userId,
                        UpdatedByUserId = userId,
                        CreatedAt = importedAt,
                        UpdatedAt = importedAt
                    };

                    db.Risks.Add(risk);
                    importedCount++;
                }

                if (importedCount > 0)
                    await db.SaveChangesAsync();

                return Results.Ok(new ExcelImportResponse(
                    Success: errorLogs.Count == 0,
                    TotalImported: importedCount,
                    TotalFailed: errorLogs.Count,
                    Errors: errorLogs
                ));
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { message = "Excel dosyası okunamadı. Geçerli bir .xlsx dosyası yükleyin." });
            }
            catch (Exception)
            {
                return Results.Json(
                    new { message = "Riskler Excel dosyasından içe aktarılırken hata oluştu." },
                    statusCode: 500);
            }
        })
        .DisableAntiforgery();
    }

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static Dictionary<string, int> ReadHeaderColumns(IXLWorksheet worksheet)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = NormalizeHeader(GetCellText(worksheet.Cell(1, column)));
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

    private static bool IsEmptyRow(IXLWorksheet worksheet, int row)
    {
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        return lastColumn == 0 ||
               Enumerable.Range(1, lastColumn)
                   .All(column => worksheet.Cell(row, column).IsEmpty());
    }

    private static string? NormalizeRiskStatus(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Açık";

        return value.Trim().ToLower(TurkishCulture) switch
        {
            "açık" or "acik" => "Açık",
            "izleniyor" => "İzleniyor",
            "azaltıldı" or "azaltildi" => "Azaltıldı",
            "kapalı" or "kapali" => "Kapalı",
            _ => null
        };
    }

    private static bool TryReadDate(IXLCell cell, out DateTime date)
    {
        if (cell.TryGetValue<DateTime>(out var dateValue))
        {
            date = dateValue.Date;
            return true;
        }

        if (cell.TryGetValue<double>(out var serialDate))
        {
            try
            {
                date = DateTime.FromOADate(serialDate).Date;
                return true;
            }
            catch (ArgumentException)
            {
                // Fall through to text parsing.
            }
        }

        var text = GetCellText(cell);
        if (DateTime.TryParse(text, TurkishCulture, DateTimeStyles.AllowWhiteSpaces, out date) ||
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
        {
            date = date.Date;
            return true;
        }

        date = default;
        return false;
    }

    private static string GetCellText(IXLCell cell) =>
        cell.GetFormattedString().Trim();

    private static string? ReadCellText(IXLCell cell)
    {
        var value = GetCellText(cell);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static (User? User, bool Ambiguous) FindRiskOwner(
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
