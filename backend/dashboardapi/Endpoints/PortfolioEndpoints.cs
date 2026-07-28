using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Data;
using dashboardapi.DTOs;
using dashboardapi.Models; // Customer için
using dashboardapi.Services;
// Program için dashboardapi.Models.Program kullanacağız!

namespace dashboardapi.Endpoints;

public static class PortfolioEndpoints
{
    public static void MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        // ==========================================
        // 1. MÜŞTERİ (CUSTOMER) UÇ NOKTALARI
        // ==========================================
        
        app.MapGet("customers", async (AppDbContext db) =>
        {
            var customers = await db.Set<Customer>().ToListAsync();
            var result = customers.Select(c => new CustomerDto(
                c.CustomerId, c.CustomerName, c.CustomerType, c.CustomerStatus
            )).ToList();

            return Results.Ok(result);
        });

        app.MapPost("customers", async (CreateCustomerRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
                return Results.Json(new { message = "Müşteri ekleme yetkiniz yok!" }, statusCode: 403);

            var customerId = await CustomerIdGenerator.GenerateAsync(db);

            var newCustomer = new Customer
            {
                CustomerId = customerId,
                CustomerName = request.CustomerName,
                CustomerType = request.CustomerType,
                CustomerStatus = request.CustomerStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Customer>().Add(newCustomer);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Müşteri başarıyla eklendi.", customerId = newCustomer.CustomerId }, statusCode: 201);
        });

        app.MapDelete("customers/{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
                return Results.Json(new { message = "Müşteri silme yetkiniz yok!" }, statusCode: 403);

            var customer = await db.Set<Customer>().FindAsync(id);
            if (customer == null)
                return Results.NotFound(new { message = "Müşteri bulunamadı." });

            var hasProjects = await db.Set<Project>().AnyAsync(p => p.CustomerId == id);
            if (hasProjects)
                return Results.Conflict(new { message = "Bu müşteriye bağlı projeler var. Önce projeleri başka bir müşteriye taşıyın veya silin." });

            db.Set<Customer>().Remove(customer);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Müşteri başarıyla silindi." });
        });

        app.MapPatch("customers/{id}", async (string id, CreateCustomerRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
                return Results.Json(new { message = "Müşteri güncelleme yetkiniz yok!" }, statusCode: 403);

            var customer = await db.Set<Customer>().FindAsync(id);
            if (customer == null)
                return Results.NotFound(new { message = "Müşteri bulunamadı." });

            customer.CustomerName = request.CustomerName;
            customer.CustomerType = request.CustomerType;
            customer.CustomerStatus = request.CustomerStatus;
            customer.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Müşteri başarıyla güncellendi." });
        });

        // ==========================================
        // 2. PROGRAM (PORTFÖY) UÇ NOKTALARI
        // ==========================================

        app.MapGet("programs", async (AppDbContext db) =>
        {
            // DİKKAT: dashboardapi.Models.Program ile uygulamanın kendi Program.cs çakışmasını önlüyoruz
            var programs = await db.Set<dashboardapi.Models.Program>().ToListAsync();
            var result = programs.Select(p => new ProgramDto(
                p.ProgramId, p.ProgramName, p.ProgramDescription, p.ProgramStatus
            )).ToList();

            return Results.Ok(result);
        });

        app.MapPost("programs", async (CreateProgramRequest request, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Sistem Yöneticisi" && userRole != "Üst Yönetim")
                return Results.Json(new { message = "Program/Portföy ekleme yetkiniz yok!" }, statusCode: 403);

            var newProgram = new dashboardapi.Models.Program
            {
                ProgramId = Guid.NewGuid().ToString(),
                ProgramName = request.ProgramName,
                ProgramDescription = request.ProgramDescription,
                ProgramStatus = request.ProgramStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<dashboardapi.Models.Program>().Add(newProgram);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Program başarıyla oluşturuldu.", programId = newProgram.ProgramId }, statusCode: 201);
        });
    }
}