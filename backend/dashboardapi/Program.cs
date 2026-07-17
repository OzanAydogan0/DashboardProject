using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using dashboardapi.Data;
using dashboardapi.Endpoints; // Artık bu klasörü kullanıyoruz!

var builder = WebApplication.CreateBuilder(args);

// 1. API Dokümantasyonu (OpenAPI / Swagger) Desteği
builder.Services.AddOpenApi();

// 2. CORS Ayarı (React frontend uygulamasının bağlanabilmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React'in (Vite) varsayılan portu
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Veritabanı (SQLite) Bağlantı Servisi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. JWT Kimlik Doğrulama Servislerinin Eklenmesi
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Yetkilendirme (Authorization) Servisi
builder.Services.AddAuthorization();

var app = builder.Build();

// 6. CORS Aktifleştirme (Giriş noktalarından ÖNCE olmalıdır)
app.UseCors("AllowReactApp");

// 7. Kimlik Doğrulama ve Yetkilendirme Middleware'leri (CORS'tan sonra, Endpoint'lerden önce olmalı!)
app.UseAuthentication();
app.UseAuthorization();

// 9. Geliştirme Ortamı Ayarları
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- API ENDPOINT'LERİ (GİRİŞ NOKTALARI) ---
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProjectEndpoints();
app.MapDashboardEndpoints();
app.MapRiskEndpoints();
app.Run();