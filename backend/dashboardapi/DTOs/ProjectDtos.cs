namespace dashboardapi.DTOs;

public record ProjectSummaryDto(
    string ProjectId, string ProjectCode, string ProjectName, string ProjectStatus, 
    string ManualHealth, decimal PlannedProgress, decimal ActualProgress, decimal Bac, string Currency,
    DateTime StartDate, DateTime BaselineFinishDate, string? ProjectManagerUserId, string? ProjectManagerFullName, int IsActive
);

public record ProjectDetailDto(
    string ProjectId, string ProjectCode, string ProjectName, string? ProjectDescription, 
    string ProjectStatus, string ManualHealth, string AutoHealthRecommendation, 
    decimal PlannedProgress, decimal ActualProgress, decimal Bac, string Currency, 
    DateTime StartDate, DateTime BaselineFinishDate, DateTime ForecastFinishDate, DateTime? ActualFinishDate, 
    string ProgramId, string CustomerId, string ProjectManagerUserId, string? ProjectManagerFullName, 
    string ReportingFrequency, string Confidentiality, decimal ScheduleVariance, decimal SchedulePerformanceIndex, int IsActive
);

public record ProjectCreateDto(
    string ProjectCode, string ProjectName, string? ProjectDescription, string? ProjectStatus, 
    string? ManualHealth, decimal PlannedProgress, decimal ActualProgress, decimal Bac, 
    string? Currency, DateTime StartDate, DateTime BaselineFinishDate, DateTime ForecastFinishDate, 
    string ProgramId, string CustomerId, string ProjectManagerUserId, string? Confidentiality
);

public record ProjectUpdateDto(
    string? ProjectName, string? ProjectDescription, string? ProjectStatus, string? ManualHealth, 
    decimal? PlannedProgress, decimal? ActualProgress, decimal? Bac, string? Currency, 
    DateTime? ForecastFinishDate, DateTime? ActualFinishDate, string? Confidentiality, string? ProjectManagerUserId, int? IsActive
);