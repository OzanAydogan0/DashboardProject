using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

namespace dashboardapi.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("projects").RequireAuthorization();

        // 1. GET /projects -> Rol Bazlı ve Akıllı Filtreli Proje Listesi
        group.MapGet("", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            IQueryable<Project> query = db.Projects;

            // ROL KONTROLÜ: Sistem Yöneticisi veya Üst Yönetim DEĞİLSE filtrele
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                // Kullanıcı ya projenin yöneticisi olmalı YA DA proje ekibinde yer almalı
                query = query.Where(p => p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId));
            }

            var projects = await query
                .Select(p => new ProjectSummaryDto(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectStatus,
                    p.ManualHealth,
                    p.PlannedProgress,
                    p.ActualProgress,
                    p.StartDate,
                    p.BaselineFinishDate
                ))
                .ToListAsync();

            return Results.Ok(projects);
        });

        // 2. GET /projects/{id} -> Detaylı Proje Verisi
        group.MapGet("{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var project = await db.Projects.FindAsync(id);
            if (project == null) return Results.NotFound(new { message = "Proje bulunamadı." });

            // GÜVENLİK KONTROLÜ: Kullanıcının bu projeyi görmeye yetkisi var mı?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = project.ProjectManagerUserId == userId || 
                                 await db.ProjectUsers.AnyAsync(pu => pu.ProjectId == id && pu.UserId == userId);
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin detaylarını görmeye yetkiniz yok!" }, statusCode: 403);
            }

            var detailDto = new ProjectDetailDto(
                project.ProjectId,
                project.ProjectCode,
                project.ProjectName,
                project.ProjectDescription,
                project.ProjectStatus,
                project.ManualHealth,
                project.PlannedProgress,
                project.ActualProgress,
                project.Bac,
                project.Currency,
                project.StartDate,
                project.BaselineFinishDate,
                project.ForecastFinishDate,
                project.ActualFinishDate,
                project.ProgramId,
                project.CustomerId,
                project.ProjectManagerUserId
            );

            return Results.Ok(detailDto);
        });
    }
}