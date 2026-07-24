using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using dashboardapi.Data;
using dashboardapi.Models;
using dashboardapi.DTOs;

namespace dashboardapi.Endpoints;

public static class ExcelImportEndpoints
{
    public static void MapExcelImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("projects/import", async (
            [FromForm] IFormFile file, // 👈 1. [FromForm] özniteliği eklendi (415 hatasını çözer)
            ClaimsPrincipal userClaims, 
            AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (userRole != "Sistem Yöneticisi")
                return Results.Json(new { message = "Bu operasyon için Sistem Yöneticisi yetkiniz olmalıdır!" }, statusCode: 403);

            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "Lütfen geçerli bir Excel dosyası yükleyin." });

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var importedCount = 0;
            var errorLogs = new List<string>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null) return Results.BadRequest(new { message = "Çalışma sayfası bulunamadı." });

                var rowCount = worksheet.Dimension?.End.Row ?? 0;
                if (rowCount < 2) return Results.BadRequest(new { message = "İşlenecek veri bulunamadı." });

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var projectCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var projectName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var programName = worksheet.Cells[row, 3].Value?.ToString()?.Trim(); 
                        var customerName = worksheet.Cells[row, 4].Value?.ToString()?.Trim(); 
                        var pmIdentifier = worksheet.Cells[row, 5].Value?.ToString()?.Trim(); // PM ID veya E-posta
                        var startDateStr = worksheet.Cells[row, 6].Value?.ToString();
                        var endDateStr = worksheet.Cells[row, 7].Value?.ToString();
                        var bacStr = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                        var currency = worksheet.Cells[row, 9].Value?.ToString()?.Trim() ?? "TRY";
                        var confidentiality = worksheet.Cells[row, 10].Value?.ToString()?.Trim() ?? "Şirket İçi";
                        var reportingFreq = worksheet.Cells[row, 11].Value?.ToString()?.Trim() ?? "Aylık";
                        var status = worksheet.Cells[row, 12].Value?.ToString()?.Trim() ?? "Planlandı";

                        if (string.IsNullOrEmpty(projectCode) || string.IsNullOrEmpty(projectName) || 
                            string.IsNullOrEmpty(pmIdentifier) || string.IsNullOrEmpty(programName) || string.IsNullOrEmpty(customerName))
                        {
                            errorLogs.Add($"Satır {row}: Proje Kodu, Adı, Program Adı, Müşteri Adı ve PM ID/E-posta alanları zorunludur.");
                            continue;
                        }

                        // 🔍 Validasyon 1: Mükerrer Proje Kontrolü
                        if (await db.Projects.AnyAsync(p => p.ProjectCode == projectCode))
                        {
                            errorLogs.Add($"Satır {row}: '{projectCode}' kodlu proje zaten mevcut.");
                            continue;
                        }

                        // 🧠 AKILLI EŞLEŞTİRME & OTOMATİK OLUŞTURMA (Get or Create)
                        
                        // 1. Programı Bul veya Otomatik Oluştur
                        var program = db.Programs.Local.FirstOrDefault(p => p.ProgramName == programName) 
                                      ?? await db.Programs.FirstOrDefaultAsync(p => p.ProgramName == programName);

                        if (program == null)
                        {
                            program = new dashboardapi.Models.Program
                            {
                                ProgramId = Guid.NewGuid().ToString(),
                                ProgramName = programName,
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
                                CustomerId = Guid.NewGuid().ToString(),
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

                        // Projeyi Oluştur
                        var newProject = new Project
                        {
                            ProjectId = "PRJ-" + Guid.NewGuid().ToString()[..8].ToUpper(),
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
                            ManualHealth = "Yeşil",
                            PlannedProgress = 0m,
                            ActualProgress = 0m,
                            Bac = bacValue,
                            Currency = currency,
                            ReportingFrequency = reportingFreq,
                            Confidentiality = confidentiality,
                            ProjectDescription = null,
                            IsActive = 1,
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
                        errorLogs.Add($"Satır {row}: Beklenmeyen bir hata oluştu -> {ex.Message}");
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
    }
}