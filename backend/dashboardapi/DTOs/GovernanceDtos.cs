namespace dashboardapi.DTOs;

// ==========================================
// 1. YÖNETİM KARARLARI (MANAGEMENT DECISIONS)
// ==========================================
public record ManagementDecisionDto(
    string ManagementDecisionId,
    string ProjectId,
    string DecisionTitle,
    string Decision,
    string DecisionOwnerUserId,
    string DecisionOwnerFullName, // Frontend için eklendi
    DateTime DecisionDueDate,
    string DecisionStatus,
    string DecisionImpact,
    string? IfDelayed,
    string? Recommendation,
    DateTime DecisionDate
);

public record CreateManagementDecisionRequest(
    string ProjectId,
    string DecisionTitle,
    string Decision,
    string DecisionOwnerUserId,
    DateTime DecisionDueDate,
    string DecisionStatus,
    string DecisionImpact,
    string? IfDelayed,
    string? Recommendation,
    DateTime DecisionDate
);

// ==========================================
// 2. PIR RAPORLARI (POST IMPLEMENTATION REVIEW)
// ==========================================
public record PirReportDto(
    string? PirReportId,
    string? ProjectId,
    string? ProjectCode,
    string? ProjectName,
    string? Period,
    DateTime? ReportDate,
    string? ExecutiveSummary,
    string? CompletedWork,
    string? Delays,
    string? NextPeriodPlan,
    string? ManagementExpectations,
    string? ManualHealth,
    string? ReportStatus,
    DateTime? PublishedAt
);

public record CreatePirReportRequest(
    string ProjectId,
    string Period,
    DateTime ReportDate,
    string ExecutiveSummary,
    string CompletedWork,
    string? Delays,
    string NextPeriodPlan,
    string? ManagementExpectations,
    string ManualHealth,
    string ReportStatus
);

public record UpdatePirReportRequest(
    string? Period,
    DateTime? ReportDate,
    string? ExecutiveSummary,
    string? CompletedWork,
    string? Delays,
    string? NextPeriodPlan,
    string? ManagementExpectations,
    string? ManualHealth,
    string? ReportStatus
);