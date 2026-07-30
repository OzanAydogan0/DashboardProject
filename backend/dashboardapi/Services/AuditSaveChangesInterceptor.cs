using System.Security.Claims;
using System.Text.Json;
using dashboardapi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace dashboardapi.Services;

public sealed class AuditSaveChangesInterceptor(
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? db)
    {
        if (db is null)
        {
            return;
        }

        db.ChangeTracker.DetectChanges();

        var changedEntries = db.ChangeTracker
            .Entries()
            .Where(entry =>
                entry.Entity is not AuditLog &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (changedEntries.Count == 0)
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var changedAt = DateTime.UtcNow;

        foreach (var entry in changedEntries)
        {
            var (oldValues, newValues) = GetChangedValues(entry);

            db.Set<AuditLog>().Add(new AuditLog
            {
                AuditLogId = $"LOG-{Guid.NewGuid():N}",
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                EntityName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
                EntityId = GetEntityId(entry),
                ActionType = GetActionType(entry.State),
                OldValues = SerializeValues(oldValues),
                NewValues = SerializeValues(newValues),
                ChangedAt = changedAt,
                IpAddress = ipAddress
            });
        }
    }

    private static (
        Dictionary<string, object?> OldValues,
        Dictionary<string, object?> NewValues) GetChangedValues(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;

            if (entry.State == EntityState.Added)
            {
                newValues[propertyName] = SanitizeValue(propertyName, property.CurrentValue);
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                oldValues[propertyName] = SanitizeValue(propertyName, property.OriginalValue);
                continue;
            }

            if (!property.IsModified)
            {
                continue;
            }

            oldValues[propertyName] = SanitizeValue(propertyName, property.OriginalValue);
            newValues[propertyName] = SanitizeValue(propertyName, property.CurrentValue);
        }

        return (oldValues, newValues);
    }

    private static object? SanitizeValue(string propertyName, object? value)
    {
        return propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase)
            ? "[GİZLENDİ]"
            : value;
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
        {
            return "Bilinmiyor";
        }

        var values = primaryKey.Properties
            .Select(property =>
            {
                var propertyEntry = entry.Property(property.Name);
                var value = entry.State == EntityState.Deleted
                    ? propertyEntry.OriginalValue
                    : propertyEntry.CurrentValue;

                return value?.ToString();
            })
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(",", values);
    }

    private static string GetActionType(EntityState state) =>
        state switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

    private static string? SerializeValues(Dictionary<string, object?> values) =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
}
