using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using BCrypt.Net; // BCrypt kütüphanesini ekledik

namespace dashboardapi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("auth");

        // POST /auth/login
        group.MapPost("login", async (LoginRequest? request, AppDbContext db, IConfiguration config) =>
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new
                {
                    message = "E-posta ve şifre alanları zorunludur."
                });
            }

            var normalizedEmail =
                request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(
                candidate =>
                    candidate.Email.ToLower() == normalizedEmail);
            
            // 🛡️ BCrypt ile Şifre Doğrulama (Düz metin yerine hash kontrolü yapıyoruz)
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Json(new { message = "E-posta veya şifre hatalı!" }, statusCode: 401);

            if (user.UserStatus != "Aktif")
                return Results.Json(new { message = "Kullanıcı hesabı aktif değil!" }, statusCode: 403);

            // JWT Token Üretimi
            var jwtSettings = config.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserRole),
                new Claim("fullName", user.FullName),
                new Claim(
                    "securityVersion",
                    user.UpdatedAt.Ticks.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(
                    double.Parse(
                        jwtSettings["ExpiryMinutes"]!,
                        System.Globalization.CultureInfo.InvariantCulture)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new LoginResponse(tokenString, user.UserId, user.FullName, user.UserRole));
        })
            .AllowAnonymous()
            .RequireRateLimiting("LoginLimiter"); // 🛡️ Hız sınırını bu endpoint'e bağladık!

        // GET /auth/me
        group.MapGet("me", async (ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user == null)
                return Results.NotFound(new { message = "Kullanıcı bulunamadı." });

            return Results.Ok(new MeResponse(user.UserId, user.Email, user.FullName, user.UserRole, user.UserStatus));
        }).RequireAuthorization(); // JWT koruması ekledik

    }
}
