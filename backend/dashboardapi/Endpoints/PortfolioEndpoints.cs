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
    private static readonly HashSet<string> AllowedStatuses = ["Aktif", "Pasif"];

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
            var userRole = PermissionHelper.GetUserRole(userClaims);
            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Müşteri ekleme yetkiniz yok!" }, statusCode: 403);

            var customerName = request.CustomerName?.Trim();
            var customerType = request.CustomerType?.Trim();
            var customerStatus = request.CustomerStatus?.Trim();
            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(customerType) ||
                customerStatus is null ||
                !AllowedStatuses.Contains(customerStatus))
            {
                return Results.BadRequest(new
                {
                    message = "Müşteri adı/türü zorunludur ve durum Aktif veya Pasif olmalıdır."
                });
            }

            var customerId = await CustomerIdGenerator.GenerateAsync(db);

            var newCustomer = new Customer
            {
                CustomerId = customerId,
                CustomerName = customerName,
                CustomerType = customerType,
                CustomerStatus = customerStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<Customer>().Add(newCustomer);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Müşteri başarıyla eklendi.", customerId = newCustomer.CustomerId }, statusCode: 201);
        });

        app.MapDelete("customers/{id}", async (string id, ClaimsPrincipal userClaims, AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            if (!PermissionHelper.IsSystemAdmin(userRole))
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
            var userRole = PermissionHelper.GetUserRole(userClaims);
            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Müşteri güncelleme yetkiniz yok!" }, statusCode: 403);

            var customerName = request.CustomerName?.Trim();
            var customerType = request.CustomerType?.Trim();
            var customerStatus = request.CustomerStatus?.Trim();
            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(customerType) ||
                customerStatus is null ||
                !AllowedStatuses.Contains(customerStatus))
            {
                return Results.BadRequest(new
                {
                    message = "Müşteri adı/türü zorunludur ve durum Aktif veya Pasif olmalıdır."
                });
            }

            var customer = await db.Set<Customer>().FindAsync(id);
            if (customer == null)
                return Results.NotFound(new { message = "Müşteri bulunamadı." });

            customer.CustomerName = customerName;
            customer.CustomerType = customerType;
            customer.CustomerStatus = customerStatus;
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
            var userRole = PermissionHelper.GetUserRole(userClaims);
            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Program/Portföy ekleme yetkiniz yok!" }, statusCode: 403);

            var programName = request.ProgramName?.Trim();
            var programDescription = request.ProgramDescription?.Trim();
            var programStatus = request.ProgramStatus?.Trim();
            if (string.IsNullOrWhiteSpace(programName) ||
                programStatus is null ||
                !AllowedStatuses.Contains(programStatus))
            {
                return Results.BadRequest(new
                {
                    message = "Program adı zorunludur ve durum Aktif veya Pasif olmalıdır."
                });
            }

            var duplicateName = await db.Set<dashboardapi.Models.Program>()
                .AnyAsync(program =>
                    program.ProgramName.ToLower() == programName.ToLower());
            if (duplicateName)
            {
                return Results.Conflict(new
                {
                    message = "Bu program adı zaten kullanımda."
                });
            }

            var newProgram = new dashboardapi.Models.Program
            {
                ProgramId = await IdentifierGenerator.GenerateAsync(db.Set<dashboardapi.Models.Program>(), p => p.ProgramId, "PRG-"),
                ProgramName = programName,
                ProgramDescription = string.IsNullOrWhiteSpace(programDescription)
                    ? null
                    : programDescription,
                ProgramStatus = programStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<dashboardapi.Models.Program>().Add(newProgram);
            await db.SaveChangesAsync();

            return Results.Json(new { message = "Program başarıyla oluşturuldu.", programId = newProgram.ProgramId }, statusCode: 201);
        });

        app.MapPatch("programs/{id}", async (
            string id,
            CreateProgramRequest request,
            ClaimsPrincipal userClaims,
            AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Program güncelleme yetkiniz yok!" }, statusCode: 403);

            var programName = request.ProgramName?.Trim();
            var programDescription = request.ProgramDescription?.Trim();
            var programStatus = request.ProgramStatus?.Trim();
            if (string.IsNullOrWhiteSpace(programName) ||
                programStatus is null ||
                !AllowedStatuses.Contains(programStatus))
            {
                return Results.BadRequest(new
                {
                    message = "Program adı zorunludur ve durum Aktif veya Pasif olmalıdır."
                });
            }

            var program = await db.Set<dashboardapi.Models.Program>()
                .FindAsync(id);
            if (program is null)
                return Results.NotFound(new { message = "Program bulunamadı." });

            var duplicateName = await db.Set<dashboardapi.Models.Program>()
                .AnyAsync(candidate =>
                    candidate.ProgramId != id &&
                    candidate.ProgramName.ToLower() == programName.ToLower());
            if (duplicateName)
            {
                return Results.Conflict(new
                {
                    message = "Bu program adı zaten kullanımda."
                });
            }

            program.ProgramName = programName;
            program.ProgramDescription = string.IsNullOrWhiteSpace(programDescription)
                ? null
                : programDescription;
            program.ProgramStatus = programStatus;
            program.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Program başarıyla güncellendi." });
        });

        app.MapDelete("programs/{id}", async (
            string id,
            ClaimsPrincipal userClaims,
            AppDbContext db) =>
        {
            var userRole = PermissionHelper.GetUserRole(userClaims);
            if (!PermissionHelper.IsSystemAdmin(userRole))
                return Results.Json(new { message = "Program silme yetkiniz yok!" }, statusCode: 403);

            var program = await db.Set<dashboardapi.Models.Program>()
                .FindAsync(id);
            if (program is null)
                return Results.NotFound(new { message = "Program bulunamadı." });

            var hasProjects = await db.Projects
                .AnyAsync(project => project.ProgramId == id);
            if (hasProjects)
            {
                return Results.Conflict(new
                {
                    message = "Bu programa bağlı projeler var. Programı silmek yerine pasife alın."
                });
            }

            db.Remove(program);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Program başarıyla silindi." });
        });
    }
}
