namespace dashboardapi.DTOs;

public record IssueDto(
    string IssueId, string ProjectId, string IssueTitle, string IssuePriority, string IssueOwnerUserId,
    string IssueOwnerFullName, DateTime IssueDueDate, string IssueStatus, string IssueImpact,
    string? RootCause, string? IssueResolution, DateTime OpenedDate, DateTime? ClosedDate,
    string? RiskId = null, string? RiskTitle = null
);

public record CreateIssueRequest(
    string ProjectId, string IssueTitle, string IssuePriority, string IssueOwnerUserId,
    DateTime IssueDueDate, string IssueStatus, string IssueImpact, string? RootCause,
    string? RiskId = null
);

// Yeni Eklenen Güncelleme Paketi
public record UpdateIssueRequest(
    string? IssueTitle, string? IssuePriority, string? IssueOwnerUserId,
    DateTime? IssueDueDate, string? IssueStatus, string? IssueImpact,
    string? RootCause, string? IssueResolution
);
