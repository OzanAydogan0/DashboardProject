using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class GovernanceEndpoints
{
    public static void MapGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        // ==========================================
        // 1. YÖNETİM KARARLARI UÇ NOKTALARI
        // ==========================================

        app.MapGet("projects/{id}/decisions", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Yetkiniz yok!" }, statusCode: 403);

            var decisions = await db.Set<ManagementDecision>()
                .Include(d => d.DecisionOwnerUser)
                .Where(d => d.ProjectId == id)
                .OrderByDescending(d => d.DecisionDate)
                .ToListAsync();

            var result = decisions.Select(d => new ManagementDecisionDto(
                d.ManagementDecisionId, d.ProjectId, d.DecisionTitle, d.Decision, d.DecisionOwnerUserId,
                d.DecisionOwnerUser?.FullName ?? "Atanmamış", d.DecisionDueDate, d.DecisionStatus,
                d.DecisionImpact, d.IfDelayed, d.Recommendation, d.DecisionDate
            )).ToList();

            return Results.Ok(result);
        });

        app.MapPost("decisions", async (CreateManagementDecisionRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü karar ekleyemez!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, request.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye karar ekleme yetkiniz yok!" }, statusCode: 403);

            var newDecision = new ManagementDecision
            {
                ManagementDecisionId = await IdentifierGenerator.GenerateAsync(db.Set<ManagementDecision>(), d => d.ManagementDecisionId, "DEC-"),
                ProjectId = request.ProjectId,
                DecisionTitle = request.DecisionTitle,
                Decision = request.Decision,
                DecisionOwnerUserId = request.DecisionOwnerUserId,
                DecisionDueDate = request.DecisionDueDate,
                DecisionStatus = request.DecisionStatus,
                DecisionImpact = request.DecisionImpact,
                IfDelayed = request.IfDelayed,
                Recommendation = request.Recommendation,
                DecisionDate = request.DecisionDate,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<ManagementDecision>().Add(newDecision);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Karar kaydedildi.", decisionId = newDecision.ManagementDecisionId }, statusCode: 201);
        });

        // ==========================================
        // 2. PIR (KAPANIŞ RAPORLARI) UÇ NOKTALARI
        // ==========================================

        app.MapGet("projects/{id}/pirs", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Yetkiniz yok!" }, statusCode: 403);

            var pirs = await db.Set<VwPir>()
                .Where(p => p.ProjectId == id)
                .OrderByDescending(p => p.ReportDate)
                .ToListAsync();

            var result = pirs.Select(p => new PirReportDto(
                p.PirReportId, p.ProjectId, p.ProjectCode, p.ProjectName, p.Period,
                p.ReportDate, p.ExecutiveSummary, p.CompletedWork, p.Delays,
                p.NextPeriodPlan, p.ManagementExpectations, HealthStatusHelper.Normalize(p.ManualHealth),
                p.ReportStatus, p.PublishedAt
            )).ToList();

            return Results.Ok(result);
        });

        app.MapPost("pirs", async (CreatePirReportRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü PIR raporu ekleyemez!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, request.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye PIR ekleme yetkiniz yok!" }, statusCode: 403);

            var newPir = new PirReport
            {
                PirReportId = await IdentifierGenerator.GenerateAsync(db.Set<PirReport>(), p => p.PirReportId, "PIR-"),
                ProjectId = request.ProjectId,
                Period = request.Period,
                ReportDate = request.ReportDate,
                ExecutiveSummary = request.ExecutiveSummary,
                CompletedWork = request.CompletedWork,
                Delays = request.Delays,
                NextPeriodPlan = request.NextPeriodPlan,
                ManagementExpectations = request.ManagementExpectations,
                ManualHealth = HealthStatusHelper.ToStorageValue(request.ManualHealth),
                ReportStatus = request.ReportStatus,
                PublishedByUserId = request.ReportStatus == "Yayımlandı" ? userId : null,
                PublishedAt = request.ReportStatus == "Yayımlandı" ? DateTime.UtcNow : null,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<PirReport>().Add(newPir);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "PIR Raporu oluşturuldu.", pirId = newPir.PirReportId }, statusCode: 201);
        });

        app.MapPatch("pirs/{id}", async (string id, UpdatePirReportRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü PIR raporu düzenleyemez!" }, statusCode: 403);

            var existingPir = await db.PirReports.FirstOrDefaultAsync(p => p.PirReportId == id);
            if (existingPir == null)
                return Results.NotFound(new { message = "PIR raporu bulunamadı." });

            if (!await PermissionHelper.CanWriteProjectAsync(db, existingPir.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye PIR düzenleme yetkiniz yok!" }, statusCode: 403);

            if (request.Period is not null) existingPir.Period = request.Period;
            if (request.ReportDate.HasValue) existingPir.ReportDate = request.ReportDate.Value;
            if (request.ExecutiveSummary is not null) existingPir.ExecutiveSummary = request.ExecutiveSummary;
            if (request.CompletedWork is not null) existingPir.CompletedWork = request.CompletedWork;
            if (request.Delays is not null) existingPir.Delays = request.Delays;
            if (request.NextPeriodPlan is not null) existingPir.NextPeriodPlan = request.NextPeriodPlan;
            if (request.ManagementExpectations is not null) existingPir.ManagementExpectations = request.ManagementExpectations;
            if (request.ManualHealth is not null)
                existingPir.ManualHealth = HealthStatusHelper.ToStorageValue(request.ManualHealth);
            if (request.ReportStatus is not null) existingPir.ReportStatus = request.ReportStatus;

            if (request.ReportStatus == "Yayımlandı")
            {
                existingPir.PublishedByUserId = userId;
                existingPir.PublishedAt = DateTime.UtcNow;
            }
            else if (request.ReportStatus is not null && request.ReportStatus != "Yayımlandı")
            {
                existingPir.PublishedByUserId = null;
                existingPir.PublishedAt = null;
            }

            existingPir.UpdatedByUserId = userId;
            existingPir.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "PIR raporu güncellendi." });
        });

        app.MapDelete("pirs/{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü PIR raporu silemez!" }, statusCode: 403);

            var existingPir = await db.PirReports.FirstOrDefaultAsync(p => p.PirReportId == id);
            if (existingPir == null)
                return Results.NotFound(new { message = "PIR raporu bulunamadı." });

            if (!await PermissionHelper.CanWriteProjectAsync(db, existingPir.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye PIR silme yetkiniz yok!" }, statusCode: 403);

            db.PirReports.Remove(existingPir);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "PIR raporu silindi." });
        });

        // 5. PIR Raporunu PDF olarak dışa aktarma
        app.MapGet("pirs/{id}/export/pdf", async (string id, AppDbContext db) =>
{
    // 1. PIR Raporunu View üzerinden çek
    var reportData = await db.Set<VwPir>().FirstOrDefaultAsync(p => p.PirReportId == id);
    if (reportData == null) 
        return Results.NotFound(new { message = "Rapor bulunamadı." });

    reportData.ManualHealth = HealthStatusHelper.Normalize(reportData.ManualHealth);

    // 2. İlişkili Proje verilerini (Bütçe ve İlerleme) çek
    var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == reportData.ProjectId);

    // 3. Varsa logo dosyasını oku (Örn: wwwroot/images/logo.png içinden)
    byte[]? logoBytes = null;
    string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
    if (File.Exists(logoPath))
    {
        logoBytes = await File.ReadAllBytesAsync(logoPath);
    }

    // 4. PDF Veri Paketini Hazırla
    var pdfData = new Services.PirPdfData(
        Report: reportData,
        Bac: project?.Bac ?? 0,
        PlannedProgress: project?.PlannedProgress ?? 0,
        ActualProgress: project?.ActualProgress ?? 0,
        Cpi: 1.00m, // Varsa EVM tablosundan anlık değer çekilebilir
        Spi: 1.00m,
        Currency: project?.Currency ?? "TRY",
        LogoBytes: logoBytes
    );

    // 5. PDF'i Üret ve Fırlat
    byte[] pdfBytes = Services.PirPdfGenerator.Generate(pdfData);
    string fileName = $"{reportData.ProjectCode ?? "PRJ"}_PIR_{reportData.Period}.pdf";

    return Results.File(pdfBytes, "application/pdf", fileName);
}).RequireAuthorization();

