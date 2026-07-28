namespace dashboardapi.DTOs;

public record ActionDto(
    string ActionId, string ProjectId, string ActionDescription, string SourceType, string? SourceReference,
    string ActionOwnerUserId, string ActionOwnerFullName, DateTime ActionDueDate, string ActionStatus,
    decimal ActionProgress, string ActionPriority, DateTime? CompletedDate, string? ProjectName
);

public record CreateActionRequest(
    string ProjectId, string ActionDescription, string SourceType, string? SourceReference,
    string ActionOwnerUserId, DateTime ActionDueDate, string ActionStatus, decimal ActionProgress, string ActionPriority
);

// Yeni Eklenen Güncelleme Paketi
public record UpdateActionRequest(
    string? ActionDescription, string? SourceType, string? SourceReference, string? ActionOwnerUserId,
    DateTime? ActionDueDate, string? ActionStatus, decimal? ActionProgress, string? ActionPriority
);