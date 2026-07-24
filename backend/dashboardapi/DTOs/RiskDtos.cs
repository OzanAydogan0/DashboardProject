namespace dashboardapi.DTOs;

// Hem listelemede hem detayda kullanılacak tek ve güçlü Risk DTO'su
public record RiskDto(
    string? RiskId,
    string? ProjectId,
    string? ProjectCode,
    string? ProjectName,
    string? RiskTitle,
    string? RiskCategory,
    int? RiskProbability,
    int? RiskImpact,
    int? RiskScore,
    string? RiskStatus,
    DateTime? RiskDueDate,
    string? RiskOwnerUserId,
    string? RiskOwnerFullName,
    string RiskHealth
);

// Yeni risk eklerken frontend'den istenecek minimum veri paketi
public record CreateRiskRequest(
    string ProjectId,
    string RiskTitle,
    string RiskCategory,
    int RiskProbability,
    int RiskImpact,
    string? RiskOwnerUserId,
    string? RiskMitigation,
    DateTime? RiskDueDate,
    string? RiskStatus
);

public record UpdateRiskRequest(
    string? RiskTitle, string? RiskCategory, int? RiskProbability, 
    int? RiskImpact, string? RiskStatus, string? RiskMitigation, DateTime? RiskDueDate
);
