using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;

namespace dashboardapi.Endpoints;

public static class RiskEndpoints
{
    public static void MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/risks -> Projeye Ait Tüm Riskleri Listeleme (vw_risk üzerinden)
        app.MapGet("projects/{id}/risks", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projenin risklerini görmeye yetkili mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == id && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projenin risk verilerini görmeye yetkiniz yok!" }, statusCode: 403);
            }

            // Görünümden ilgili projeye ait riskleri çekiyoruz
            var rawRisks = await db.Set<VwRisk>()
                .Where(r => r.ProjectId == id)
                .ToListAsync();

            // Verileri DTO'ya dönüştürürken byte[] alanını string'e çeviriyoruz
            var result = rawRisks.Select(r => new RiskDto(
                r.RiskId,
                r.ProjectId,
                r.ProjectCode,
                r.ProjectName,
                r.RiskTitle,
                r.RiskCategory,
                r.RiskProbability,
                r.RiskImpact,
                r.RiskScore,
                r.RiskStatus,
                r.RiskDueDate,
                r.RiskOwnerUserId,
                r.RiskOwnerFullName,
                ParseString(r.RiskHealth)
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /risks -> Projeye Yeni Risk Ekleme
        app.MapPost("risks", async (CreateRiskRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu projeye risk ekleyebilir mi?
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && 
                    (p.ProjectManagerUserId == userId || p.ProjectUsers.Any(pu => pu.UserId == userId)));
                
                if (!hasAccess) 
                    return Results.Json(new { message = "Bu projeye risk ekleme yetkiniz yok!" }, statusCode: 403);
            }

            // Yeni risk nesnesini oluşturuyoruz
            var newRisk = new Risk
            {
                RiskId = Guid.NewGuid().ToString(), // Benzersiz string ID üretiyoruz
                ProjectId = request.ProjectId,
                RiskTitle = request.RiskTitle,
                RiskCategory = request.RiskCategory,
                RiskProbability = request.RiskProbability,
                RiskImpact = request.RiskImpact,
                RiskScore = 0, // Önemli değil, veritabanındaki TRIGGER bunu otomatik hesaplayacak!
                RiskOwnerUserId = request.RiskOwnerUserId,
                RiskMitigation = request.RiskMitigation,
                RiskDueDate = request.RiskDueDate,
                RiskStatus = request.RiskStatus,
                OpenedDate = DateTime.UtcNow,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Risk>().Add(newRisk);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Risk başarıyla tanımlandı. Skor veritabanı tarafından hesaplandı.", riskId = newRisk.RiskId }, statusCode: 201);
        });
    }

    // SQLite'tan gelen RiskHealth byte dizisini metne (Kırmızı/Sarı/Yeşil) çeviren sihirbaz
    private static string ParseString(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return "Belirsiz";
        return Encoding.UTF8.GetString(bytes);
    }
}