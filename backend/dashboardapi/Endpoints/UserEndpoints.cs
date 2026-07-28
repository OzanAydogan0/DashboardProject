using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using BCrypt.Net; // 1. BCrypt kütüphanesini yukarıya ekledik

namespace dashboardapi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // 🛡️ RAPOR UYARISI: Kullanıcı yönetimi modülü (SCR-09) SADECE Sistem Yöneticisi'ne açıktır.
        // Güvenlik riski oluşmaması için rol kontrolünü direkt GRUP seviyesine uygulayarak tüm uçları koruyoruz.
        var group = app.MapGroup("users")
            .RequireAuthorization(policy => policy.RequireRole("Sistem Yöneticisi")); 

        // GET /users - Kullanıcıları Listeleme (Artık sadece Sistem Yöneticisi erişebilir)
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

            // 🛡️ RAPOR KURALI: Gelen düz şifreyi DB'ye yazmadan önce BCrypt ile güvenli şekilde hash'liyoruz.
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new dashboardapi.Models.User
            {
                UserId = "USR-" + Guid.NewGuid().ToString()[..8].ToUpper(),
                Email = dto.Email,
                FullName = dto.FullName,
                UserRole = dto.Role,
                PasswordHash = hashedPassword, // Artık hash'lenmiş şifre kaydediliyor
                UserStatus = "Aktif",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            return Results.Created($"/users/{newUser.UserId}", 
                new UserDto(newUser.UserId, newUser.Email, newUser.FullName, newUser.UserRole, newUser.UserStatus));
        });

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
}