using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using dashboardapi.Data;
using dashboardapi.Endpoints; // Artık bu klasörü kullanıyoruz!
using dashboardapi.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options
        .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

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

builder.Services.AddRateLimiter(options =>
{
    // "LoginLimiter" adında bir politika oluşturuyoruz
    options.AddFixedWindowLimiter(policyName: "LoginLimiter", fixedWindowOptions =>
    {
        fixedWindowOptions.PermitLimit = 5; // 1 dakika içinde en fazla 5 isteğe izin ver
        fixedWindowOptions.Window = TimeSpan.FromMinutes(1); // Zaman penceresi: 1 dakika
        fixedWindowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        fixedWindowOptions.QueueLimit = 0; // Sınır aşılırsa istekleri kuyruğa alma, direkt reddet
    });

    // Sınırı aşan kullanıcılara döneceğimiz hata kodunu ayarlıyoruz
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; 
});

// 5. Yetkilendirme (Authorization) Servisi
builder.Services.AddAuthorization();
// EPPlus Excel Lisans Ayarı (Ticari Olmayan Kullanım)
OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

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
app.UseRateLimiter();

// --- API ENDPOINT'LERİ (GİRİŞ NOKTALARI) ---
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProjectEndpoints();
app.MapDashboardEndpoints();
app.MapRiskEndpoints();
app.MapIssueEndpoints();
app.MapActionEndpoints();
app.MapMilestoneEndpoints();
app.MapPortfolioEndpoints();
app.MapGovernanceEndpoints();
app.MapSystemEndpoints();
app.MapExcelImportEndpoints();
app.MapProjectRecordExcelImportEndpoints();

app.Run();

public partial class Program;
