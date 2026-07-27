using dashboardapi.Data;
using Microsoft.EntityFrameworkCore;

namespace dashboardapi.Services;

public static class CustomerIdGenerator
{
    public static async Task<string> GenerateAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existingCustomerIds = await db.Customers
            .Where(c => c.CustomerId != null && c.CustomerId.StartsWith("CST-"))
            .Select(c => c.CustomerId!)
            .ToListAsync(cancellationToken);

        var maxNumber = existingCustomerIds
            .Select(id => int.TryParse(id[4..], out var number) ? number : (int?)null)
            .Where(number => number.HasValue)
            .Select(number => number!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return $"CST-{(maxNumber + 1):D3}";
    }
}