// 6. PIR Raporunu Excel olarak dışa aktarma
        app.MapGet("pirs/{id}/export/excel", async (string id, AppDbContext db) =>
{
    var reportData = await db.Set<VwPir>().FirstOrDefaultAsync(p => p.PirReportId == id);
    if (reportData == null) 
        return Results.NotFound(new { message = "Rapor bulunamadı." });

    reportData.ManualHealth = HealthStatusHelper.Normalize(reportData.ManualHealth);

    var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == reportData.ProjectId);

    // Aynı veri yapısını kullanıyoruz
    var excelData = new Services.PirPdfData(
        Report: reportData,
        Bac: project?.Bac ?? 0,
        PlannedProgress: project?.PlannedProgress ?? 0,
        ActualProgress: project?.ActualProgress ?? 0,
        Cpi: 1.00m,
        Spi: 1.00m,
        Currency: project?.Currency ?? "TRY"
    );

    byte[] excelBytes = dashboardapi.Services.PirExcelGenerator.Generate(excelData);
    string fileName = $"{reportData.ProjectCode ?? "PRJ"}_PIR_{reportData.Period}.xlsx";

    return Results.File(
        fileContents: excelBytes, 
        contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
        fileDownloadName: fileName
    );
}).RequireAuthorization();
    }
}
