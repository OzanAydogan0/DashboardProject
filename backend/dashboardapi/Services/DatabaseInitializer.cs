using System.Data;
using System.Net.Mail;
using dashboardapi.Data;
using dashboardapi.Models;
using Microsoft.EntityFrameworkCore;

namespace dashboardapi.Services;

public static class DatabaseInitializer
{
    private const string BootstrapSection = "BootstrapAdmin";

    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        await db.Database.EnsureCreatedAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            ViewDefinitions,
            cancellationToken);

        if (await db.Users.AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        var section = configuration.GetSection(BootstrapSection);
        var email = section["Email"]?.Trim();
        var fullName = section["FullName"]?.Trim();
        var password = section["Password"];

        var suppliedValueCount = new[] { email, fullName, password }
            .Count(value => !string.IsNullOrWhiteSpace(value));

        if (suppliedValueCount == 0)
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Veritabanında kullanıcı yok. İlk yöneticiyi oluşturmak için " +
                    "BootstrapAdmin:Email, BootstrapAdmin:FullName ve " +
                    "BootstrapAdmin:Password ayarlarının tamamı zorunludur.");
            }

            logger.LogWarning(
                "Veritabanında kullanıcı yok ve bootstrap yönetici ayarları sağlanmadı.");
            return;
        }

        if (suppliedValueCount != 3)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Email, BootstrapAdmin:FullName ve " +
                "BootstrapAdmin:Password ayarları birlikte sağlanmalıdır.");
        }

        ValidateEmail(email!);
        ValidateFullName(fullName!);
        ValidatePassword(password!);

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        // Birden fazla süreç aynı volume üzerinde eşzamanlı başlarsa ikinci
        // süreç yönetici eklememelidir.
        if (await db.Users.AnyAsync(cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;
        var administrator = new User
        {
            UserId = await IdentifierGenerator.GenerateAsync(
                db.Users,
                user => user.UserId,
                "USR-",
                cancellationToken),
            Email = email!.ToLowerInvariant(),
            FullName = fullName!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                password!,
                workFactor: 12),
            UserRole = PermissionHelper.SystemAdminRole,
            UserStatus = "Aktif",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(administrator);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "İlk sistem yöneticisi güvenli bootstrap ile oluşturuldu. UserId: {UserId}",
            administrator.UserId);
    }

    private static void ValidateEmail(string email)
    {
        if (!MailAddress.TryCreate(email, out var address) ||
            !string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Email geçerli bir e-posta adresi olmalıdır.");
        }
    }

    private static void ValidateFullName(string fullName)
    {
        if (fullName.Length is < 2 or > 200)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:FullName 2 ile 200 karakter arasında olmalıdır.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (!PasswordPolicy.IsStrong(password))
        {
            throw new InvalidOperationException(
                $"BootstrapAdmin:Password geçersiz. {PasswordPolicy.RequirementsMessage}");
        }
    }

    private const string ViewDefinitions = """
        CREATE VIEW IF NOT EXISTS vw_dashboard AS
        SELECT
            p.project_id,
            p.project_code,
            p.project_name,
            p.project_status,
            p.manual_health,
            p.planned_progress,
            p.actual_progress,
            p.baseline_finish_date,
            p.forecast_finish_date,
            p.bac,
            p.currency,
            COUNT(DISTINCT CASE WHEN r.risk_status <> 'Kapalı' THEN r.risk_id END) AS open_risk_count,
            COUNT(DISTINCT CASE WHEN i.issue_status <> 'Kapalı' THEN i.issue_id END) AS open_issue_count,
            COUNT(DISTINCT CASE WHEN a.action_status <> 'Tamamlandı' THEN a.action_id END) AS open_action_count,
            COUNT(DISTINCT CASE WHEN m.milestone_status <> 'Tamamlandı' THEN m.milestone_id END) AS open_milestone_count,
            MAX(e.period) AS latest_evm_period
        FROM projects p
        LEFT JOIN risks r ON r.project_id = p.project_id
        LEFT JOIN issues i ON i.project_id = p.project_id
        LEFT JOIN actions a ON a.project_id = p.project_id
        LEFT JOIN milestones m ON m.project_id = p.project_id
        LEFT JOIN evm_records e ON e.project_id = p.project_id
        WHERE p.is_active = 1
        GROUP BY p.project_id;

        CREATE VIEW IF NOT EXISTS vw_risk AS
        SELECT
            r.risk_id,
            r.project_id,
            p.project_code,
            p.project_name,
            r.risk_title,
            r.risk_category,
            r.risk_probability,
            r.risk_impact,
            r.risk_score,
            r.risk_status,
            r.risk_due_date,
            r.risk_owner_user_id,
            u.full_name AS risk_owner_full_name,
            CASE
                WHEN r.risk_score BETWEEN 1 AND 4 THEN 'Yeşil'
                WHEN r.risk_score BETWEEN 5 AND 15 THEN 'Sarı'
                ELSE 'Kırmızı'
            END AS risk_health
        FROM risks r
        JOIN projects p ON p.project_id = r.project_id
        JOIN users u ON u.user_id = r.risk_owner_user_id;

        CREATE VIEW IF NOT EXISTS vw_pir AS
        SELECT
            pr.pir_report_id,
            pr.project_id,
            p.project_code,
            p.project_name,
            pr.period,
            pr.report_date,
            pr.executive_summary,
            pr.completed_work,
            pr.delays,
            pr.next_period_plan,
            pr.management_expectations,
            pr.manual_health,
            pr.report_status,
            pr.published_at,
            pr.published_by_user_id
        FROM pir_reports pr
        JOIN projects p ON p.project_id = pr.project_id;

        CREATE VIEW IF NOT EXISTS vw_evm AS
        SELECT
            e.evm_record_id,
            e.project_id,
            p.project_code,
            p.project_name,
            e.period,
            e.bac,
            e.pv,
            e.ev,
            e.ac,
            (e.ev - e.pv) AS sv,
            (e.ev - e.ac) AS cv,
            CASE WHEN e.pv = 0 THEN NULL ELSE ROUND((1.0 * e.ev) / e.pv, 4) END AS spi,
            CASE WHEN e.ac = 0 THEN NULL ELSE ROUND((1.0 * e.ev) / e.ac, 4) END AS cpi,
            CASE WHEN e.ac = 0 OR e.ev = 0 THEN NULL ELSE ROUND((1.0 * e.bac * e.ac) / e.ev, 2) END AS eac,
            CASE WHEN e.ac = 0 OR e.ev = 0 THEN NULL ELSE ROUND(e.bac - ((1.0 * e.bac * e.ac) / e.ev), 2) END AS vac
        FROM evm_records e
        JOIN projects p ON p.project_id = e.project_id;
        """;
}
