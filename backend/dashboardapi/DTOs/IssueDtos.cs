namespace dashboardapi.DTOs;

// Hem listelemede hem detayda React'e fırlatacağımız zengin Sorun veri paketi
public record IssueDto(
    string IssueId,
    string ProjectId,
    string IssueTitle,
    string IssuePriority,
    string IssueOwnerUserId,
    string IssueOwnerFullName, // React ekranında "Ad Soyad" göstermek için ekledik
    DateTime IssueDueDate,
    string IssueStatus,
    string IssueImpact,
    string? RootCause,
    string? IssueResolution,
    DateTime OpenedDate,
    DateTime? ClosedDate
);

// Yeni sorun açılırken frontend'den beklediğimiz girdi paketi
public record CreateIssueRequest(
    string ProjectId,
    string IssueTitle,
    string IssuePriority,
    string IssueOwnerUserId,
    DateTime IssueDueDate,
    string IssueStatus,
    string IssueImpact,
    string? RootCause
);