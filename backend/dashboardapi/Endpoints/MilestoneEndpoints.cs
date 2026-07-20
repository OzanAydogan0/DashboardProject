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

            // GÜVENLİK KONTROLÜ
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin kilometre taşlarını görmeye yetkiniz yok!" }, statusCode: 403);
            }

            // Kilometre taşlarını ve atanan kişilerin bilgisini çekiyoruz
            var milestones = await db.Set<Milestone>()
                .Include(m => m.MilestoneOwnerUser)
                .Where(m => m.ProjectId == id)
                // Tarihe göre sıralı gelmesi frontend tarafında Timeline çizmek için harika olur
                .OrderBy(m => m.PlannedDate) 
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

            // GÜVENLİK KONTROLÜ
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye kilometre taşı ekleme yetkiniz yok!" }, statusCode: 403);
            }

            var newMilestone = new Milestone
            {
                MilestoneId = Guid.NewGuid().ToString(),
                ProjectId = request.ProjectId,
                MilestoneName = request.MilestoneName,
                PlannedDate = request.PlannedDate,
                ForecastDate = request.ForecastDate,
                ActualDate = null, // Yeni eklenen bir fazın gerçekleşme tarihi henüz olmaz
                MilestoneStatus = request.MilestoneStatus,
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
    }
}