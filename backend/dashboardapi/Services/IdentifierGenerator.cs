using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace dashboardapi.Services;

public static class IdentifierGenerator
{
    public static async Task<string> GenerateAsync<T>(DbSet<T> dbSet, Expression<Func<T, string>> idSelector, string prefix, CancellationToken cancellationToken = default)
        where T : class
    {
        var selectorFunc = idSelector.Compile();

        var existingIds = await dbSet
            .Select(idSelector)
            .Where(id => id != null && id.StartsWith(prefix))
            .ToListAsync(cancellationToken);

        existingIds.AddRange(dbSet.Local.Select(selectorFunc).Where(id => id != null && id.StartsWith(prefix)));

        var maxNumber = existingIds
            .Select(id => ParseNumericSuffix(id!, prefix))
            .Where(number => number.HasValue)
            .Select(number => number!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxNumber + 1):D4}";
    }

    private static int? ParseNumericSuffix(string id, string prefix)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length <= prefix.Length)
            return null;

        var numericPart = id[prefix.Length..];
        return int.TryParse(numericPart, out var number) ? number : null;
    }
}
