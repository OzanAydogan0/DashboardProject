using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class IssueEndpoints
{
    private static readonly HashSet<string> AllowedPriorities = ["Düşük", "Orta", "Yüksek", "Kritik"];
    private static readonly HashSet<string> AllowedStatuses = ["Açık", "Devam Ediyor", "Çözüldü", "Kapalı"];
    private static readonly HashSet<string> AllowedImpacts = ["Düşük", "Orta", "Yüksek", "Kritik"];

    public static void MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/issues -> Projeye Ait Tüm Aktif/Kapalı Sorunları Listeleme
        app.MapGet("projects/{id}/issues", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Bu projenin sorun (issue) verilerini görmeye yetkiniz yok!" }, statusCode: 403);

            var issues = await db.Set<Issue>()
                .Include(i => i.IssueOwnerUser)
                .Include(i => i.Risk)
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
                i.ClosedDate,
                i.RiskId,
                i.Risk?.RiskTitle
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /issues -> Projede Yeni Bir Sorun (Issue) Bildirme
        app.MapPost("issues", async (CreateIssueRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme sorun kaydı ekleme yetkisi yoktur!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, request.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye sorun kaydı ekleme yetkiniz yok!" }, statusCode: 403);

            var issueTitle = request.IssueTitle?.Trim();
            var issuePriority = request.IssuePriority?.Trim();
            var issueOwnerUserId = request.IssueOwnerUserId?.Trim();
            var issueStatus = request.IssueStatus?.Trim();
            var issueImpact = request.IssueImpact?.Trim();

            if (string.IsNullOrWhiteSpace(issueTitle))
                return Results.BadRequest(new { message = "Sorun tanımı zorunludur." });

            if (string.IsNullOrWhiteSpace(issuePriority) || !AllowedPriorities.Contains(issuePriority))
                return Results.BadRequest(new { message = "Geçerli bir sorun önceliği seçiniz." });

            if (string.IsNullOrWhiteSpace(issueStatus) || !AllowedStatuses.Contains(issueStatus))
                return Results.BadRequest(new { message = "Geçerli bir sorun durumu seçiniz." });

            if (string.IsNullOrWhiteSpace(issueImpact) || !AllowedImpacts.Contains(issueImpact))
                return Results.BadRequest(new { message = "Geçerli bir sorun etkisi seçiniz." });

            if (request.IssueDueDate == default)
                return Results.BadRequest(new { message = "Hedef tarihi zorunludur." });

            if (string.IsNullOrWhiteSpace(issueOwnerUserId) ||
                !await db.Users.AnyAsync(u => u.UserId == issueOwnerUserId))
            {
                return Results.BadRequest(new { message = "Geçerli bir sorumlu kullanıcı seçiniz." });
            }

            var riskId = string.IsNullOrWhiteSpace(request.RiskId)
                ? null
                : request.RiskId.Trim();

            if (riskId is not null)
            {
                var linkedRisk = await db.Risks
                    .AsNoTracking()
                    .Where(r => r.RiskId == riskId)
                    .Select(r => new { r.ProjectId })
                    .SingleOrDefaultAsync();

                if (linkedRisk is null)
                    return Results.BadRequest(new { message = "Bağlanmak istenen risk bulunamadı." });

                if (linkedRisk.ProjectId != request.ProjectId)
                    return Results.BadRequest(new { message = "Sorun yalnızca aynı projedeki bir riske bağlanabilir." });
            }

            var newIssue = new Issue
            {
                IssueId = await IdentifierGenerator.GenerateAsync(db.Set<Issue>(), i => i.IssueId, "ISS-"),
                ProjectId = request.ProjectId,
                RiskId = riskId,
                IssueTitle = issueTitle,
                IssuePriority = issuePriority,
                IssueOwnerUserId = issueOwnerUserId,
                IssueDueDate = request.IssueDueDate,
                IssueStatus = issueStatus,
                IssueImpact = issueImpact,
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
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var issue = await db.Set<Issue>().FindAsync(id);
            if (issue == null) return Results.NotFound(new { message = "Sorun kaydı bulunamadı." });

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü sorunlar üzerinde değişiklik yapamaz!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, issue.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projenin sorun kaydını güncelleme yetkiniz yok!" }, statusCode: 403);

            if (request.IssuePriority is not null &&
                !AllowedPriorities.Contains(request.IssuePriority.Trim()))
            {
                return Results.BadRequest(new { message = "Geçerli bir sorun önceliği seçiniz." });
            }

            if (request.IssueStatus is not null &&
                !AllowedStatuses.Contains(request.IssueStatus.Trim()))
            {
                return Results.BadRequest(new { message = "Geçerli bir sorun durumu seçiniz." });
            }

            if (request.IssueImpact is not null &&
                !AllowedImpacts.Contains(request.IssueImpact.Trim()))
            {
                return Results.BadRequest(new { message = "Geçerli bir sorun etkisi seçiniz." });
            }

            if (request.IssueOwnerUserId is not null)
            {
                var requestedOwnerUserId = request.IssueOwnerUserId.Trim();
                if (string.IsNullOrWhiteSpace(requestedOwnerUserId) ||
                    !await db.Users.AnyAsync(u => u.UserId == requestedOwnerUserId))
                {
                    return Results.BadRequest(new { message = "Geçerli bir sorumlu kullanıcı seçiniz." });
                }
            }

            // Alanları güvenli şekilde güncelleme
            if (!string.IsNullOrWhiteSpace(request.IssueTitle)) issue.IssueTitle = request.IssueTitle.Trim();
            if (!string.IsNullOrWhiteSpace(request.IssuePriority)) issue.IssuePriority = request.IssuePriority.Trim();
            if (!string.IsNullOrWhiteSpace(request.IssueOwnerUserId)) issue.IssueOwnerUserId = request.IssueOwnerUserId.Trim();
            if (request.IssueDueDate.HasValue) issue.IssueDueDate = request.IssueDueDate.Value;
            if (!string.IsNullOrWhiteSpace(request.IssueImpact)) issue.IssueImpact = request.IssueImpact.Trim();
            if (request.RootCause != null) issue.RootCause = request.RootCause;
            if (request.IssueResolution != null) issue.IssueResolution = request.IssueResolution;

            // 🧠 AKILLI KAPATMA MOTORU: Durum kapandıya çekildiyse kapanış tarihini otomatik yönet
            if (!string.IsNullOrEmpty(request.IssueStatus))
            {
                var requestedStatus = request.IssueStatus.Trim();
                issue.IssueStatus = requestedStatus;
                if ((requestedStatus.Equals("Kapalı", StringComparison.OrdinalIgnoreCase) ||
                     requestedStatus.Equals("Çözüldü", StringComparison.OrdinalIgnoreCase)) && issue.ClosedDate == null)
                {
                    issue.ClosedDate = DateTime.UtcNow;
                }
                else if (!requestedStatus.Equals("Kapalı", StringComparison.OrdinalIgnoreCase) &&
                         !requestedStatus.Equals("Çözüldü", StringComparison.OrdinalIgnoreCase))
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
