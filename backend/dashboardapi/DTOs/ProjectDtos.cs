namespace dashboardapi.DTOs;

// Proje listesinde (Dashboard kartlarında) gösterilecek özet veriler
public record ProjectSummaryDto(
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string ProjectStatus,
    string ManualHealth,
    decimal PlannedProgress,
    decimal ActualProgress,
    DateTime StartDate,
    DateTime BaselineFinishDate
);

// Proje detay sayfasına girildiğinde gösterilecek tüm veriler
public record ProjectDetailDto(
    string ProjectId,
    string ProjectCode,
    string ProjectName,
    string? ProjectDescription,
    string ProjectStatus,
    string ManualHealth,
    decimal PlannedProgress,
    decimal ActualProgress,
    decimal Bac,
    string Currency,
    DateTime StartDate,
    DateTime BaselineFinishDate,
    DateTime ForecastFinishDate,
    DateTime? ActualFinishDate,
    string ProgramId,
    string CustomerId,
    string ProjectManagerUserId
);