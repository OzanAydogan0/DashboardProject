using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;

namespace dashboardapi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("auth");

        // POST /auth/login
        group.MapPost("login", async (LoginRequest request, AppDbContext db, IConfiguration config) =>
        {
            // Veritabanından kullanıcıyı e-posta ile buluyoruz
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            // Basit şifre kontrolü (Şifrelerin düz metin olarak karşılaştırıldığını varsayıyoruz, BCrypt varsa hash'leyip kontrol edebilirsin)
            if (user == null || user.PasswordHash != request.Password)
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
                new Claim("fullName", user.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new LoginResponse(tokenString, user.UserId, user.FullName, user.UserRole));
        });

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