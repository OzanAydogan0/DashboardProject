namespace dashboardapi.DTOs;

// Ana sayfadaki büyük özet tablosu için tertemiz DTO
public record DashboardSummaryDto(
    string? ProjectId,
    string? ProjectCode,
    string? ProjectName,
    string? ProjectStatus,
    string? ManualHealth,
    decimal? PlannedProgress,
    decimal? ActualProgress,
    DateTime? BaselineFinishDate,
    DateTime? ForecastFinishDate,
    decimal? Bac,
    string? Currency,
    int OpenRiskCount,
    int OpenIssueCount,
    int OpenActionCount,
    int OpenMilestoneCount,
    string LatestEvmPeriod
);

// Proje bazlı Kazanılmış Değer Analizi (EVM) grafik verisi için DTO
public record EvmPerformanceDto(
    string? EvmRecordId,
    string? ProjectId,
    string? ProjectCode,
    string? ProjectName,
    string? Period,
    decimal? Bac,
    decimal? Pv,
    decimal? Ev,
    decimal? Ac,
    decimal Sv,
    decimal Cv,
    decimal Spi,
    decimal Cpi,
    decimal Eac,
    decimal Vac
);