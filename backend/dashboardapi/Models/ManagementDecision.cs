using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class ManagementDecision
{
    public string ManagementDecisionId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string DecisionTitle { get; set; } = null!;

    public string Decision { get; set; } = null!;

    public string DecisionOwnerUserId { get; set; } = null!;

    public DateTime DecisionDueDate { get; set; }

    public string DecisionStatus { get; set; } = null!;

    public string DecisionImpact { get; set; } = null!;

    public string? IfDelayed { get; set; }

    public string? Recommendation { get; set; }

    public DateTime DecisionDate { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual User DecisionOwnerUser { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User UpdatedByUser { get; set; } = null!;
}
