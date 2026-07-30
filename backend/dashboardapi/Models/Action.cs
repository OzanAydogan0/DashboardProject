using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class Action
{
    public string ActionId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string? RiskId { get; set; }

    public string? IssueId { get; set; }

    public string ActionDescription { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public string? SourceReference { get; set; }

    public string ActionOwnerUserId { get; set; } = null!;

    public DateTime ActionDueDate { get; set; }

    public string ActionStatus { get; set; } = null!;

    public decimal ActionProgress { get; set; }

    public string ActionPriority { get; set; } = null!;

    public DateTime? CompletedDate { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User ActionOwnerUser { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Issue? Issue { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Risk? Risk { get; set; }

    public virtual User UpdatedByUser { get; set; } = null!;
}
