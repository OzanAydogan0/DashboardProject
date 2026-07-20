namespace dashboardapi.DTOs;

// ==========================================
// 1. EVM (KAZANILMIŞ DEĞER) DTO'ları
// ==========================================
public record EvmRecordDto(
    string EvmRecordId,
    string ProjectId,
    string Period,
    decimal Bac,
    decimal Pv,
    decimal Ev,
    decimal Ac,
    decimal? Sv,
    decimal? Cv,
    decimal? Spi,
    decimal? Cpi,
    decimal? Eac,
    decimal? Vac
);

// Yeni EVM kaydı girerken sadece ana metrikleri (BAC, PV, EV, AC) istiyoruz
public record CreateEvmRecordRequest(
    string ProjectId,
    string Period,
    decimal Bac,
    decimal Pv,
    decimal Ev,
    decimal Ac
);

// ==========================================
// 2. AUDIT LOG (DENETİM İZİ) DTO'ları
// ==========================================
public record AuditLogDto(
    string AuditLogId,
    string? UserId,
    string UserFullName, // Logu kimin oluşturduğunu göstermek için
    string EntityName,
    string EntityId,
    string ActionType,
    string? OldValues,
    string? NewValues,
    DateTime ChangedAt,
    string? IpAddress
);