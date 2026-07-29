using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dashboard").RequireAuthorization();

        // 1. GET /dashboard -> Akıllı Performans Paneli (vw_dashboard verisi)
        group.MapGet("", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // Önce görünüm sorgusunu hazırlıyoruz
            var activeProjectIds = await db.Projects
                .Where(p => p.IsActive == 1)
                .Select(p => p.ProjectId)
                .ToListAsync();

            var query = db.Set<VwDashboard>()
                .Where(v => v.ProjectId != null && activeProjectIds.Contains(v.ProjectId))
                .AsQueryable();

            if (!PermissionHelper.IsSystemAdmin(userRole) && !PermissionHelper.IsExecutive(userRole))
            {
                var allowedProjectIds = await db.Projects
                    .Where(p => p.IsActive == 1 && (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)))
                    .Select(p => p.ProjectId)
                    .ToListAsync();

                query = query.Where(v => v.ProjectId != null && allowedProjectIds.Contains(v.ProjectId));
            }

            // SQLite'ın byte[] verilerini hafızada işlemek için listeyi çekiyoruz
            var rawData = await query.ToListAsync();

            // Ham veriyi DTO'ya dönüştürürken byte dizilerini sayıya/metne çeviriyoruz
            var result = rawData.Select(v => new DashboardSummaryDto(
                v.ProjectId,
                v.ProjectCode,
                v.ProjectName,
                v.ProjectStatus,
                HealthStatusHelper.Normalize(v.ManualHealth),
                v.PlannedProgress,
                v.ActualProgress,
                v.BaselineFinishDate,
                v.ForecastFinishDate,
                v.Bac,
                v.Currency,
                ParseInt(v.OpenRiskCount),
                ParseInt(v.OpenIssueCount),
                ParseInt(v.OpenActionCount),
                ParseInt(v.OpenMilestoneCount),
                ParseString(v.LatestEvmPeriod)
            )).ToList();

            return Results.Ok(result);
        });

        // 2. GET /dashboard/projects/{id}/evm -> Projeye Ait Dönemsel EVM Grafikleri (vw_evm verisi)
        group.MapGet("projects/{id}/evm", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId!, userRole))
                return Results.Json(new { message = "Bu projenin finansal analiz verilerini görmeye yetkiniz yok!" }, statusCode: 403);

            // İlgili projenin EVM geçmişini çekiyoruz
            var rawEvmData = await db.Set<VwEvm>()
                .Where(e => e.ProjectId == id)
                .OrderBy(e => e.Period) // Döneme göre sıralı gelsin (Örn: 2026-01, 2026-02)
                .ToListAsync();

            var result = rawEvmData.Select(e => new EvmPerformanceDto(
                e.EvmRecordId,
                e.ProjectId,
                e.ProjectCode,
                e.ProjectName,
                e.Period,
                e.Bac,
                e.Pv,
                e.Ev,
                e.Ac,
                ParseDecimal(e.Sv),
                ParseDecimal(e.Cv),
                ParseDecimal(e.Spi),
                ParseDecimal(e.Cpi),
                ParseDecimal(e.Eac),
                ParseDecimal(e.Vac)
            )).ToList();

            return Results.Ok(result);
        });
    }

    // --- SQLite Byte[] Dönüştürücü Yardımcı Metotlar ---
    private static int ParseInt(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return 0;
        var str = Encoding.UTF8.GetString(bytes);
        return int.TryParse(str, out var val) ? val : 0;
    }

    private static decimal ParseDecimal(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return 0;
        var str = Encoding.UTF8.GetString(bytes);
        return decimal.TryParse(str, out var val) ? val : 0;
    }

    private static string ParseString(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return string.Empty;
        return Encoding.UTF8.GetString(bytes);
    }
}
