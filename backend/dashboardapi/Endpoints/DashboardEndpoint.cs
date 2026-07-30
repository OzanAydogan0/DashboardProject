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
            return await GetDashboardAsync(userClaims, db);
        });

        // 2. GET /dashboard/portfolio?projectIds=... -> Seçili projelerin portföy özeti
        group.MapGet("portfolio", async (
            HttpRequest request,
            ClaimsPrincipal userClaims,
            AppDbContext db) =>
        {
            var projectIds = request.Query["projectIds"]
                .SelectMany(value => (value ?? string.Empty).Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return await GetDashboardAsync(userClaims, db, projectIds);
        });

        // 3. GET /dashboard/projects/{id}/evm -> Projeye Ait Dönemsel EVM Grafikleri (vw_evm verisi)
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

    private static async Task<IResult> GetDashboardAsync(
        ClaimsPrincipal userClaims,
        AppDbContext db,
        IReadOnlyCollection<string>? requestedProjectIds = null)
    {
        var userRole = PermissionHelper.GetUserRole(userClaims);
        var userId = PermissionHelper.GetUserId(userClaims);

        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var activeProjects = db.Projects
            .Where(project => project.IsActive == 1);

        if (requestedProjectIds is { Count: > 0 })
        {
            activeProjects = activeProjects.Where(project =>
                requestedProjectIds.Contains(project.ProjectId));
        }

        if (!PermissionHelper.IsSystemAdmin(userRole) &&
            !PermissionHelper.IsExecutive(userRole))
        {
            activeProjects = activeProjects.Where(project =>
                project.ProjectManagerUserId == userId ||
                project.ProjectUsers.Any(assignment =>
                    assignment.UserId == userId &&
                    assignment.AssignmentStatus == "Aktif"));
        }

        var allowedProjectIds = await activeProjects
            .Select(project => project.ProjectId)
            .ToListAsync();

        var rawData = await db.Set<VwDashboard>()
            .Where(summary =>
                summary.ProjectId != null &&
                allowedProjectIds.Contains(summary.ProjectId))
            .OrderBy(summary => summary.ProjectId)
            .ToListAsync();

        var result = rawData.Select(summary => new DashboardSummaryDto(
            summary.ProjectId,
            summary.ProjectCode,
            summary.ProjectName,
            summary.ProjectStatus,
            HealthStatusHelper.Normalize(summary.ManualHealth),
            summary.PlannedProgress,
            summary.ActualProgress,
            summary.BaselineFinishDate,
            summary.ForecastFinishDate,
            summary.Bac,
            summary.Currency,
            ParseInt(summary.OpenRiskCount),
            ParseInt(summary.OpenIssueCount),
            ParseInt(summary.OpenActionCount),
            ParseInt(summary.OpenMilestoneCount),
            ParseString(summary.LatestEvmPeriod)
        )).ToList();

        return Results.Ok(result);
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
