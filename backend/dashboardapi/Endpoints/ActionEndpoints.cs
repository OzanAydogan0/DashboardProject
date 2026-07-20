using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;

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

            // GÜVENLİK KONTROLÜ
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin aksiyonlarını görmeye yetkiniz yok!" }, statusCode: 403);
            }

            // Aksiyonları ve atanan kişilerin bilgisini çekiyoruz. 
            // DİKKAT: dashboardapi.Models.Action kullanarak sistem sınıfıyla çakışmayı önlüyoruz
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

            // GÜVENLİK KONTROLÜ
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye aksiyon ekleme yetkiniz yok!" }, statusCode: 403);
            }

            var newAction = new dashboardapi.Models.Action
            {
                ActionId = Guid.NewGuid().ToString(),
                ProjectId = request.ProjectId,
                ActionDescription = request.ActionDescription,
                SourceType = request.SourceType,
                SourceReference = request.SourceReference,
                ActionOwnerUserId = request.ActionOwnerUserId,
                ActionDueDate = request.ActionDueDate,
                ActionStatus = request.ActionStatus,
                ActionProgress = request.ActionProgress,
                ActionPriority = request.ActionPriority,
                CompletedDate = request.ActionProgress == 100 ? DateTime.UtcNow : null, // Eğer ilerleme %100 ise otomatik tamamlanma tarihi atıyoruz
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<dashboardapi.Models.Action>().Add(newAction);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Aksiyon başarıyla oluşturuldu.", actionId = newAction.ActionId }, statusCode: 201);
        });
    }
}