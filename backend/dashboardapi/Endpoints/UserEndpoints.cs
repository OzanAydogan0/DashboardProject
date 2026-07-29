using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class UserEndpoints
{
    private static readonly HashSet<string> AllowedRoles =
    [
        "Sistem Yöneticisi",
        "Proje Yöneticisi",
        "Üst Yönetim İzleyicisi"
    ];

    private static readonly HashSet<string> AllowedStatuses = ["Aktif", "Pasif"];

    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users")
            .RequireAuthorization(policy => policy.RequireRole("Sistem Yöneticisi"));

        group.MapGet("", async (AppDbContext db) =>
        {
            var users = await db.Users
                .AsNoTracking()
                .Include(u => u.ProjectUserUsers)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Results.Ok(users.Select(ToDto).ToList());
        });

        group.MapPost("", async (UserCreateDto dto, AppDbContext db) =>
        {
            var email = dto.Email?.Trim().ToLowerInvariant();
            var fullName = dto.FullName?.Trim();
            var role = dto.Role?.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(role) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return Results.BadRequest(new { message = "Ad soyad, e-posta, rol ve şifre alanları zorunludur." });
            }

            if (!AllowedRoles.Contains(role))
                return Results.BadRequest(new { message = "Geçersiz kullanıcı rolü seçildi." });

            if (dto.Password.Length < 8)
                return Results.BadRequest(new { message = "Şifre en az 8 karakter olmalıdır." });

            var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (exists)
                return Results.BadRequest(new { message = "Bu e-posta adresi zaten kullanımda!" });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new User
            {
                UserId = await IdentifierGenerator.GenerateAsync(db.Users, u => u.UserId, "USR-"),
                Email = email,
                FullName = fullName,
                UserRole = role,
                PasswordHash = hashedPassword,
                UserStatus = "Aktif",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/users/{newUser.UserId}",
                new UserDto(
                    newUser.UserId,
                    newUser.Email,
                    newUser.FullName,
                    newUser.UserRole,
                    newUser.UserStatus,
                    []));
        });

        group.MapPatch("{id}", async (
            string id,
            UserUpdateDto dto,
            ClaimsPrincipal userClaims,
            AppDbContext db) =>
        {
            var administratorId = PermissionHelper.GetUserId(userClaims);
            if (string.IsNullOrWhiteSpace(administratorId))
                return Results.Unauthorized();

            var user = await db.Users
                .Include(u => u.ProjectUserUsers)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return Results.NotFound(new { message = "Kullanıcı bulunamadı." });

            var role = dto.Role?.Trim();
            if (role is not null && !AllowedRoles.Contains(role))
                return Results.BadRequest(new { message = "Geçersiz kullanıcı rolü seçildi." });

            var status = dto.Status?.Trim();
            if (status is not null && !AllowedStatuses.Contains(status))
                return Results.BadRequest(new { message = "Geçersiz kullanıcı durumu seçildi." });

            var email = dto.Email?.Trim().ToLowerInvariant();
            if (dto.Email is not null && string.IsNullOrWhiteSpace(email))
                return Results.BadRequest(new { message = "E-posta adresi boş bırakılamaz." });

            if (email is not null)
            {
                var emailInUse = await db.Users.AnyAsync(u =>
                    u.UserId != id && u.Email.ToLower() == email);

                if (emailInUse)
                    return Results.BadRequest(new { message = "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor." });
            }

            var fullName = dto.FullName?.Trim();
            if (dto.FullName is not null && string.IsNullOrWhiteSpace(fullName))
                return Results.BadRequest(new { message = "Ad soyad alanı boş bırakılamaz." });

            if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password.Length < 8)
                return Results.BadRequest(new { message = "Yeni şifre en az 8 karakter olmalıdır." });

            HashSet<string>? desiredProjectIds = null;
            if (dto.ProjectIds is not null)
            {
                desiredProjectIds = dto.ProjectIds
                    .Where(projectId => !string.IsNullOrWhiteSpace(projectId))
                    .ToHashSet(StringComparer.Ordinal);

                var existingProjectIds = desiredProjectIds.Count == 0
                    ? []
                    : await db.Projects
                        .Where(project => desiredProjectIds.Contains(project.ProjectId))
                        .Select(project => project.ProjectId)
                        .ToListAsync();

                var missingProjectIds = desiredProjectIds
                    .Except(existingProjectIds)
                    .ToList();

                if (missingProjectIds.Count > 0)
                {
                    return Results.BadRequest(new
                    {
                        message = $"Bulunamayan proje: {string.Join(", ", missingProjectIds)}"
                    });
                }
            }

            var userChanged = false;

            if (role is not null && user.UserRole != role)
            {
                user.UserRole = role;
                userChanged = true;
            }

            if (status is not null && user.UserStatus != status)
            {
                user.UserStatus = status;
                userChanged = true;
            }

            if (email is not null && user.Email != email)
            {
                user.Email = email;
                userChanged = true;
            }

            if (fullName is not null && user.FullName != fullName)
            {
                user.FullName = fullName;
                userChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                userChanged = true;
            }

            if (desiredProjectIds is not null)
            {
                var assignmentsByProject = user.ProjectUserUsers
                    .ToDictionary(assignment => assignment.ProjectId);

                foreach (var assignment in user.ProjectUserUsers
                    .Where(assignment => !desiredProjectIds.Contains(assignment.ProjectId))
                    .ToList())
                {
                    db.ProjectUsers.Remove(assignment);
                    userChanged = true;
                }

                foreach (var projectId in desiredProjectIds)
                {
                    if (assignmentsByProject.TryGetValue(projectId, out var assignment))
                    {
                        if (assignment.AssignmentStatus != "Aktif")
                        {
                            assignment.AssignmentStatus = "Aktif";
                            assignment.AssignedByUserId = administratorId;
                            assignment.AssignedAt = DateTime.UtcNow;
                            assignment.UpdatedAt = DateTime.UtcNow;
                            userChanged = true;
                        }

                        continue;
                    }

                    db.ProjectUsers.Add(new ProjectUser
                    {
                        ProjectUserId = await IdentifierGenerator.GenerateAsync(
                            db.ProjectUsers,
                            projectUser => projectUser.ProjectUserId,
                            "PU-"),
                        ProjectId = projectId,
                        UserId = id,
                        AssignedByUserId = administratorId,
                        AssignmentStatus = "Aktif",
                        AssignedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                    userChanged = true;
                }
            }

            if (userChanged)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            var assignedProjectIds = desiredProjectIds ??
                user.ProjectUserUsers
                    .Where(assignment =>
                        assignment.AssignmentStatus == "Aktif" &&
                        db.Entry(assignment).State != EntityState.Deleted)
                    .Select(assignment => assignment.ProjectId)
                    .ToHashSet(StringComparer.Ordinal);

            return Results.Ok(new UserDto(
                user.UserId,
                user.Email,
                user.FullName,
                user.UserRole,
                user.UserStatus,
                assignedProjectIds.OrderBy(projectId => projectId).ToList()));
        });

        group.MapDelete("{id}", async (string id, AppDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user == null)
                return Results.NotFound(new { message = "Kullanıcı bulunamadı." });

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Kullanıcı başarıyla silindi." });
        });
    }

    private static UserDto ToDto(User user) =>
        new(
            user.UserId,
            user.Email,
            user.FullName,
            user.UserRole,
            user.UserStatus,
            user.ProjectUserUsers
                .Where(assignment => assignment.AssignmentStatus == "Aktif")
                .Select(assignment => assignment.ProjectId)
                .OrderBy(projectId => projectId)
                .ToList());
}
