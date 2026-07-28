using dashboardapi.Data;
using Microsoft.EntityFrameworkCore;

namespace dashboardapi.Services;

public static class CustomerIdGenerator
{
    public static async Task<string> GenerateAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        return await IdentifierGenerator.GenerateAsync(db.Customers, c => c.CustomerId, "CST-", cancellationToken);
    }
}
