using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("projects").RequireAuthorization();

        // 1. GET /projects -> Rol Bazlı ve Akıllı Filtreli Proje Listesi
        group.MapGet("", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            IQueryable<Project> query = db.Projects;

            if (!PermissionHelper.IsSystemAdmin(userRole) && !PermissionHelper.IsExecutive(userRole))
            {
                query = query.Where(p =>
                    p.ProjectManagerUserId == userId ||
                    p.ProjectUsers.Any(pu =>
                        pu.UserId == userId &&
                        pu.AssignmentStatus == "Aktif"));
            }

            var projects = await query
                .Include(p => p.ProjectManagerUser)
                .Select(p => new ProjectSummaryDto(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.ProjectStatus,
                    p.ManualHealth,
                    p.PlannedProgress,
                    p.ActualProgress,
                    p.Bac,
                    p.Currency,
                    p.StartDate,
                    p.BaselineFinishDate,
                    p.ProjectManagerUserId,
                    p.ProjectManagerUser != null ? p.ProjectManagerUser.FullName : null,
                    p.IsActive
                ))
                .ToListAsync();

            var result = projects
                .Select(project => project with
                {
                    ManualHealth = HealthStatusHelper.Normalize(project.ManualHealth)
                })
                .ToList();

            return Results.Ok(result);
        });

        // 2. GET /projects/{id} -> Detaylı Proje Verisi + Otomatik Hesaplama Motoru
        group.MapGet("{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var project = await db.Projects.FirstOrDefaultAsync(
                candidate => candidate.ProjectId == id);
            if (project == null) return Results.NotFound(new { message = "Proje bulunamadı." });

            if (!await PermissionHelper.CanAccessProjectAsync(
                    db,
                    id,
                    userId,
                    userRole,
                    includeInactive: true))
                return Results.Json(new { message = "Bu projenin detaylarını görmeye yetkiniz yok!" }, statusCode: 403);

            // 🧮 RAPOR KURALI: Otomatik Sağlık Önerisi Algoritması (Planlanan vs Gerçekleşen İlerleme kıyası)
            string autoHealthRecommendation = HealthStatusHelper.Good;
            decimal progressGap = project.PlannedProgress - project.ActualProgress;
            if (progressGap > 15) autoHealthRecommendation = HealthStatusHelper.Critical;
            else if (progressGap > 5) autoHealthRecommendation = HealthStatusHelper.Medium;

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
                HealthStatusHelper.Normalize(project.ManualHealth),
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
                project.ProjectManagerUser?.FullName,
                project.ReportingFrequency ?? "Aylık", // MVP Kuralı
                project.Confidentiality ?? "Şirket İçi",
                sv,
                spi,
                project.IsActive
            );

            return Results.Ok(detailDto);
        });

        // 3. POST /projects -> Yeni Proje Oluşturma (Sadece Sistem Yöneticisi)
        group.MapPost("", async (ProjectCreateDto dto, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Yeni proje oluşturma yetkisi sadece Sistem Yöneticisine aittir!" }, statusCode: 403);

            var exists = await db.Projects.AnyAsync(p => p.ProjectCode == dto.ProjectCode);
            if (exists)
                return Results.BadRequest(new { message = "Bu proje kodu zaten kullanımda!" });

            // Oturum açan kullanıcının ID'si yoksa varsayılan atanır
            var creatorUserId = !string.IsNullOrEmpty(userId) ? userId : "USR-001";

            var newProject = new Project
            {
                ProjectId = await IdentifierGenerator.GenerateAsync(db.Projects, p => p.ProjectId, "PRJ-"),
                ProjectCode = dto.ProjectCode,
                ProjectName = dto.ProjectName,
                ProjectDescription = dto.ProjectDescription,
                ProjectStatus = dto.ProjectStatus ?? "Taslak",
                ManualHealth = HealthStatusHelper.ToStorageValue(dto.ManualHealth, HealthStatusHelper.Good),
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
                Confidentiality = dto.Confidentiality ?? "Şirket İçi",

                // 🚨 ZORUNLU VERİTABANI ALANLARI DÜZELTİLDİ:
                CreatedByUserId = creatorUserId,
                UpdatedByUserId = creatorUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = 1
            };

            db.Projects.Add(newProject);
            await db.SaveChangesAsync();

            return Results.Created($"/projects/{newProject.ProjectId}", new { message = "Proje başarıyla oluşturuldu.", projectId = newProject.ProjectId });
        });

        // 4. PATCH /projects/{id} -> Proje Güncelleme (Sistem Yöneticisi veya Atanmış PM)
        group.MapPatch("{id}", async (string id, ProjectUpdateDto dto, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == id);
            if (project == null) return Results.NotFound(new { message = "Proje bulunamadı." });

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü projeler üzerinde değişiklik yapamaz!" }, statusCode: 403);

            if (!PermissionHelper.IsSystemAdmin(userRole) &&
                !await PermissionHelper.CanManageProjectAsync(
                    db,
                    id,
                    userId,
                    userRole,
                    includeInactive: true))
                return Results.Json(new { message = "Sadece kendi sorumlu olduğunuz projeleri güncelleyebilirsiniz!" }, statusCode: 403);

            // Alanları Güvenli Şekilde Güncelleme
            if (!string.IsNullOrEmpty(dto.ProjectName)) project.ProjectName = dto.ProjectName;
            if (!string.IsNullOrEmpty(dto.ProjectDescription)) project.ProjectDescription = dto.ProjectDescription;
            if (!string.IsNullOrEmpty(dto.ProjectStatus)) project.ProjectStatus = dto.ProjectStatus;
            if (!string.IsNullOrEmpty(dto.ManualHealth))
                project.ManualHealth = HealthStatusHelper.ToStorageValue(dto.ManualHealth);
            if (dto.PlannedProgress.HasValue) project.PlannedProgress = dto.PlannedProgress.Value;
            if (dto.ActualProgress.HasValue) project.ActualProgress = dto.ActualProgress.Value;
            if (dto.Bac.HasValue) project.Bac = dto.Bac.Value;
            if (!string.IsNullOrEmpty(dto.Currency)) project.Currency = dto.Currency;
            if (dto.ForecastFinishDate.HasValue) project.ForecastFinishDate = dto.ForecastFinishDate.Value;
            if (dto.ActualFinishDate.HasValue) project.ActualFinishDate = dto.ActualFinishDate.Value;
            if (!string.IsNullOrEmpty(dto.Confidentiality)) project.Confidentiality = dto.Confidentiality;
            if (!string.IsNullOrEmpty(dto.ProjectManagerUserId) && userRole == "Sistem Yöneticisi") 
                project.ProjectManagerUserId = dto.ProjectManagerUserId;
            if (dto.IsActive.HasValue) project.IsActive = dto.IsActive.Value;

            // 🚨 GÜNCELLEME DENETİM (AUDIT) ALANLARI:
            if (!string.IsNullOrEmpty(userId))
            {
                project.UpdatedByUserId = userId;
            }
            project.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Proje başarıyla güncellendi." });
        });
    }
}
