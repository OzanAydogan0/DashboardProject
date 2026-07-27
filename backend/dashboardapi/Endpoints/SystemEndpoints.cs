using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;
using System.ComponentModel.DataAnnotations;

namespace dashboardapi.Endpoints;

public static class SystemEndpoints
{
    private static (decimal? Sv, decimal? Cv, decimal? Spi, decimal? Cpi, decimal? Eac, decimal? Vac) CalculateEvmMetrics(decimal bac, decimal pv, decimal ev, decimal ac)
    {
        decimal? sv = ev - pv;
        decimal? cv = ev - ac;
        decimal? spi = pv != 0 ? Math.Round(ev / pv, 2) : null;
        decimal? cpi = ac != 0 ? Math.Round(ev / ac, 2) : null;
        decimal? eac = (cpi != null && cpi != 0) ? Math.Round(bac / cpi.Value, 2) : null;
        decimal? vac = eac != null ? bac - eac : null;

        return (sv, cv, spi, cpi, eac, vac);
    }

    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        // ==========================================
        // 1. EVM (KAZANILMIŞ DEĞER) UÇ NOKTALARI
        // ==========================================

        app.MapGet("projects/{id}/evm-records", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Bu projenin EVM kayıtlarını görmeye yetkiniz yok!" }, statusCode: 403);

            // 1. Adım: Projenin kendi para birimini 'Projects' tablosundan sorguluyoruz
            var projectCurrency = await db.Projects
                .Where(p => p.ProjectId == id)
                .Select(p => p.Currency)
                .FirstOrDefaultAsync() ?? "TRY";

            // 2. Adım: Projeye ait EVM kayıtlarını getiriyoruz
            var records = await db.Set<EvmRecord>()
                .Where(e => e.ProjectId == id)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            // 3. Adım: DTO'ya 'e.Currency' yerine veritabanından aldığımız 'projectCurrency' değerini veriyoruz
            var result = records.Select(e => new EvmRecordDto(
                e.EvmRecordId, 
                e.ProjectId, 
                e.Period, 
                projectCurrency, // 👈 CS1061 ÇÖZÜMÜ: 'e.Currency' yerine 'projectCurrency' kullanıldı
                e.Bac, 
                e.Pv, 
                e.Ev, 
                e.Ac,
                e.Sv, 
                e.Cv, 
                e.Spi, 
                e.Cpi, 
                e.Eac, 
                e.Vac
            )).ToList();

            return Results.Ok(result);
        });

        app.MapPost("evm-records", async (CreateEvmRecordRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü EVM kaydı ekleyemez!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, request.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye EVM kaydı ekleme yetkiniz yok!" }, statusCode: 403);

            // --- EVM FORMÜLLERİ (BACKEND'DE OTOMATİK HESAPLANIYOR) ---
            var (sv, cv, spi, cpi, eac, vac) = CalculateEvmMetrics(request.Bac, request.Pv, request.Ev, request.Ac);

            var newRecord = new EvmRecord
            {
                EvmRecordId = Guid.NewGuid().ToString(),
                ProjectId = request.ProjectId,
                Period = request.Period,
                // 👈 CS1061 ÇÖZÜMÜ: 'EvmRecord' veritabanı modelinde 'Currency' alanı olmadığı için
                // hatalı atama satırı kaldırıldı. Para birimi ana projeye aittir.
                Bac = request.Bac,
                Pv = request.Pv,
                Ev = request.Ev,
                Ac = request.Ac,
                Sv = sv,
                Cv = cv,
                Spi = spi,
                Cpi = cpi,
                Eac = eac,
                Vac = vac,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<EvmRecord>().Add(newRecord);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "EVM kaydı başarıyla oluşturuldu ve metrikler hesaplandı.", evmRecordId = newRecord.EvmRecordId }, statusCode: 201);
        });

        app.MapPut("evm-records/{id}", async (string id, UpdateEvmRecordRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü EVM kaydını güncelleyemez!" }, statusCode: 403);

            var existingRecord = await db.Set<EvmRecord>().FirstOrDefaultAsync(e => e.EvmRecordId == id);
            if (existingRecord is null)
                return Results.Json(new { message = "EVM kaydı bulunamadı." }, statusCode: 404);

            if (!await PermissionHelper.CanWriteProjectAsync(db, existingRecord.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye EVM kaydı güncelleme yetkiniz yok!" }, statusCode: 403);

            if (!string.IsNullOrWhiteSpace(request.ProjectId) && request.ProjectId != existingRecord.ProjectId)
                return Results.Json(new { message = "EVM kaydının proje bilgisi değiştirilemez." }, statusCode: 400);

            var periodConflict = await db.Set<EvmRecord>().AnyAsync(e => e.ProjectId == existingRecord.ProjectId && e.Period == request.Period && e.EvmRecordId != id);
            if (periodConflict)
                return Results.Json(new { message = "Bu dönem için zaten bir EVM kaydı bulunuyor." }, statusCode: 409);

            var (sv, cv, spi, cpi, eac, vac) = CalculateEvmMetrics(request.Bac, request.Pv, request.Ev, request.Ac);

            existingRecord.Period = request.Period;
            existingRecord.Bac = request.Bac;
            existingRecord.Pv = request.Pv;
            existingRecord.Ev = request.Ev;
            existingRecord.Ac = request.Ac;
            existingRecord.Sv = sv;
            existingRecord.Cv = cv;
            existingRecord.Spi = spi;
            existingRecord.Cpi = cpi;
            existingRecord.Eac = eac;
            existingRecord.Vac = vac;
            existingRecord.UpdatedByUserId = userId;
            existingRecord.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Json(new { message = "EVM kaydı başarıyla güncellendi.", evmRecordId = existingRecord.EvmRecordId }, statusCode: 200);
        });

        app.MapDelete("evm-records/{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü EVM kaydını silemez!" }, statusCode: 403);

            var existingRecord = await db.Set<EvmRecord>().FirstOrDefaultAsync(e => e.EvmRecordId == id);
            if (existingRecord is null)
                return Results.Json(new { message = "EVM kaydı bulunamadı." }, statusCode: 404);

            if (!await PermissionHelper.CanWriteProjectAsync(db, existingRecord.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye EVM kaydı silme yetkiniz yok!" }, statusCode: 403);

            db.Set<EvmRecord>().Remove(existingRecord);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "EVM kaydı başarıyla silindi.", evmRecordId = existingRecord.EvmRecordId }, statusCode: 200);
        });

        // ==========================================
        // 2. AUDIT LOG (DENETİM İZİ) UÇ NOKTASI
        // ==========================================

        app.MapGet("audit-logs", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            
            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Sistem loglarını görüntüleme yetkiniz yok!" }, statusCode: 403);

            var logs = await db.Set<AuditLog>()
                .Include(a => a.User)
                .OrderByDescending(a => a.ChangedAt)
                .Take(100) // Performans için son 100 logu getiriyoruz
                .ToListAsync();

            var result = logs.Select(a => new AuditLogDto(
                a.AuditLogId,
                a.UserId,
                a.User != null ? $"{a.User.FullName}".Trim() : "Sistem",
                a.EntityName,
                a.EntityId,
                a.ActionType,
                a.OldValues,
                a.NewValues,
                a.ChangedAt,
                a.IpAddress
            )).ToList();

            return Results.Ok(result);
        });
    }
}