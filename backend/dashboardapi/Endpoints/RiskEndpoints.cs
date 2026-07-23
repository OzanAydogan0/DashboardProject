using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

namespace dashboardapi.Endpoints;

public static class RiskEndpoints
{
    public static void MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/risks -> Projeye Ait Tüm Riskleri Listeleme (Görünüm Üzerinden)
        app.MapGet("projects/{id}/risks", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projenin risklerini görmeye yetkili mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin risk verilerini görmeye yetkiniz yok!" }, statusCode: 403);
            }

            var rawRisks = await db.Set<VwRisk>()
                .Where(r => r.ProjectId == id)
                .ToListAsync();

            var result = rawRisks.Select(r => new RiskDto(
                r.RiskId,
                r.ProjectId,
                r.ProjectCode,
                r.ProjectName,
                r.RiskTitle,
                r.RiskCategory,
                r.RiskProbability,
                r.RiskImpact,
                r.RiskScore,
                r.RiskStatus,
                r.RiskDueDate,
                r.RiskOwnerUserId,
                r.RiskOwnerFullName,
                ParseString(r.RiskHealth)
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /risks -> Projeye Yeni Risk Ekleme
        app.MapPost("risks", async (CreateRiskRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // 🛡️ RAPOR KURALI: Üst Yönetim rolü hiçbir şekilde veri ekleyemez (Salt Okunur)
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme risk ekleme yetkisi yoktur!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: PM veya ekip üyesi mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye risk ekleme yetkiniz yok!" }, statusCode: 403);
            }

            var newRisk = new Risk
            {
                RiskId = "RSK-" + Guid.NewGuid().ToString()[..8].ToUpper(), // Standart proje ID formatı
                ProjectId = request.ProjectId,
                RiskTitle = request.RiskTitle,
                RiskCategory = request.RiskCategory,
                RiskProbability = request.RiskProbability,
                RiskImpact = request.RiskImpact,
                RiskScore = 0, // DB Trigger'ı bunu otomatik hesaplayacak, harika mantık!
                RiskOwnerUserId = request.RiskOwnerUserId,
                RiskMitigation = request.RiskMitigation,
                RiskDueDate = request.RiskDueDate,
                RiskStatus = request.RiskStatus ?? "Açık",
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
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var risk = await db.Set<Risk>().FindAsync(id);
            if (risk == null) return Results.NotFound(new { message = "Risk kaydı bulunamadı." });

            // 🛡️ RAPOR KURALI: Üst Yönetim güncelleyemez
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolü riskler üzerinde değişiklik yapamaz!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Sistem yöneticisi değilse, riskin ait olduğu projenin PM'i veya ekip üyesi mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == risk.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));

                if (!hasAccess)
                    return Results.Json(new { message = "Bu projenin riskini güncelleme yetkiniz yok!" }, statusCode: 403);
            }

            // Alanları güvenli şekilde güncelle
            if (!string.IsNullOrEmpty(request.RiskTitle)) risk.RiskTitle = request.RiskTitle;
            if (!string.IsNullOrEmpty(request.RiskCategory)) risk.RiskCategory = request.RiskCategory;
            if (request.RiskProbability.HasValue) risk.RiskProbability = request.RiskProbability.Value;
            if (request.RiskImpact.HasValue) risk.RiskImpact = request.RiskImpact.Value;
            if (!string.IsNullOrEmpty(request.RiskStatus)) risk.RiskStatus = request.RiskStatus;
            if (!string.IsNullOrEmpty(request.RiskMitigation)) risk.RiskMitigation = request.RiskMitigation;
            if (request.RiskDueDate.HasValue) risk.RiskDueDate = request.RiskDueDate.Value;
            
            risk.UpdatedByUserId = userId;
            risk.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Risk başarıyla güncellendi." });
        });

        // 4. GET /risks -> Tüm Aktif Projelerin Risklerini Listeleme (Genel Dashboard İçin)
            app.MapGet("risks", async (ClaimsPrincipal userClaims, AppDbContext db) =>
            {
                var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
                var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var query = db.Set<VwRisk>().AsQueryable();

                // GÜVENLİK KONTROLÜ: Yöneticiler her şeyi görür, diğer kullanıcılar sadece üye/PM olduğu projelerin risklerini görür
                if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
                {
                    var accessibleProjectIds = await db.Projects
                        .Where(p => p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId))
                        .Select(p => p.ProjectId)
                        .ToListAsync();

                    query = query.Where(r => accessibleProjectIds.Contains(r.ProjectId));
                }

                var rawRisks = await query.ToListAsync();

                var result = rawRisks.Select(r => new RiskDto(
                    r.RiskId,
                    r.ProjectId,
                    r.ProjectCode,
                    r.ProjectName,
                    r.RiskTitle,
                    r.RiskCategory,
                    r.RiskProbability,
                    r.RiskImpact,
                    r.RiskScore,
                    r.RiskStatus,
                    r.RiskDueDate,
                    r.RiskOwnerUserId,
                    r.RiskOwnerFullName,
                    ParseString(r.RiskHealth)
                )).ToList();

                return Results.Ok(result);
            });
    }

    private static string ParseString(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return "Belirsiz";
        return Encoding.UTF8.GetString(bytes);
    }
}