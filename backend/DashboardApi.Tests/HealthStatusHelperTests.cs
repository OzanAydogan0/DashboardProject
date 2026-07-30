using dashboardapi.Services;

namespace DashboardApi.Tests;

public class HealthStatusHelperTests
{
    [Theory]
    [InlineData("Kırmızı", HealthStatusHelper.Critical)]
    [InlineData("Sarı", HealthStatusHelper.Medium)]
    [InlineData("Yeşil", HealthStatusHelper.Good)]
    [InlineData("Gri", HealthStatusHelper.Uncertain)]
    [InlineData("Kritik", HealthStatusHelper.Critical)]
    [InlineData("Orta", HealthStatusHelper.Medium)]
    [InlineData("İyi", HealthStatusHelper.Good)]
    [InlineData("Belirsiz", HealthStatusHelper.Uncertain)]
    public void Normalize_ReturnsCanonicalHealthStatus(string input, string expected)
    {
        Assert.Equal(expected, HealthStatusHelper.Normalize(input));
    }

    [Theory]
    [InlineData(HealthStatusHelper.Critical, "Kırmızı")]
    [InlineData(HealthStatusHelper.Medium, "Sarı")]
    [InlineData(HealthStatusHelper.Good, "Yeşil")]
    [InlineData(HealthStatusHelper.Uncertain, "Gri")]
    public void ToStorageValue_ReturnsDatabaseCompatibleValue(string input, string expected)
    {
        Assert.Equal(expected, HealthStatusHelper.ToStorageValue(input));
    }
}
