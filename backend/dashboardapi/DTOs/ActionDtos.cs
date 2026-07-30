namespace dashboardapi.DTOs;

public record ActionDto(
    string ActionId, string ProjectId, string ActionDescription, string SourceType, string? SourceReference,
    string ActionOwnerUserId, string ActionOwnerFullName, DateTime ActionDueDate, string ActionStatus,
    decimal ActionProgress, string ActionPriority, DateTime? CompletedDate, string? ProjectName,
    string? RiskId = null, string? RiskTitle = null,
    string? IssueId = null, string? IssueTitle = null
);

public record CreateActionRequest(
    string ProjectId, string ActionDescription, string SourceType, string? SourceReference,
    string ActionOwnerUserId, DateTime ActionDueDate, string ActionStatus, decimal ActionProgress, string ActionPriority,
    string? RiskId = null, string? IssueId = null
);

// Yeni Eklenen Güncelleme Paketi
public record UpdateActionRequest(
    string? ActionDescription, string? SourceType, string? SourceReference, string? ActionOwnerUserId,
    DateTime? ActionDueDate, string? ActionStatus, decimal? ActionProgress, string? ActionPriority
);
