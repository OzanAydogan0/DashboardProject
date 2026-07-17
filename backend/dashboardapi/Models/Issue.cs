using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class Issue
{
    public string IssueId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string IssueTitle { get; set; } = null!;

    public string IssuePriority { get; set; } = null!;

    public string IssueOwnerUserId { get; set; } = null!;

    public DateTime IssueDueDate { get; set; }

    public string IssueStatus { get; set; } = null!;

    public string IssueImpact { get; set; } = null!;

    public string? RootCause { get; set; }

    public string? IssueResolution { get; set; }

    public DateTime OpenedDate { get; set; }

    public DateTime? ClosedDate { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual User IssueOwnerUser { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User UpdatedByUser { get; set; } = null!;
}
