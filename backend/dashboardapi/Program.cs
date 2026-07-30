using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using dashboardapi.Data;
using dashboardapi.Endpoints;
using dashboardapi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

ConfigureQuestPdfLicense(builder.Configuration, builder.Environment);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
var forwardedHeaderLimit = GetPositiveIntegerSetting(
    builder.Configuration,
    "ReverseProxy:ForwardLimit",
    defaultValue: 1);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = forwardedHeaderLimit;

    var configuredProxies =
        (builder.Configuration["ReverseProxy:KnownProxies"] ?? string.Empty)
        .Split(
            [',', ';'],
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

    foreach (var configuredProxy in configuredProxies)
    {
        if (!IPAddress.TryParse(configuredProxy, out var proxyAddress))
        {
            throw new InvalidOperationException(
                $"ReverseProxy:KnownProxies geçersiz IP içeriyor: '{configuredProxy}'.");
        }

        if (!options.KnownProxies.Contains(proxyAddress))
            options.KnownProxies.Add(proxyAddress);
    }
});

var allowedOrigins = GetAllowedOrigins(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = GetSqliteConnectionString(
    builder.Configuration,
    builder.Environment.ContentRootPath);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options
        .UseSqlite(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = RequireSetting(jwtSettings, "Secret");
var jwtIssuer = RequireSetting(jwtSettings, "Issuer");
var jwtAudience = RequireSetting(jwtSettings, "Audience");

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "JwtSettings:Secret en az 32 bayt uzunluğunda olmalıdır.");
}

if (!double.TryParse(
        jwtSettings["ExpiryMinutes"],
        System.Globalization.NumberStyles.Number,
        System.Globalization.CultureInfo.InvariantCulture,
        out var expiryMinutes) ||
    expiryMinutes <= 0)
{
    throw new InvalidOperationException(
        "JwtSettings:ExpiryMinutes pozitif bir sayı olmalıdır.");
}

var secretKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                    .Value;
                var tokenRole = context.Principal?
                    .FindFirst(System.Security.Claims.ClaimTypes.Role)?
                    .Value;
                var tokenSecurityVersion = context.Principal?
                    .FindFirst("securityVersion")?
                    .Value;

                if (string.IsNullOrWhiteSpace(userId) ||
                    string.IsNullOrWhiteSpace(tokenRole) ||
                    string.IsNullOrWhiteSpace(tokenSecurityVersion))
                {
                    context.Fail("Token kullanıcı bilgileri eksik.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();
                var currentUser = await db.Users
                    .AsNoTracking()
                    .Where(user => user.UserId == userId)
                    .Select(user => new
                    {
                        user.UserRole,
                        user.UserStatus,
                        user.UpdatedAt
                    })
                    .SingleOrDefaultAsync(
                        context.HttpContext.RequestAborted);

                if (currentUser is null ||
                    currentUser.UserStatus != "Aktif" ||
                    PermissionHelper.NormalizeRole(currentUser.UserRole) !=
                    PermissionHelper.NormalizeRole(tokenRole) ||
                    currentUser.UpdatedAt.Ticks.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) !=
                    tokenSecurityVersion)
                {
                    context.Fail(
                        "Kullanıcı veya token güvenlik bilgileri artık güncel değil.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(
        policyName: "LoginLimiter",
        partitioner: httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    httpContext.Connection.RemoteIpAddress?.ToString() ??
                    "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(
    app.Services,
    app.Configuration,
    app.Environment);

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();

app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        // Bağlantının yanı sıra uygulamanın ihtiyaç duyduğu şemanın da mevcut
        // olduğunu doğrular. Boş bir users tablosu sağlıklı kabul edilir.
        _ = await db.Users
            .AsNoTracking()
            .AnyAsync(cancellationToken);

        return Results.Ok(new { status = "Healthy" });
    }
    catch
    {
        return Results.Json(
            new { status = "Unhealthy" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

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

static string RequireSetting(IConfigurationSection section, string key)
{
    var value = section[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"{section.Path}:{key} yapılandırması zorunludur. " +
            "Gizli değerleri ortam değişkeni veya secret store üzerinden sağlayın.");
    }

    return value;
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = (configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToArray();

    if (configuredOrigins.Length == 0)
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins en az bir HTTP/HTTPS origin içermelidir.");
    }

    var origins = new List<string>();
    foreach (var origin in configuredOrigins)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                $"Geçersiz CORS origin değeri: '{origin}'. " +
                "Yol, kullanıcı bilgisi, sorgu veya fragment içermeyen HTTP/HTTPS origin kullanın.");
        }

        origins.Add(uri.GetLeftPart(UriPartial.Authority));
    }

    return origins
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static int GetPositiveIntegerSetting(
    IConfiguration configuration,
    string key,
    int defaultValue)
{
    var configuredValue = configuration[key];
    if (string.IsNullOrWhiteSpace(configuredValue))
        return defaultValue;

    if (!int.TryParse(configuredValue, out var parsedValue) ||
        parsedValue <= 0)
    {
        throw new InvalidOperationException(
            $"{key} pozitif bir tam sayı olmalıdır.");
    }

    return parsedValue;
}

static string GetSqliteConnectionString(
    IConfiguration configuration,
    string contentRootPath)
{
    var configuredConnectionString =
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection yapılandırması zorunludur.");
    }

    var sqlite = new SqliteConnectionStringBuilder(configuredConnectionString);
    if (!string.IsNullOrWhiteSpace(sqlite.DataSource) &&
        sqlite.DataSource != ":memory:" &&
        !Path.IsPathRooted(sqlite.DataSource))
    {
        sqlite.DataSource = Path.GetFullPath(sqlite.DataSource, contentRootPath);
    }

    return sqlite.ToString();
}

static void ConfigureQuestPdfLicense(
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    var isDevelopmentOrTesting =
        environment.IsDevelopment() || environment.IsEnvironment("Testing");

    var questPdfSetting =
        configuration["Licensing:QuestPDF:LicenseType"];
    if (string.IsNullOrWhiteSpace(questPdfSetting))
    {
        if (!isDevelopmentOrTesting)
        {
            throw new InvalidOperationException(
                "Production ortamında Licensing:QuestPDF:LicenseType " +
                "(Community, Professional veya Enterprise) açıkça ayarlanmalıdır.");
        }

        questPdfSetting = nameof(LicenseType.Evaluation);
    }

    if (!Enum.TryParse<LicenseType>(
            questPdfSetting,
            ignoreCase: true,
            out var questPdfLicense) ||
        !Enum.IsDefined(questPdfLicense))
    {
        throw new InvalidOperationException(
            "Licensing:QuestPDF:LicenseType geçerli bir QuestPDF lisans türü olmalıdır.");
    }

    if (environment.IsProduction() &&
        questPdfLicense == LicenseType.Evaluation)
    {
        throw new InvalidOperationException(
            "QuestPDF Evaluation lisansı Production ortamında kullanılamaz. " +
            "Community uygunluğunu doğrulayın veya Professional/Enterprise lisansı sağlayın.");
    }

    QuestPDF.Settings.License = questPdfLicense;
}

public partial class Program;
