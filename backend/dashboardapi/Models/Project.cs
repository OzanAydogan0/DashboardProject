using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class Project
{
    public string ProjectId { get; set; } = null!;

    public string ProjectCode { get; set; } = null!;

    public string ProjectName { get; set; } = null!;

    public string ProgramId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string ProjectManagerUserId { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime BaselineFinishDate { get; set; }

    public DateTime ForecastFinishDate { get; set; }

    public DateTime? ActualFinishDate { get; set; }

    public string ProjectStatus { get; set; } = null!;

    public string ManualHealth { get; set; } = null!;

    public decimal PlannedProgress { get; set; }

    public decimal ActualProgress { get; set; }

    public decimal Bac { get; set; }

    public string Currency { get; set; } = null!;

    public string ReportingFrequency { get; set; } = null!;

    public string Confidentiality { get; set; } = null!;

    public string? ProjectDescription { get; set; }

    public int IsActive { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Action> Actions { get; set; } = new List<Action>();

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<EvmRecord> EvmRecords { get; set; } = new List<EvmRecord>();

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();

    public virtual ICollection<ManagementDecision> ManagementDecisions { get; set; } = new List<ManagementDecision>();

    public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();

    public virtual ICollection<PirReport> PirReports { get; set; } = new List<PirReport>();

    public virtual Program Program { get; set; } = null!;

    public virtual User ProjectManagerUser { get; set; } = null!;

    public virtual ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();

    public virtual ICollection<Risk> Risks { get; set; } = new List<Risk>();

    public virtual User UpdatedByUser { get; set; } = null!;
}
