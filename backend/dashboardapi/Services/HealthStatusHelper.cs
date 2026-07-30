using System.Globalization;

namespace dashboardapi.Services;

public static class HealthStatusHelper
{
    public const string Critical = "Kritik";
    public const string Medium = "Orta";
    public const string Good = "İyi";
    public const string Uncertain = "Belirsiz";

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string Normalize(string? value, string fallback = Uncertain)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Trim().ToLower(TurkishCulture) switch
        {
            "kırmızı" or "kirmizi" or "kritik" or "red" => Critical,
            "sarı" or "sari" or "orta" or "yellow" => Medium,
            "yeşil" or "yesil" or "iyi" or "düşük" or "dusuk" or "green" => Good,
            "gri" or "gray" or "grey" or "belirsiz" or "unknown" => Uncertain,
            _ => fallback
        };
    }

    // The current database CHECK constraints still use the legacy color values.
    public static string ToStorageValue(string? value, string fallback = Uncertain) =>
        Normalize(value, fallback) switch
        {
            Critical => "Kırmızı",
            Medium => "Sarı",
            Good => "Yeşil",
            _ => "Gri"
        };
}
