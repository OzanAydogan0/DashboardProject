namespace dashboardapi.DTOs;

// Liste ve detay için ana Kilometre Taşı DTO'su
public record MilestoneDto(
    string MilestoneId,
    string ProjectId,
    string MilestoneName,
    DateTime PlannedDate,
    DateTime ForecastDate,
    DateTime? ActualDate,
    string MilestoneStatus,
    int Critical,
    string MilestoneOwnerUserId,
    string MilestoneOwnerFullName, // Frontend'de göstermek için
    string AcceptanceCriteria,
    string? MilestoneDescription
);

// Yeni kilometre taşı oluştururken beklenen veriler
public record CreateMilestoneRequest(
    string ProjectId,
    string MilestoneName,
    DateTime PlannedDate,
    DateTime ForecastDate,
    string MilestoneStatus,
    int Critical,
    string MilestoneOwnerUserId,
    string AcceptanceCriteria,
    string? MilestoneDescription
);