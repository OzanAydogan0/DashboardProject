using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

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
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // Sadece Yönetici/Üst Yönetim veya projeye dahil olanlar görebilir
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) return Results.Json(new { message = "Yetkiniz yok!" }, statusCode: 403);
            }

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
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var newDecision = new ManagementDecision
            {
                ManagementDecisionId = Guid.NewGuid().ToString(),
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
            // View üzerinden okuma yapıyoruz (Hızlı ve Join gerektirmez)
            var pirs = await db.Set<VwPir>()
                .Where(p => p.ProjectId == id)
                .OrderByDescending(p => p.ReportDate)
                .ToListAsync();

            var result = pirs.Select(p => new PirReportDto(
                p.PirReportId, p.ProjectId, p.ProjectCode, p.ProjectName, p.Period,
                p.ReportDate, p.ExecutiveSummary, p.CompletedWork, p.Delays,
                p.NextPeriodPlan, p.ManagementExpectations, p.ManualHealth, p.ReportStatus, p.PublishedAt
            )).ToList();

            return Results.Ok(result);
        });

        app.MapPost("pirs", async (CreatePirReportRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var newPir = new PirReport
            {
                PirReportId = Guid.NewGuid().ToString(),
                ProjectId = request.ProjectId,
                Period = request.Period,
                ReportDate = request.ReportDate,
                ExecutiveSummary = request.ExecutiveSummary,
                CompletedWork = request.CompletedWork,
                Delays = request.Delays,
                NextPeriodPlan = request.NextPeriodPlan,
                ManagementExpectations = request.ManagementExpectations,
                ManualHealth = request.ManualHealth,
                ReportStatus = request.ReportStatus,
                PublishedByUserId = request.ReportStatus == "Yayınlandı" ? userId : null,
                PublishedAt = request.ReportStatus == "Yayınlandı" ? DateTime.UtcNow : null,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<PirReport>().Add(newPir);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "PIR Raporu oluşturuldu.", pirId = newPir.PirReportId }, statusCode: 201);
        });
    }
}