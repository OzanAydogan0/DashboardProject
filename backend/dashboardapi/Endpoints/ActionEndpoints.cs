using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

namespace dashboardapi.Endpoints;

public static class ActionEndpoints
{
    public static void MapActionEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/actions -> Projeye Ait Tüm Aksiyonları Listeleme
        app.MapGet("projects/{id}/actions", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projenin aksiyonlarını görmeye yetkili mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin aksiyonlarını görmeye yetkiniz yok!" }, statusCode: 403);
            }

            var actions = await db.Set<dashboardapi.Models.Action>()
                .Include(a => a.ActionOwnerUser)
                .Where(a => a.ProjectId == id)
                .ToListAsync();

            var result = actions.Select(a => new ActionDto(
                a.ActionId,
                a.ProjectId,
                a.ActionDescription,
                a.SourceType,
                a.SourceReference,
                a.ActionOwnerUserId,
                a.ActionOwnerUser?.FullName ?? "Atanmamış",
                a.ActionDueDate,
                a.ActionStatus,
                a.ActionProgress,
                a.ActionPriority,
                a.CompletedDate
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /actions -> Projeye Yeni Aksiyon Ekleme
        app.MapPost("actions", async (CreateActionRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // 🛡️ RAPOR KURALI: Üst Yönetim rolü aksiyon oluşturamaz (Salt Okunur)
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme aksiyon ekleme yetkisi yoktur!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Admin değilse, projenin PM'i veya ekip üyesi mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye aksiyon ekleme yetkiniz yok!" }, statusCode: 403);
            }

            var newAction = new dashboardapi.Models.Action
            {
                ActionId = "ACT-" + Guid.NewGuid().ToString()[..8].ToUpper(), // Standart ID Formatı
                ProjectId = request.ProjectId,
                ActionDescription = request.ActionDescription,
                SourceType = request.SourceType,
                SourceReference = request.SourceReference,
                ActionOwnerUserId = request.ActionOwnerUserId,
                ActionDueDate = request.ActionDueDate,
                ActionStatus = request.ActionStatus ?? "Açık",
                ActionProgress = request.ActionProgress,
                ActionPriority = request.ActionPriority,
                CompletedDate = request.ActionProgress == 100m ? DateTime.UtcNow : null, 
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<dashboardapi.Models.Action>().Add(newAction);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Aksiyon başarıyla oluşturuldu.", actionId = newAction.ActionId }, statusCode: 201);
        });

        // 3. PATCH /actions/{id} -> Aksiyon Güncelleme ve Otomatik Tamamlanma Motoru (Eksik Operasyon Eklendi)
        app.MapPatch("actions/{id}", async (string id, UpdateActionRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var action = await db.Set<dashboardapi.Models.Action>().FindAsync(id);
            if (action == null) return Results.NotFound(new { message = "Aksiyon kaydı bulunamadı." });

            // 🛡️ RAPOR KURALI: Üst Yönetim değişiklik yapamaz
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolü aksiyonlar üzerinde değişiklik yapamaz!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Yetkili PM veya Admin mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == action.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));

                if (!hasAccess)
                    return Results.Json(new { message = "Bu projenin aksiyonunu güncelleme yetkiniz yok!" }, statusCode: 403);
            }

            // Alanları güvenli şekilde güncelleme
            if (!string.IsNullOrEmpty(request.ActionDescription)) action.ActionDescription = request.ActionDescription;
            if (!string.IsNullOrEmpty(request.SourceType)) action.SourceType = request.SourceType;
            if (request.SourceReference != null) action.SourceReference = request.SourceReference;
            if (!string.IsNullOrEmpty(request.ActionOwnerUserId)) action.ActionOwnerUserId = request.ActionOwnerUserId;
            if (request.ActionDueDate.HasValue) action.ActionDueDate = request.ActionDueDate.Value;
            if (!string.IsNullOrEmpty(request.ActionPriority)) action.ActionPriority = request.ActionPriority;

            if (request.ActionProgress.HasValue) action.ActionProgress = request.ActionProgress.Value;
            if (!string.IsNullOrEmpty(request.ActionStatus)) action.ActionStatus = request.ActionStatus;

            // 🧠 AKILLI TAMAMLANMA MOTORU: Yüzde 100 ise veya durum Tamamlandı ise tarihi otomatik yönet
            if (action.ActionProgress == 100m || 
                (!string.IsNullOrEmpty(action.ActionStatus) && action.ActionStatus.Equals("Tamamlandı", StringComparison.OrdinalIgnoreCase)))
            {
                if (action.CompletedDate == null) action.CompletedDate = DateTime.UtcNow;
            }
            else
            {
                action.CompletedDate = null; // Aksiyon geri açılırsa tarihi temizle
            }

            action.UpdatedByUserId = userId;
            action.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Aksiyon başarıyla güncellendi." });
        });
        // 4. GET /actions -> Tüm Projelerin Aksiyonlarını Listeleme (Genel Dashboard İçin)
        app.MapGet("actions", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var query = db.Set<dashboardapi.Models.Action>()
                .Include(a => a.ActionOwnerUser)
                .AsQueryable();

            // GÜVENLİK KONTROLÜ: Admin ve Üst Yönetim hepsini görür, diğerleri sadece kendi projelerindekini görür
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var accessibleProjectIds = await db.Projects
                    .Where(p => p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId))
                    .Select(p => p.ProjectId)
                    .ToListAsync();

                query = query.Where(a => accessibleProjectIds.Contains(a.ProjectId));
            }

            var actions = await query.ToListAsync();

            var result = actions.Select(a => new ActionDto(
                a.ActionId,
                a.ProjectId,
                a.ActionDescription,
                a.SourceType,
                a.SourceReference,
                a.ActionOwnerUserId,
                a.ActionOwnerUser?.FullName ?? "Atanmamış",
                a.ActionDueDate,
                a.ActionStatus,
                a.ActionProgress,
                a.ActionPriority,
                a.CompletedDate
            )).ToList();

            return Results.Ok(result);
        });
    }
}