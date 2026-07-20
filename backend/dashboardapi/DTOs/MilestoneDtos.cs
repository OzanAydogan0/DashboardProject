namespace dashboardapi.DTOs;

public record MilestoneDto(
    string MilestoneId, string ProjectId, string MilestoneName, DateTime PlannedDate, 
    DateTime ForecastDate, DateTime? ActualDate, string MilestoneStatus, int Critical, 
    string MilestoneOwnerUserId, string MilestoneOwnerFullName, string AcceptanceCriteria, string? MilestoneDescription
);

public record CreateMilestoneRequest(
    string ProjectId, string MilestoneName, DateTime PlannedDate, DateTime ForecastDate, 
    string MilestoneStatus, int Critical, string MilestoneOwnerUserId, string AcceptanceCriteria, string? MilestoneDescription
);

// Yeni Eklenen Güncelleme Paketi
public record UpdateMilestoneRequest(
    string? MilestoneName, DateTime? PlannedDate, DateTime? ForecastDate, DateTime? ActualDate, 
    string? MilestoneStatus, int? Critical, string? MilestoneOwnerUserId, string? AcceptanceCriteria, string? MilestoneDescription
);