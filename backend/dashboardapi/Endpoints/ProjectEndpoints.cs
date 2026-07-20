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

            // ROL KONTROLÜ: Sistem Yöneticisi veya Üst Yönetim DEĞİLSE filtrele (Pasif projeler elenir)
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

        // 2. GET /projects/{id} -> Detaylı Proje Verisi + Otomatik Hesaplama Motoru
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

            // 🧮 RAPOR KURALI: Otomatik Sağlık Önerisi Algoritması (Planlanan vs Gerçekleşen İlerleme kıyası)
            string autoHealthRecommendation = "Yeşil";
            decimal progressGap = project.PlannedProgress - project.ActualProgress;
            if (progressGap > 15) autoHealthRecommendation = "Kırmızı";
            else if (progressGap > 5) autoHealthRecommendation = "Sarı";

            // 🧮 RAPOR KURALI: EVM (Kazanılmış Değer) Temel Hesaplamaları
            decimal bac = project.Bac;
            decimal pv = (project.PlannedProgress / 100) * bac; // Planlanan Değer
            decimal ev = (project.ActualProgress / 100) * bac;  // Kazanılan Değer
            decimal sv = ev - pv;                               // Zaman Sapması (Schedule Variance)
            decimal spi = pv > 0 ? ev / pv : 1;                 // Zaman Performans Endeksi

            var detailDto = new ProjectDetailDto(
                project.ProjectId,
                project.ProjectCode,
                project.ProjectName,
                project.ProjectDescription,
                project.ProjectStatus,
                project.ManualHealth,
                autoHealthRecommendation, // Hesaplanan otomatik sağlık durumu
                project.PlannedProgress,
                project.ActualProgress,
                bac,
                project.Currency,
                project.StartDate,
                project.BaselineFinishDate,
                project.ForecastFinishDate,
                project.ActualFinishDate,
                project.ProgramId,
                project.CustomerId,
                project.ProjectManagerUserId,
                project.ReportingFrequency ?? "Aylık", // MVP Kuralı
                project.Confidentiality ?? "Şirket İçi",
                sv,
                spi
            );

            return Results.Ok(detailDto);
        });

        // 3. POST /projects -> Yeni Proje Oluşturma (Sadece Sistem Yöneticisi)
        group.MapPost("", async (ProjectCreateDto dto, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Sistem Yöneticisi")
                return Results.Json(new { message = "Yeni proje oluşturma yetkisi sadece Sistem Yöneticisine aittir!" }, statusCode: 403);

            var exists = await db.Projects.AnyAsync(p => p.ProjectCode == dto.ProjectCode);
            if (exists)
                return Results.BadRequest(new { message = "Bu proje kodu zaten kullanımda!" });

            var newProject = new Project
            {
                ProjectId = "PRJ-" + Guid.NewGuid().ToString()[..8].ToUpper(),
                ProjectCode = dto.ProjectCode,
                ProjectName = dto.ProjectName,
                ProjectDescription = dto.ProjectDescription,
                ProjectStatus = dto.ProjectStatus ?? "Taslak",
                ManualHealth = dto.ManualHealth ?? "Yeşil",
                PlannedProgress = dto.PlannedProgress,
                ActualProgress = dto.ActualProgress,
                Bac = dto.Bac,
                Currency = dto.Currency ?? "TRY",
                StartDate = dto.StartDate,
                BaselineFinishDate = dto.BaselineFinishDate,
                ForecastFinishDate = dto.ForecastFinishDate,
                ProgramId = dto.ProgramId,
                CustomerId = dto.CustomerId,
                ProjectManagerUserId = dto.ProjectManagerUserId,
                ReportingFrequency = "Aylık", // MVP Sabiti
                Confidentiality = dto.Confidentiality ?? "Şirket İçi"
            };

            db.Projects.Add(newProject);
            await db.SaveChangesAsync();

            return Results.Created($"/projects/{newProject.ProjectId}", new { message = "Proje başarıyla oluşturuldu.", projectId = newProject.ProjectId });
        });

        // 4. PATCH /projects/{id} -> Proje Güncelleme (Sistem Yöneticisi veya Atanmış PM)
        group.MapPatch("{id}", async (string id, ProjectUpdateDto dto, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var project = await db.Projects.FindAsync(id);
            if (project == null) return Results.NotFound(new { message = "Proje bulunamadı." });

            // ROL KONTROLÜ: Üst yönetim güncelleyemez. PM ise sadece kendi projesini güncelleyebilir.
            if (userRole == "Üst Yönetim")
                return Results.Json(new { message = "Üst Yönetim rolü projeler üzerinde değişiklik yapamaz!" }, statusCode: 403);

            if (userRole == "Proje Yöneticisi" && project.ProjectManagerUserId != userId)
                return Results.Json(new { message = "Sadece kendi sorumlu olduğunuz projeleri güncelleyebilirsiniz!" }, statusCode: 403);

            // Alanları Güvenli Şekilde Güncelleme
            if (!string.IsNullOrEmpty(dto.ProjectName)) project.ProjectName = dto.ProjectName;
            if (!string.IsNullOrEmpty(dto.ProjectDescription)) project.ProjectDescription = dto.ProjectDescription;
            if (!string.IsNullOrEmpty(dto.ProjectStatus)) project.ProjectStatus = dto.ProjectStatus;
            if (!string.IsNullOrEmpty(dto.ManualHealth)) project.ManualHealth = dto.ManualHealth;
            if (dto.PlannedProgress.HasValue) project.PlannedProgress = dto.PlannedProgress.Value;
            if (dto.ActualProgress.HasValue) project.ActualProgress = dto.ActualProgress.Value;
            if (dto.Bac.HasValue) project.Bac = dto.Bac.Value;
            if (!string.IsNullOrEmpty(dto.Currency)) project.Currency = dto.Currency;
            if (dto.ForecastFinishDate.HasValue) project.ForecastFinishDate = dto.ForecastFinishDate.Value;
            if (dto.ActualFinishDate.HasValue) project.ActualFinishDate = dto.ActualFinishDate.Value;
            if (!string.IsNullOrEmpty(dto.Confidentiality)) project.Confidentiality = dto.Confidentiality;
            if (!string.IsNullOrEmpty(dto.ProjectManagerUserId) && userRole == "Sistem Yöneticisi") 
                project.ProjectManagerUserId = dto.ProjectManagerUserId; // PM değiştirmeyi sadece Admin yapabilir

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Proje başarıyla güncellendi." });
        });
    }
}