using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public string UserStatus { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Action> ActionActionOwnerUsers { get; set; } = new List<Action>();

    public virtual ICollection<Action> ActionCreatedByUsers { get; set; } = new List<Action>();

    public virtual ICollection<Action> ActionUpdatedByUsers { get; set; } = new List<Action>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<EvmRecord> EvmRecordCreatedByUsers { get; set; } = new List<EvmRecord>();

    public virtual ICollection<EvmRecord> EvmRecordUpdatedByUsers { get; set; } = new List<EvmRecord>();

    public virtual ICollection<Issue> IssueCreatedByUsers { get; set; } = new List<Issue>();

    public virtual ICollection<Issue> IssueIssueOwnerUsers { get; set; } = new List<Issue>();

    public virtual ICollection<Issue> IssueUpdatedByUsers { get; set; } = new List<Issue>();

    public virtual ICollection<ManagementDecision> ManagementDecisionCreatedByUsers { get; set; } = new List<ManagementDecision>();

    public virtual ICollection<ManagementDecision> ManagementDecisionDecisionOwnerUsers { get; set; } = new List<ManagementDecision>();

    public virtual ICollection<ManagementDecision> ManagementDecisionUpdatedByUsers { get; set; } = new List<ManagementDecision>();

    public virtual ICollection<Milestone> MilestoneCreatedByUsers { get; set; } = new List<Milestone>();

    public virtual ICollection<Milestone> MilestoneMilestoneOwnerUsers { get; set; } = new List<Milestone>();

    public virtual ICollection<Milestone> MilestoneUpdatedByUsers { get; set; } = new List<Milestone>();

    public virtual ICollection<PirReport> PirReportCreatedByUsers { get; set; } = new List<PirReport>();

    public virtual ICollection<PirReport> PirReportPublishedByUsers { get; set; } = new List<PirReport>();

    public virtual ICollection<PirReport> PirReportUpdatedByUsers { get; set; } = new List<PirReport>();

    public virtual ICollection<Project> ProjectCreatedByUsers { get; set; } = new List<Project>();

    public virtual ICollection<Project> ProjectProjectManagerUsers { get; set; } = new List<Project>();

    public virtual ICollection<Project> ProjectUpdatedByUsers { get; set; } = new List<Project>();

    public virtual ICollection<ProjectUser> ProjectUserAssignedByUsers { get; set; } = new List<ProjectUser>();

    public virtual ICollection<ProjectUser> ProjectUserUsers { get; set; } = new List<ProjectUser>();

    public virtual ICollection<Risk> RiskCreatedByUsers { get; set; } = new List<Risk>();

    public virtual ICollection<Risk> RiskRiskOwnerUsers { get; set; } = new List<Risk>();

    public virtual ICollection<Risk> RiskUpdatedByUsers { get; set; } = new List<Risk>();
}
