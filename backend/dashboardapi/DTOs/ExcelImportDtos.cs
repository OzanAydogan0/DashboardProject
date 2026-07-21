namespace dashboardapi.DTOs;

/// <summary>
/// Excel'den toplu proje yükleme işleminin sonuç raporu
/// </summary>
public record ExcelImportResponse(
    bool Success,           // İşlem tamamen başarılı mı? (Hiç hata yoksa true)
    int TotalImported,      // Veritabanına başarıyla eklenen proje sayısı
    int TotalFailed,        // Hata alınan satır sayısı
    List<string> Errors     // Hata detayları (Örn: "Satır 5: PRJ-001 kodlu proje zaten mevcut")
);