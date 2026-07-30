using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class Risk
{
    public string RiskId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string RiskTitle { get; set; } = null!;

    public string RiskCategory { get; set; } = null!;

    public int RiskProbability { get; set; }

    public int RiskImpact { get; set; }

    public int RiskScore { get; set; }

    public string RiskOwnerUserId { get; set; } = null!;

    public string RiskMitigation { get; set; } = null!;

    public DateTime RiskDueDate { get; set; }

    public string RiskStatus { get; set; } = null!;

    public DateTime OpenedDate { get; set; }

    public DateTime? ClosedDate { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Action> Actions { get; set; } = new List<Action>();

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();

    public virtual Project Project { get; set; } = null!;

    public virtual User RiskOwnerUser { get; set; } = null!;

    public virtual User UpdatedByUser { get; set; } = null!;
}
