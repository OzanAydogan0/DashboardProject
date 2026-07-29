using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class RiskEndpoints
{
    public static void MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/risks -> Projeye Ait Tüm Riskleri Listeleme (Görünüm Üzerinden)
        app.MapGet("projects/{id}/risks", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Bu projenin risk verilerini görmeye yetkiniz yok!" }, statusCode: 403);

            var rawRisks = await db.Risks
                .Where(r => r.ProjectId == id)
                .Include(r => r.Project)
                .Include(r => r.RiskOwnerUser)
                .ToListAsync();

            var result = rawRisks.Select(MapRiskToDto).ToList();

            return Results.Ok(result);
        });

        // 2. POST /risks -> Projeye Yeni Risk Ekleme
        app.MapPost("risks", async (CreateRiskRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme risk ekleme yetkisi yoktur!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, request.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye risk ekleme yetkiniz yok!" }, statusCode: 403);

            var ownerUserId = string.IsNullOrWhiteSpace(request.RiskOwnerUserId)
                ? userId
                : request.RiskOwnerUserId;

            var mitigationText = string.IsNullOrWhiteSpace(request.RiskMitigation)
                ? "Belirtilmedi"
                : request.RiskMitigation;

            var dueDate = request.RiskDueDate ?? DateTime.UtcNow;

            var newRisk = new Risk
            {
                RiskId = await IdentifierGenerator.GenerateAsync(db.Set<Risk>(), r => r.RiskId, "RSK-"),
                ProjectId = request.ProjectId,
                RiskTitle = request.RiskTitle,
                RiskCategory = request.RiskCategory,
                RiskProbability = request.RiskProbability,
                RiskImpact = request.RiskImpact,
                RiskScore = 0,
                RiskOwnerUserId = ownerUserId,
                RiskMitigation = mitigationText,
                RiskDueDate = dueDate,
                RiskStatus = string.IsNullOrWhiteSpace(request.RiskStatus) ? "Açık" : request.RiskStatus,
                OpenedDate = DateTime.UtcNow,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Risk>().Add(newRisk);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Risk başarıyla tanımlandı. Skor veritabanı tarafından hesaplanacak.", riskId = newRisk.RiskId }, statusCode: 201);
        });

        // 3. PATCH /risks/{id} -> Risk Güncelleme (Eksik Operasyon Tamamlandı)
        app.MapPatch("risks/{id}", async (string id, UpdateRiskRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var risk = await db.Set<Risk>().FindAsync(id);
            if (risk == null) return Results.NotFound(new { message = "Risk kaydı bulunamadı." });

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü riskler üzerinde değişiklik yapamaz!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, risk.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projenin riskini güncelleme yetkiniz yok!" }, statusCode: 403);

            // Alanları güvenli şekilde güncelle
            if (!string.IsNullOrEmpty(request.RiskTitle)) risk.RiskTitle = request.RiskTitle;
            if (!string.IsNullOrEmpty(request.RiskCategory)) risk.RiskCategory = request.RiskCategory;
            if (request.RiskProbability.HasValue) risk.RiskProbability = request.RiskProbability.Value;
            if (request.RiskImpact.HasValue) risk.RiskImpact = request.RiskImpact.Value;
            if (!string.IsNullOrEmpty(request.RiskStatus))
            {
                risk.RiskStatus = request.RiskStatus;
                if (string.Equals(request.RiskStatus, "Kapalı", StringComparison.OrdinalIgnoreCase))
                    risk.ClosedDate ??= DateTime.UtcNow;
                else
                    risk.ClosedDate = null;
            }
            if (!string.IsNullOrEmpty(request.RiskMitigation)) risk.RiskMitigation = request.RiskMitigation;
            if (request.RiskDueDate.HasValue) risk.RiskDueDate = request.RiskDueDate.Value;
            if (!string.IsNullOrWhiteSpace(request.RiskOwnerUserId)) risk.RiskOwnerUserId = request.RiskOwnerUserId;

            if (string.IsNullOrWhiteSpace(risk.RiskTitle)) risk.RiskTitle = "Başlık Belirtilmedi";
            if (string.IsNullOrWhiteSpace(risk.RiskCategory)) risk.RiskCategory = "Genel";
            if (string.IsNullOrWhiteSpace(risk.RiskStatus)) risk.RiskStatus = "Açık";
            if (string.IsNullOrWhiteSpace(risk.RiskMitigation)) risk.RiskMitigation = "Belirtilmedi";
            if (string.IsNullOrWhiteSpace(risk.RiskOwnerUserId)) risk.RiskOwnerUserId = userId;
            if (risk.RiskDueDate == default) risk.RiskDueDate = DateTime.UtcNow;
            
            risk.UpdatedByUserId = userId;
            risk.UpdatedAt = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Risk başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return Results.Json(new { message = "Risk durumu güncellenemedi. Lütfen tekrar deneyin.", detail = ex.Message }, statusCode: 500);
            }
        });

        // 4. DELETE /risks/{id} -> Risk Silme
        app.MapDelete("risks/{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var risk = await db.Set<Risk>().FindAsync(id);
            if (risk == null) return Results.NotFound(new { message = "Risk kaydı bulunamadı." });

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü riskleri silemez!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, risk.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projenin riskini silme yetkiniz yok!" }, statusCode: 403);

            db.Set<Risk>().Remove(risk);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Risk başarıyla silindi." });
        });

        // 5. GET /risks -> Tüm Aktif Projelerin Risklerini Listeleme (Genel Dashboard İçin)
            app.MapGet("risks", async (ClaimsPrincipal userClaims, AppDbContext db) =>
            {
                var userRole = PermissionHelper.GetUserRole(userClaims);
                var userId = PermissionHelper.GetUserId(userClaims);

                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var activeProjectIds = await db.Projects
                .Where(p => p.IsActive == 1)
                .Select(p => p.ProjectId)
                .ToListAsync();

            var query = db.Risks
                .Where(r => activeProjectIds.Contains(r.ProjectId))
                .Include(r => r.Project)
                .Include(r => r.RiskOwnerUser)
                .AsQueryable();

            if (!PermissionHelper.IsSystemAdmin(userRole) && !PermissionHelper.IsExecutive(userRole))
            {
                var accessibleProjectIds = await db.Projects
                    .Where(p => p.IsActive == 1 && (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)))
                    .Select(p => p.ProjectId)
                    .ToListAsync();

                query = query.Where(r => accessibleProjectIds.Contains(r.ProjectId));
            }

            var rawRisks = await query.ToListAsync();

            var result = rawRisks.Select(MapRiskToDto).ToList();

                return Results.Ok(result);
            });
    }

    private static RiskDto MapRiskToDto(Risk risk)
    {
        var riskHealth = risk.RiskScore switch
        {
            <= 4 => "Yeşil",
            <= 15 => "Sarı",
            _ => "Kırmızı"
        };

        return new RiskDto(
            risk.RiskId,
            risk.ProjectId,
            risk.Project?.ProjectCode,
            risk.Project?.ProjectName,
            risk.RiskTitle,
            risk.RiskCategory,
            risk.RiskProbability,
            risk.RiskImpact,
            risk.RiskScore,
            risk.RiskStatus,
            risk.RiskDueDate,
            risk.RiskOwnerUserId,
            risk.RiskOwnerUser?.FullName,
            risk.RiskMitigation,
            riskHealth
        );
    }
}
