using Microsoft.EntityFrameworkCore; // 1. Adım: EF Core namespace'ini ekledik
using dashboardapi.Data; // 2. Adım: AppDbContext'in bulunduğu klasörün namespace'i (Kendine göre düzenle)

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React'in (Vite) varsayılan portu
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Adım: DbContext ve SQLite konfigürasyonunu servis konteynerine ekliyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseCors("AllowReactApp"); // Bunun MapGet/MapPost komutlarından ÖNCE eklenmesi gerekir!

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// 1. Tüm Kullanıcıları Listele (SQL'de eklediğin 'Ahmet Yılmaz' buraya gelecek)
app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
});

// 2. Ana Dashboard Görünümünü Listele (Oluşturduğun vw_dashboard View'ı)
app.MapGet("/api/dashboard", async (AppDbContext db) =>
{
    var dashboardData = await db.VwDashboards.ToListAsync();
    return Results.Ok(dashboardData);
});

// 3. Risk Görünümünü Listele (Oluşturduğun vw_risk View'ı)
app.MapGet("/api/risks", async (AppDbContext db) =>
{
    var risks = await db.VwRisks.ToListAsync();
    return Results.Ok(risks);
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}