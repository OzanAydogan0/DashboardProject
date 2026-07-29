using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models;
using dashboardapi.Services;

namespace dashboardapi.Endpoints;

public static class MilestoneEndpoints
{
    public static void MapMilestoneEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /projects/{id}/milestones -> Projeye Ait Tüm Kilometre Taşlarını Listeleme
        app.MapGet("projects/{id}/milestones", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!await PermissionHelper.CanAccessProjectAsync(db, id, userId, userRole))
                return Results.Json(new { message = "Bu projenin kilometre taşlarını görmeye yetkiniz yok!" }, statusCode: 403);

            var milestones = await db.Set<Milestone>()
                .Include(m => m.MilestoneOwnerUser)
                .Where(m => m.ProjectId == id && m.MilestoneStatus != "Silindi")
                .OrderBy(m => m.PlannedDate) // Timeline görünümü için kronolojik sıralama
                .ToListAsync();

            var result = milestones.Select(m => new MilestoneDto(
                m.MilestoneId,
                m.ProjectId,
                m.MilestoneName,
                m.PlannedDate,
                m.ForecastDate,
                m.ActualDate,
                m.MilestoneStatus,
                m.Critical,
                m.MilestoneOwnerUserId,
                m.MilestoneOwnerUser?.FullName ?? "Atanmamış",
                m.AcceptanceCriteria,
                m.MilestoneDescription
            )).ToList();

