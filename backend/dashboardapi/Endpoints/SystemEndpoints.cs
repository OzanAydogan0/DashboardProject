using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using System.ComponentModel.DataAnnotations;

namespace dashboardapi.Endpoints;

public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        // ==========================================
        // 1. EVM (KAZANILMIŞ DEĞER) UÇ NOKTALARI
        // ==========================================

        app.MapGet("projects/{id}/evm-records", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
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
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // --- EVM FORMÜLLERİ (BACKEND'DE OTOMATİK HESAPLANIYOR) ---
            decimal? sv = request.Ev - request.Pv; // Zaman Sapması
            decimal? cv = request.Ev - request.Ac; // Maliyet Sapması
            
            // Sıfıra bölünme hatalarını (DivideByZeroException) önlemek için kontroller:
            decimal? spi = request.Pv != 0 ? Math.Round(request.Ev / request.Pv, 2) : null; 
            decimal? cpi = request.Ac != 0 ? Math.Round(request.Ev / request.Ac, 2) : null; 
            
            decimal? eac = (cpi != null && cpi != 0) ? Math.Round(request.Bac / cpi.Value, 2) : null;
            decimal? vac = eac != null ? request.Bac - eac : null;

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

        // ==========================================
        // 2. AUDIT LOG (DENETİM İZİ) UÇ NOKTASI
        // ==========================================

        app.MapGet("audit-logs", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            
            // Logları sadece Sistem Yöneticisi görebilir!
            if (userRole != "Sistem Yöneticisi")
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