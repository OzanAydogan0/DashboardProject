namespace dashboardapi.DTOs;

// Liste ve detay için ana Aksiyon DTO'su
public record ActionDto(
    string ActionId,
    string ProjectId,
    string ActionDescription,
    string SourceType,
    string? SourceReference,
    string ActionOwnerUserId,
    string ActionOwnerFullName, // Frontend'de göstermek için User tablosundan alacağız
    DateTime ActionDueDate,
    string ActionStatus,
    decimal ActionProgress,
    string ActionPriority,
    DateTime? CompletedDate
);

// Yeni aksiyon oluştururken beklenen veriler
public record CreateActionRequest(
    string ProjectId,
    string ActionDescription,
    string SourceType,
    string? SourceReference,
    string ActionOwnerUserId,
    DateTime ActionDueDate,
    string ActionStatus,
    decimal ActionProgress,
    string ActionPriority
);