            return Results.Ok(result);
        });

        // 2. POST /projects/{projectId}/milestones -> Projeye Yeni Kilometre Taşı Ekleme
        app.MapPost("projects/{projectId}/milestones", async (string projectId, CreateMilestoneRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolünün sisteme kilometre taşı ekleme yetkisi yoktur!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, projectId, userId, userRole))
                return Results.Json(new { message = "Bu projeye kilometre taşı ekleme yetkiniz yok!" }, statusCode: 403);

            // GÜVENLİK AĞI: Eğer frontend'den Sahip ID gelmediyse veya boşsa, işlemi yapan kişiyi(userId) ata.
            // Bu sayede veritabanındaki 'NOT NULL' (Boş Olamaz) hatasını önlüyoruz.
            string ownerId = string.IsNullOrWhiteSpace(request.MilestoneOwnerUserId) 
                ? userId 
                : request.MilestoneOwnerUserId;

            var newMilestone = new Milestone
            {
                MilestoneId = await IdentifierGenerator.GenerateAsync(db.Set<Milestone>(), m => m.MilestoneId, "MS-"),
                ProjectId = projectId, 
                MilestoneName = request.MilestoneName,
                PlannedDate = request.PlannedDate,
                ForecastDate = request.ForecastDate,
                ActualDate = request.ActualDate,
                MilestoneStatus = string.IsNullOrWhiteSpace(request.MilestoneStatus) ? "Planlandı" : request.MilestoneStatus,
                Critical = request.Critical,
                
                // GÜNCELLENEN KISIM BURASI: Artık doğrudan request'ten almak yerine yukarıda belirlediğimiz güvenli ownerId'yi kullanıyoruz.
                MilestoneOwnerUserId = ownerId, 
                
                AcceptanceCriteria = request.AcceptanceCriteria,
                MilestoneDescription = request.MilestoneDescription,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Milestone>().Add(newMilestone);
            await db.SaveChangesAsync(); // Artık burada veritabanı hata fırlatmayacak.

            return Results.Json(new { message = "Kilometre taşı başarıyla oluşturuldu.", milestoneId = newMilestone.MilestoneId }, statusCode: 201);
        });

        // 3. PATCH /milestones/{id} -> Kilometre Taşı Güncelleme 
        app.MapPatch("milestones/{id}", async (string id, UpdateMilestoneRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            var userId = PermissionHelper.GetUserId(userClaims);

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var milestone = await db.Set<Milestone>().FindAsync(id);
            if (milestone == null) return Results.NotFound(new { message = "Kilometre taşı bulunamadı." });

            if (PermissionHelper.IsExecutive(userRole))
                return Results.Json(new { message = "Üst Yönetim rolü kilometre taşları üzerinde değişiklik yapamaz!" }, statusCode: 403);

            if (!await PermissionHelper.CanWriteProjectAsync(db, milestone.ProjectId, userId, userRole))
                return Results.Json(new { message = "Bu projenin kilometre taşını güncelleme yetkiniz yok!" }, statusCode: 403);

            // Alanları güvenli şekilde güncelleme havuzu
            if (!string.IsNullOrEmpty(request.MilestoneName)) milestone.MilestoneName = request.MilestoneName;
            if (request.PlannedDate.HasValue) milestone.PlannedDate = request.PlannedDate.Value;
            if (request.ForecastDate.HasValue) milestone.ForecastDate = request.ForecastDate.Value;
            if (request.ActualDate.HasValue) milestone.ActualDate = request.ActualDate.Value;
            if (!string.IsNullOrEmpty(request.MilestoneStatus)) milestone.MilestoneStatus = request.MilestoneStatus;
            if (request.Critical.HasValue) milestone.Critical = request.Critical.Value;
            if (!string.IsNullOrEmpty(request.MilestoneOwnerUserId)) milestone.MilestoneOwnerUserId = request.MilestoneOwnerUserId;
            if (!string.IsNullOrEmpty(request.AcceptanceCriteria)) milestone.AcceptanceCriteria = request.AcceptanceCriteria;
            if (request.MilestoneDescription != null) milestone.MilestoneDescription = request.MilestoneDescription;

            milestone.UpdatedByUserId = userId;
            milestone.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Kilometre taşı başarıyla güncellendi." });
        });

        // 4. DELETE /milestones/{id} -> Kilometre Taşı İptal Etme (Soft Delete)
        app.MapDelete("milestones/{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            try
            {
                // 1. KULLANICI BİLGİLERİNİ OKUMA VE OTURUM KONTROLÜ
                var userRole = PermissionHelper.GetUserRole(userClaims);
                var userId = PermissionHelper.GetUserId(userClaims);

                // Oturum açılmamışsa veya Token geçersizse 401 dön (Veritabanı çökmesini engeller)
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                // 2. KİLOMETRE TAŞI KONTROLÜ
                var milestone = await db.Set<Milestone>().FindAsync(id);
                if (milestone == null)
                {
                    // Mesaj güncellendi
                    return Results.NotFound(new { message = "İptal edilmek istenen kilometre taşı veritabanında bulunamadı." });
                }

                // 3. YETKİ KONTROLÜ
                if (PermissionHelper.IsExecutive(userRole))
                {
                    return Results.Json(new { message = "Üst Yönetim rolü iptal işlemi yapamaz!" }, statusCode: 403);
                }

                if (!await PermissionHelper.CanWriteProjectAsync(db, milestone.ProjectId, userId, userRole))
                {
                    return Results.Json(new { message = "Bu projenin kilometre taşını iptal etme yetkiniz yok!" }, statusCode: 403);
                }

                // 4. SOFT DELETE İŞLEMİ (Durumu İptal Olarak Güncelleme)
                milestone.MilestoneStatus = "İptal"; // Hatanın çözüldüğü asıl nokta burası
                milestone.UpdatedByUserId = userId; // İşlemi kimin yaptığını kaydediyoruz
                milestone.UpdatedAt = DateTime.UtcNow; // İşlem zamanını güncelliyoruz

                // 5. VERİTABANINA KAYDETME
                await db.SaveChangesAsync();

                // Başarı mesajı güncellendi
                return Results.Ok(new { message = "Kilometre taşı başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                // Terminal ekranına hatanın detayını yazdıralım (Log başlığı güncellendi)
                Console.WriteLine($"[CANCEL MILESTONE ERROR]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[INNER ERROR]: {ex.InnerException.Message}");
                }

                return Results.Json(new { 
                    message = "İptal işlemi sırasında veritabanı hatası oluştu.", 
                    error = ex.InnerException?.Message ?? ex.Message 
                }, statusCode: 500);
            }
        });
    }
}
