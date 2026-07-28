using System.Security.Claims;
using dashboardapi.Data;
using Microsoft.EntityFrameworkCore;

namespace dashboardapi.Services;

public static class PermissionHelper
{
    public const string SystemAdminRole = "Sistem Yöneticisi";
    public const string ProjectManagerRole = "Proje Yöneticisi";
    public const string ExecutiveRole = "Üst Yönetim İzleyicisi";
    public const string LegacyExecutiveRole = "Üst Yönetim";

    public static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return string.Empty;

        return role.Trim() switch
        {
            "Üst Yönetim" or "Üst Yönetim İzleyicisi" => ExecutiveRole,
            _ => role.Trim()
        };
    }

    public static bool IsSystemAdmin(string? role) => NormalizeRole(role) == SystemAdminRole;

    public static bool IsExecutive(string? role) => NormalizeRole(role) == ExecutiveRole;

    public static bool IsProjectManager(string? role) => NormalizeRole(role) == ProjectManagerRole;

    public static string? GetUserId(ClaimsPrincipal userClaims) =>
        userClaims.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public static string? GetUserRole(ClaimsPrincipal userClaims) =>
        NormalizeRole(userClaims.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);

    public static async Task<bool> CanAccessProjectAsync(AppDbContext db, string projectId, string userId, string? role)
    {
        role = NormalizeRole(role);

        if (IsSystemAdmin(role) || IsExecutive(role))
            return await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.IsActive == 1);

        return await db.Projects.AnyAsync(p =>
            p.ProjectId == projectId &&
            p.IsActive == 1 &&
            (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
    }

    public static async Task<bool> CanWriteProjectAsync(AppDbContext db, string projectId, string userId, string? role)
    {
        role = NormalizeRole(role);

        if (IsSystemAdmin(role))
            return await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.IsActive == 1);

        if (IsExecutive(role))
            return false;

        return await db.Projects.AnyAsync(p =>
            p.ProjectId == projectId &&
            p.IsActive == 1 &&
            (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
    }

    public static async Task<bool> CanManageProjectAsync(AppDbContext db, string projectId, string userId, string? role)
    {
        role = NormalizeRole(role);

        if (IsSystemAdmin(role))
            return await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.IsActive == 1);

        if (IsExecutive(role))
            return false;

        return await db.Projects.AnyAsync(p =>
            p.ProjectId == projectId &&
            p.IsActive == 1 &&
            p.ProjectManagerUserId == userId);
    }
}
