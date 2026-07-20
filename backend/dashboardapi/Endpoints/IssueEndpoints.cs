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

            var issues = await db.Set<Issue>()
                .Include(i => i.IssueOwnerUser)
                .Where(i => i.ProjectId == id)
                .ToListAsync();

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

            // 🛡️ RAPOR KURALI: Üst Yönetim rolü hiçbir şekilde yeni sorun kaydı açamaz (Salt Okunur)
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme sorun kaydı ekleme yetkisi yoktur!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Admin değilse, projenin PM'i veya ekip üyesi mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye sorun kaydı ekleme yetkiniz yok!" }, statusCode: 403);
            }

            var newIssue = new Issue
            {
                IssueId = "ISS-" + Guid.NewGuid().ToString()[..8].ToUpper(), // Standart ID formatı
                ProjectId = request.ProjectId,
                IssueTitle = request.IssueTitle,
                IssuePriority = request.IssuePriority,
                IssueOwnerUserId = request.IssueOwnerUserId,
                IssueDueDate = request.IssueDueDate,
                IssueStatus = request.IssueStatus ?? "Açık",
                IssueImpact = request.IssueImpact,
                RootCause = request.RootCause,
                IssueResolution = null, 
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

        // 3. PATCH /issues/{id} -> Sorun Güncelleme ve Kapatma Motoru (Eksik Operasyon Eklendi)
        app.MapPatch("issues/{id}", async (string id, UpdateIssueRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var issue = await db.Set<Issue>().FindAsync(id);
            if (issue == null) return Results.NotFound(new { message = "Sorun kaydı bulunamadı." });

            // 🛡️ RAPOR KURALI: Üst Yönetim değişiklik yapamaz
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolü sorunlar üzerinde değişiklik yapamaz!" }, statusCode: 403);

            // GÜVENLİK KONTROLÜ: Yetkili PM veya Admin mi?
            if (userRole != "Sistem Yöneticisi")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == issue.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));

                if (!hasAccess)
                    return Results.Json(new { message = "Bu projenin sorun kaydını güncelleme yetkiniz yok!" }, statusCode: 403);
            }

            // Alanları güvenli şekilde güncelleme
            if (!string.IsNullOrEmpty(request.IssueTitle)) issue.IssueTitle = request.IssueTitle;
            if (!string.IsNullOrEmpty(request.IssuePriority)) issue.IssuePriority = request.IssuePriority;
            if (!string.IsNullOrEmpty(request.IssueOwnerUserId)) issue.IssueOwnerUserId = request.IssueOwnerUserId;
            if (request.IssueDueDate.HasValue) issue.IssueDueDate = request.IssueDueDate.Value;
            if (!string.IsNullOrEmpty(request.IssueImpact)) issue.IssueImpact = request.IssueImpact;
            if (request.RootCause != null) issue.RootCause = request.RootCause;
            if (request.IssueResolution != null) issue.IssueResolution = request.IssueResolution;

            // 🧠 AKILLI KAPATMA MOTORU: Durum kapandıya çekildiyse kapanış tarihini otomatik yönet
            if (!string.IsNullOrEmpty(request.IssueStatus))
            {
                issue.IssueStatus = request.IssueStatus;
                if ((request.IssueStatus.Equals("Kapalı", StringComparison.OrdinalIgnoreCase) || 
                     request.IssueStatus.Equals("Çözüldü", StringComparison.OrdinalIgnoreCase)) && issue.ClosedDate == null)
                {
                    issue.ClosedDate = DateTime.UtcNow;
                }
                else if (!request.IssueStatus.Equals("Kapalı", StringComparison.OrdinalIgnoreCase) && 
                         !request.IssueStatus.Equals("Çözüldü", StringComparison.OrdinalIgnoreCase))
                {
                    issue.ClosedDate = null; // Tekrar açılırsa tarihi temizle
                }
            }

            issue.UpdatedByUserId = userId;
            issue.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Sorun kaydı başarıyla güncellendi." });
        });
    }
}