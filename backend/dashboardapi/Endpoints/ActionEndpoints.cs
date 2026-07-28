using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class ActionEndpoints
{
    public static void MapActionEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/actions -> Projeye Ait Tüm Aksiyonları Listeleme
        app.MapGet("projects/{id}/actions", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Bu projenin aksiyonlarını görmeye yetkiniz yok!" }, statusCode: 403);

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
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme aksiyon ekleme yetkisi yoktur!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, request.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye aksiyon ekleme yetkiniz yok!" }, statusCode: 403);

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
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var action = await db.Set<dashboardapi.Models.Action>().FindAsync(id);
            if (action == null) return Results.NotFound(new { message = "Aksiyon kaydı bulunamadı." });

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü aksiyonlar üzerinde değişiklik yapamaz!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, action.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projenin aksiyonunu güncelleme yetkiniz yok!" }, statusCode: 403);

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

            var activeProjectIds = await db.Projects
                .Where(p => p.IsActive == 1)
                .Select(p => p.ProjectId)
                .ToListAsync();

            var query = db.Set<dashboardapi.Models.Action>()
                .Include(a => a.ActionOwnerUser)
                .Where(a => activeProjectIds.Contains(a.ProjectId))
                .AsQueryable();

            if (!PermissionHelper.IsSystemAdmin(userRole) && !PermissionHelper.IsExecutive(userRole))
            {
                var accessibleProjectIds = await db.Projects
                    .Where(p => p.IsActive == 1 && (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)))
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