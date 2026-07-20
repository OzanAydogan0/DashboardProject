using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

namespace dashboardapi.Endpoints;

public static class MilestoneEndpoints
{
    public static void MapMilestoneEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/milestones -> Projeye Ait Tüm Kilometre Taşlarını Listeleme
        app.MapGet("projects/{id}/milestones", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projenin kilometre taşlarını görmeye yetkili mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin kilometre taşlarını görmeye yetkiniz yok!" }, statusCode: 403);
            }

            var milestones = await db.Set<Milestone>()
                .Include(m => m.MilestoneOwnerUser)
                .Where(m => m.ProjectId == id)
                .OrderBy(m => m.PlannedDate) // Timeline görünümü için kronolojik sıralama
                .ToListAsync();

            var result = milestones.Select(m => new MilestoneDto(
                m.MilestoneId,
                m.ProjectId,
                m.MilestoneName,
                m.PlannedDate,
                m.ForecastDate,
                m.ActualDate,
                m.MilestoneStatus,
                m.Critical,
                m.MilestoneOwnerUserId,
                m.MilestoneOwnerUser?.FullName ?? "Atanmamış",
                m.AcceptanceCriteria,
                m.MilestoneDescription
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /milestones -> Projeye Yeni Kilometre Taşı Ekleme
        app.MapPost("milestones", async (CreateMilestoneRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // 🛡️ RAPOR KURALI: Üst Yönetim rolü veri ekleyemez (Salt Okunur yetki bariyeri)
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme kilometre taşı ekleme yetkisi yoktur!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Admin değilse, projenin PM'i veya ekip üyesi mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye kilometre taşı ekleme yetkiniz yok!" }, statusCode: 403);
            }

            var newMilestone = new Milestone
            {
                MilestoneId = "MS-" + Guid.NewGuid().ToString()[..8].ToUpper(), // Standart ID formatı
                ProjectId = request.ProjectId,
                MilestoneName = request.MilestoneName,
                PlannedDate = request.PlannedDate,
                ForecastDate = request.ForecastDate,
                ActualDate = null, 
                MilestoneStatus = request.MilestoneStatus ?? "Planlandı",
                Critical = request.Critical,
                MilestoneOwnerUserId = request.MilestoneOwnerUserId,
                AcceptanceCriteria = request.AcceptanceCriteria,
                MilestoneDescription = request.MilestoneDescription,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Milestone>().Add(newMilestone);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Kilometre taşı başarıyla oluşturuldu.", milestoneId = newMilestone.MilestoneId }, statusCode: 201);
        });

        // 3. PATCH /milestones/{id} -> Kilometre Taşı Güncelleme (Eksik Operasyon Eklendi)
        app.MapPatch("milestones/{id}", async (string id, UpdateMilestoneRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var milestone = await db.Set<Milestone>().FindAsync(id);
            if (milestone == null) return Results.NotFound(new { message = "Kilometre taşı bulunamadı." });

            // 🛡️ RAPOR KURALI: Üst Yönetim değişiklik yapamaz
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolü kilometre taşları üzerinde değişiklik yapamaz!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Yetkili PM veya Admin mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));

                if (!hasAccess)
                    return Results.Json(new { message = "Bu projenin kilometre taşını güncelleme yetkiniz yok!" }, statusCode: 403);
            }

            // Alanları güvenli şekilde güncelleme havuzu
            if (!string.IsNullOrEmpty(request.MilestoneName)) milestone.MilestoneName = request.MilestoneName;
            if (request.PlannedDate.HasValue) milestone.PlannedDate = request.PlannedDate.Value;
            if (request.ForecastDate.HasValue) milestone.ForecastDate = request.ForecastDate.Value;
            if (request.ActualDate.HasValue) milestone.ActualDate = request.ActualDate.Value;
            if (!string.IsNullOrEmpty(request.MilestoneStatus)) milestone.MilestoneStatus = request.MilestoneStatus;
            if (request.Critical.HasValue) milestone.Critical = request.Critical.Value;
            if (!string.IsNullOrEmpty(request.MilestoneOwnerUserId)) milestone.MilestoneOwnerUserId = request.MilestoneOwnerUserId;
            if (!string.IsNullOrEmpty(request.AcceptanceCriteria)) milestone.AcceptanceCriteria = request.AcceptanceCriteria;
            if (request.MilestoneDescription != null) milestone.MilestoneDescription = request.MilestoneDescription;

            milestone.UpdatedByUserId = userId;
            milestone.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Kilometre taşı başarıyla güncellendi." });
        });
    }
}