using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;

namespace dashboardapi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users").RequireAuthorization(); // Sadece giriş yapmış kullanıcılar görebilir

        // GET /users - Kullanıcıları Listeleme
        group.MapGet("", async (AppDbContext db) =>
        {
            var users = await db.Users
                .Select(u => new UserDto(u.UserId, u.Email, u.FullName, u.UserRole, u.UserStatus))
                .ToListAsync();

            return Results.Ok(users);
        });

        // POST /users - Kullanıcı Ekleme
        group.MapPost("", async (UserCreateDto dto, AppDbContext db) =>
        {
            var exists = await db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return Results.BadRequest(new { message = "Bu e-posta adresi zaten kullanımda!" });

            // Adresi dashboardapi.Models.User olarak düzelttik
            var newUser = new dashboardapi.Models.User
            {
                UserId = "USR-" + Guid.NewGuid().ToString()[..8].ToUpper(),
                Email = dto.Email,
                FullName = dto.FullName,
                UserRole = dto.Role,
                PasswordHash = dto.Password, 
                UserStatus = "Aktif",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            return Results.Created($"/users/{newUser.UserId}", 
                new UserDto(newUser.UserId, newUser.Email, newUser.FullName, newUser.UserRole, newUser.UserStatus));
        }).RequireAuthorization(policy => policy.RequireRole("Sistem Yöneticisi")); // Sadece Sistem Yöneticisi ekleyebilir

        // PATCH /users/{id} - Rol ve Aktiflik Güncelleme
        group.MapPatch("{id}", async (string id, UserUpdateDto dto, AppDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user == null)
                return Results.NotFound(new { message = "Kullanıcı bulunamadı." });

            if (!string.IsNullOrEmpty(dto.Role))
                user.UserRole = dto.Role;

            if (!string.IsNullOrEmpty(dto.Status))
                user.UserStatus = dto.Status;

            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new UserDto(user.UserId, user.Email, user.FullName, user.UserRole, user.UserStatus));
        }).RequireAuthorization(policy => policy.RequireRole("Sistem Yöneticisi")); // Sadece Sistem Yöneticisi güncelleyebilir
    }
}