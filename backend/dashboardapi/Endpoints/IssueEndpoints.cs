using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

namespace dashboardapi.Endpoints;

public static class IssueEndpoints
{
    public static void MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/issues -> Projeye Ait Tüm Aktif/Kapalı Sorunları Listeleme
        app.MapGet("projects/{id}/issues", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projenin sorunlarını görmeye yetkili mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin sorun (issue) verilerini görmeye yetkiniz yok!" }, statusCode: 403);
            }

            // Sorunları çekerken sahiplerinin (Owner) bilgilerini de arkadan bağlıyoruz (Eager Loading)
            var issues = await db.Set<Issue>()
                .Include(i => i.IssueOwnerUser)
                .Where(i => i.ProjectId == id)
                .ToListAsync();

            // Modeli temiz bir şekilde DTO'ya mapliyoruz
            var result = issues.Select(i => new IssueDto(
                i.IssueId,
                i.ProjectId,
                i.IssueTitle,
                i.IssuePriority,
                i.IssueOwnerUserId,
                i.IssueOwnerUser?.FullName ?? "Atanmamış",
                i.IssueDueDate,
                i.IssueStatus,
                i.IssueImpact,
                i.RootCause,
                i.IssueResolution,
                i.OpenedDate,
                i.ClosedDate
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /issues -> Projede Yeni Bir Sorun (Issue) Bildirme
        app.MapPost("issues", async (CreateIssueRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projeye müdahale edebilir mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye sorun kaydı ekleme yetkiniz yok!" }, statusCode: 403);
            }

            // Yeni Issue nesnesini hazırlıyoruz
            var newIssue = new Issue
            {
                IssueId = Guid.NewGuid().ToString(),
                ProjectId = request.ProjectId,
                IssueTitle = request.IssueTitle,
                IssuePriority = request.IssuePriority,
                IssueOwnerUserId = request.IssueOwnerUserId,
                IssueDueDate = request.IssueDueDate,
                IssueStatus = request.IssueStatus,
                IssueImpact = request.IssueImpact,
                RootCause = request.RootCause,
                IssueResolution = null, // Yeni açılan sorunun çözümü henüz olmaz
                OpenedDate = DateTime.UtcNow,
                ClosedDate = null,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Issue>().Add(newIssue);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Sorun başarıyla kaydedildi.", issueId = newIssue.IssueId }, statusCode: 201);
        });
    }
}