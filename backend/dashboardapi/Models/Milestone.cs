using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class Milestone
{
    public string MilestoneId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string MilestoneName { get; set; } = null!;

    public DateTime PlannedDate { get; set; }

    public DateTime ForecastDate { get; set; }

    public DateTime? ActualDate { get; set; }

    public string MilestoneStatus { get; set; } = null!;

    public int Critical { get; set; }

    public string MilestoneOwnerUserId { get; set; } = null!;

    public string AcceptanceCriteria { get; set; } = null!;

    public string? MilestoneDescription { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual User MilestoneOwnerUser { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User UpdatedByUser { get; set; } = null!;
}
