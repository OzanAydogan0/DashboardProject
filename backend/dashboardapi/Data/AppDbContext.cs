using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using dashboardapi.Models;
using ProgramEntity = dashboardapi.Models.Program;
using Action = dashboardapi.Models.Action;
namespace dashboardapi.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Action> Actions { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<EvmRecord> EvmRecords { get; set; }

    public virtual DbSet<Issue> Issues { get; set; }

    public virtual DbSet<ManagementDecision> ManagementDecisions { get; set; }

    public virtual DbSet<Milestone> Milestones { get; set; }

    public virtual DbSet<PirReport> PirReports { get; set; }

    public virtual DbSet<ProgramEntity> Programs { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectUser> ProjectUsers { get; set; }

    public virtual DbSet<Risk> Risks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwDashboard> VwDashboards { get; set; }

    public virtual DbSet<VwEvm> VwEvms { get; set; }

    public virtual DbSet<VwPir> VwPirs { get; set; }

    public virtual DbSet<VwRisk> VwRisks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=../../database/dashboard.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Action>(entity =>
        {
            entity.ToTable("actions");

            entity.HasIndex(e => new { e.ActionOwnerUserId, e.ActionDueDate }, "idx_actions_owner_due");

            entity.HasIndex(e => new { e.ProjectId, e.ActionStatus, e.ActionDueDate }, "idx_actions_project_status_due");

            entity.HasIndex(e => e.IssueId, "idx_actions_issue_id");

            entity.HasIndex(e => e.RiskId, "idx_actions_risk_id");

            entity.Property(e => e.ActionId).HasColumnName("action_id");
            entity.Property(e => e.ActionDescription).HasColumnName("action_description");
            entity.Property(e => e.ActionDueDate)
                .HasColumnType("DATE")
                .HasColumnName("action_due_date");
            entity.Property(e => e.ActionOwnerUserId).HasColumnName("action_owner_user_id");
            entity.Property(e => e.ActionPriority).HasColumnName("action_priority");
            entity.Property(e => e.ActionProgress)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMERIC")
                .HasColumnName("action_progress");
            entity.Property(e => e.ActionStatus)
                .HasDefaultValue("Açık")
                .HasColumnName("action_status");
            entity.Property(e => e.CompletedDate)
                .HasColumnType("DATE")
                .HasColumnName("completed_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.IssueId).HasColumnName("issue_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RiskId).HasColumnName("risk_id");
            entity.Property(e => e.SourceReference).HasColumnName("source_reference");
            entity.Property(e => e.SourceType).HasColumnName("source_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.ActionOwnerUser).WithMany(p => p.ActionActionOwnerUsers)
                .HasForeignKey(d => d.ActionOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ActionCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Issue).WithMany(p => p.Actions)
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Project).WithMany(p => p.Actions).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.Risk).WithMany(p => p.Actions)
                .HasForeignKey(d => d.RiskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ActionUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.ChangedAt, "idx_audit_logs_changed_at");

            entity.HasIndex(e => new { e.EntityName, e.EntityId, e.ChangedAt }, "idx_audit_logs_entity").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.UserId, e.ChangedAt }, "idx_audit_logs_user_changed_at").IsDescending(false, true);

            entity.Property(e => e.AuditLogId).HasColumnName("audit_log_id");
            entity.Property(e => e.ActionType).HasColumnName("action_type");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("changed_at");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityName).HasColumnName("entity_name");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.NewValues).HasColumnName("new_values");
            entity.Property(e => e.OldValues).HasColumnName("old_values");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerName).HasColumnName("customer_name");
            entity.Property(e => e.CustomerStatus)
                .HasDefaultValue("Aktif")
                .HasColumnName("customer_status");
            entity.Property(e => e.CustomerType).HasColumnName("customer_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<EvmRecord>(entity =>
        {
            entity.ToTable("evm_records");

            entity.HasIndex(e => new { e.ProjectId, e.Period }, "IX_evm_records_project_id_period").IsUnique();

            entity.HasIndex(e => new { e.ProjectId, e.Period }, "idx_evm_records_project_period_desc").IsDescending(false, true);

            entity.Property(e => e.EvmRecordId).HasColumnName("evm_record_id");
            entity.Property(e => e.Ac)
                .HasColumnType("NUMERIC")
                .HasColumnName("ac");
            entity.Property(e => e.Bac)
                .HasColumnType("NUMERIC")
                .HasColumnName("bac");
            entity.Property(e => e.Cpi)
                .HasColumnType("NUMERIC")
                .HasColumnName("cpi");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Cv)
                .HasColumnType("NUMERIC")
                .HasColumnName("cv");
            entity.Property(e => e.Eac)
                .HasColumnType("NUMERIC")
                .HasColumnName("eac");
            entity.Property(e => e.Ev)
                .HasColumnType("NUMERIC")
                .HasColumnName("ev");
            entity.Property(e => e.Period).HasColumnName("period");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Pv)
                .HasColumnType("NUMERIC")
                .HasColumnName("pv");
            entity.Property(e => e.Spi)
                .HasColumnType("NUMERIC")
                .HasColumnName("spi");
            entity.Property(e => e.Sv)
                .HasColumnType("NUMERIC")
                .HasColumnName("sv");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(e => e.Vac)
                .HasColumnType("NUMERIC")
                .HasColumnName("vac");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.EvmRecordCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.EvmRecords).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.EvmRecordUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.ToTable("issues");

            entity.HasIndex(e => new { e.IssueOwnerUserId, e.IssueDueDate }, "idx_issues_owner_due");

            entity.HasIndex(e => new { e.ProjectId, e.IssueStatus, e.IssuePriority }, "idx_issues_project_status_priority");

            entity.HasIndex(e => e.RiskId, "idx_issues_risk_id");

            entity.Property(e => e.IssueId).HasColumnName("issue_id");
            entity.Property(e => e.ClosedDate)
                .HasColumnType("DATE")
                .HasColumnName("closed_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.IssueDueDate)
                .HasColumnType("DATE")
                .HasColumnName("issue_due_date");
            entity.Property(e => e.IssueImpact).HasColumnName("issue_impact");
            entity.Property(e => e.IssueOwnerUserId).HasColumnName("issue_owner_user_id");
            entity.Property(e => e.IssuePriority).HasColumnName("issue_priority");
            entity.Property(e => e.IssueResolution).HasColumnName("issue_resolution");
            entity.Property(e => e.IssueStatus)
                .HasDefaultValue("Açık")
                .HasColumnName("issue_status");
            entity.Property(e => e.IssueTitle).HasColumnName("issue_title");
            entity.Property(e => e.OpenedDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnType("DATE")
                .HasColumnName("opened_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RiskId).HasColumnName("risk_id");
            entity.Property(e => e.RootCause).HasColumnName("root_cause");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.IssueCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.IssueOwnerUser).WithMany(p => p.IssueIssueOwnerUsers)
                .HasForeignKey(d => d.IssueOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.Issues).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.Risk).WithMany(p => p.Issues)
                .HasForeignKey(d => d.RiskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.IssueUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ManagementDecision>(entity =>
        {
            entity.ToTable("management_decisions");

            entity.HasIndex(e => new { e.ProjectId, e.DecisionStatus, e.DecisionDueDate }, "idx_management_decisions_project_status_due");

            entity.Property(e => e.ManagementDecisionId).HasColumnName("management_decision_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Decision).HasColumnName("decision");
            entity.Property(e => e.DecisionDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnType("DATE")
                .HasColumnName("decision_date");
            entity.Property(e => e.DecisionDueDate)
                .HasColumnType("DATE")
                .HasColumnName("decision_due_date");
            entity.Property(e => e.DecisionImpact).HasColumnName("decision_impact");
            entity.Property(e => e.DecisionOwnerUserId).HasColumnName("decision_owner_user_id");
            entity.Property(e => e.DecisionStatus)
                .HasDefaultValue("Açık")
                .HasColumnName("decision_status");
            entity.Property(e => e.DecisionTitle).HasColumnName("decision_title");
            entity.Property(e => e.IfDelayed).HasColumnName("if_delayed");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Recommendation).HasColumnName("recommendation");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ManagementDecisionCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.DecisionOwnerUser).WithMany(p => p.ManagementDecisionDecisionOwnerUsers)
                .HasForeignKey(d => d.DecisionOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.ManagementDecisions).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ManagementDecisionUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.ToTable("milestones");

            entity.HasIndex(e => new { e.ProjectId, e.ForecastDate }, "idx_milestones_critical_open");

            entity.HasIndex(e => new { e.ProjectId, e.ForecastDate }, "idx_milestones_project_dates");

            entity.Property(e => e.MilestoneId).HasColumnName("milestone_id");
            entity.Property(e => e.AcceptanceCriteria).HasColumnName("acceptance_criteria");
            entity.Property(e => e.ActualDate)
                .HasColumnType("DATE")
                .HasColumnName("actual_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Critical).HasColumnName("critical");
            entity.Property(e => e.ForecastDate)
                .HasColumnType("DATE")
                .HasColumnName("forecast_date");
            entity.Property(e => e.MilestoneDescription).HasColumnName("milestone_description");
            entity.Property(e => e.MilestoneName).HasColumnName("milestone_name");
            entity.Property(e => e.MilestoneOwnerUserId).HasColumnName("milestone_owner_user_id");
            entity.Property(e => e.MilestoneStatus)
                .HasDefaultValue("Planlandı")
                .HasColumnName("milestone_status");
            entity.Property(e => e.PlannedDate)
                .HasColumnType("DATE")
                .HasColumnName("planned_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.MilestoneCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.MilestoneOwnerUser).WithMany(p => p.MilestoneMilestoneOwnerUsers)
                .HasForeignKey(d => d.MilestoneOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.Milestones).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.MilestoneUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PirReport>(entity =>
        {
            entity.ToTable("pir_reports");

            entity.HasIndex(e => new { e.ProjectId, e.Period }, "IX_pir_reports_project_id_period").IsUnique();

            entity.HasIndex(e => new { e.ProjectId, e.ReportDate }, "idx_pir_reports_project_report_date").IsDescending(false, true);

            entity.HasIndex(e => new { e.ReportStatus, e.ReportDate }, "idx_pir_reports_status").IsDescending(false, true);

            entity.Property(e => e.PirReportId).HasColumnName("pir_report_id");
            entity.Property(e => e.CompletedWork).HasColumnName("completed_work");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Delays).HasColumnName("delays");
            entity.Property(e => e.ExecutiveSummary).HasColumnName("executive_summary");
            entity.Property(e => e.ManagementExpectations).HasColumnName("management_expectations");
            entity.Property(e => e.ManualHealth)
                .HasDefaultValue("Gri")
                .HasColumnName("manual_health");
            entity.Property(e => e.NextPeriodPlan).HasColumnName("next_period_plan");
            entity.Property(e => e.Period).HasColumnName("period");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PublishedAt)
                .HasColumnType("DATETIME")
                .HasColumnName("published_at");
            entity.Property(e => e.PublishedByUserId).HasColumnName("published_by_user_id");
            entity.Property(e => e.ReportDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnType("DATE")
                .HasColumnName("report_date");
            entity.Property(e => e.ReportStatus)
                .HasDefaultValue("Taslak")
                .HasColumnName("report_status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PirReportCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.PirReports).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.PublishedByUser).WithMany(p => p.PirReportPublishedByUsers)
                .HasForeignKey(d => d.PublishedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.PirReportUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProgramEntity>(entity =>
        {
            entity.ToTable("programs");

            entity.HasIndex(e => e.ProgramName, "IX_programs_program_name").IsUnique();

            entity.Property(e => e.ProgramId).HasColumnName("program_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.ProgramDescription).HasColumnName("program_description");
            entity.Property(e => e.ProgramName).HasColumnName("program_name");
            entity.Property(e => e.ProgramStatus)
                .HasDefaultValue("Aktif")
                .HasColumnName("program_status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");

            entity.HasIndex(e => e.ProjectCode, "IX_projects_project_code").IsUnique();

            entity.HasIndex(e => e.CustomerId, "idx_projects_customer_id");

            entity.HasIndex(e => e.ForecastFinishDate, "idx_projects_forecast_finish_date");

            entity.HasIndex(e => e.ProgramId, "idx_projects_program_id");

            entity.HasIndex(e => e.ProjectManagerUserId, "idx_projects_project_manager_user_id");

            entity.HasIndex(e => new { e.ProjectStatus, e.IsActive }, "idx_projects_status_active");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ActualFinishDate)
                .HasColumnType("DATE")
                .HasColumnName("actual_finish_date");
            entity.Property(e => e.ActualProgress)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMERIC")
                .HasColumnName("actual_progress");
            entity.Property(e => e.Bac)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMERIC")
                .HasColumnName("bac");
            entity.Property(e => e.BaselineFinishDate)
                .HasColumnType("DATE")
                .HasColumnName("baseline_finish_date");
            entity.Property(e => e.Confidentiality)
                .HasDefaultValue("Normal")
                .HasColumnName("confidentiality");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Currency)
                .HasDefaultValue("TRY")
                .HasColumnName("currency");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ForecastFinishDate)
                .HasColumnType("DATE")
                .HasColumnName("forecast_finish_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(1)
                .HasColumnName("is_active");
            entity.Property(e => e.ManualHealth)
                .HasDefaultValue("Gri")
                .HasColumnName("manual_health");
            entity.Property(e => e.PlannedProgress)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMERIC")
                .HasColumnName("planned_progress");
            entity.Property(e => e.ProgramId).HasColumnName("program_id");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.ProjectDescription).HasColumnName("project_description");
            entity.Property(e => e.ProjectManagerUserId).HasColumnName("project_manager_user_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.ProjectStatus)
                .HasDefaultValue("Taslak")
                .HasColumnName("project_status");
            entity.Property(e => e.ReportingFrequency)
                .HasDefaultValue("Aylık")
                .HasColumnName("reporting_frequency");
            entity.Property(e => e.StartDate)
                .HasColumnType("DATE")
                .HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ProjectCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Customer).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Program).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ProjectManagerUser).WithMany(p => p.ProjectProjectManagerUsers)
                .HasForeignKey(d => d.ProjectManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ProjectUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectUser>(entity =>
        {
            entity.ToTable("project_users");

            entity.HasIndex(e => new { e.ProjectId, e.UserId }, "IX_project_users_project_id_user_id").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.ProjectId }, "idx_project_users_user_id_active");

            entity.Property(e => e.ProjectUserId).HasColumnName("project_user_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedByUserId).HasColumnName("assigned_by_user_id");
            entity.Property(e => e.AssignmentStatus)
                .HasDefaultValue("Aktif")
                .HasColumnName("assignment_status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssignedByUser).WithMany(p => p.ProjectUserAssignedByUsers)
                .HasForeignKey(d => d.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectUsers).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.User).WithMany(p => p.ProjectUserUsers).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Risk>(entity =>
        {
            entity.ToTable("risks");

            entity.HasIndex(e => new { e.RiskOwnerUserId, e.RiskDueDate }, "idx_risks_owner_due");

            entity.HasIndex(e => new { e.ProjectId, e.RiskStatus, e.RiskScore }, "idx_risks_project_status_score").IsDescending(false, false, true);

            entity.Property(e => e.RiskId).HasColumnName("risk_id");
            entity.Property(e => e.ClosedDate)
                .HasColumnType("DATE")
                .HasColumnName("closed_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.OpenedDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnType("DATE")
                .HasColumnName("opened_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RiskCategory).HasColumnName("risk_category");
            entity.Property(e => e.RiskDueDate)
                .HasColumnType("DATE")
                .HasColumnName("risk_due_date");
            entity.Property(e => e.RiskImpact).HasColumnName("risk_impact");
            entity.Property(e => e.RiskMitigation).HasColumnName("risk_mitigation");
            entity.Property(e => e.RiskOwnerUserId).HasColumnName("risk_owner_user_id");
            entity.Property(e => e.RiskProbability).HasColumnName("risk_probability");
            entity.Property(e => e.RiskScore).HasColumnName("risk_score");
            entity.Property(e => e.RiskStatus)
                .HasDefaultValue("Açık")
                .HasColumnName("risk_status");
            entity.Property(e => e.RiskTitle).HasColumnName("risk_title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.RiskCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.Risks).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.RiskOwnerUser).WithMany(p => p.RiskRiskOwnerUsers)
                .HasForeignKey(d => d.RiskOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.RiskUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "IX_users_email").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.LastLoginAt)
                .HasColumnType("DATETIME")
                .HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserRole).HasColumnName("user_role");
            entity.Property(e => e.UserStatus)
                .HasDefaultValue("Aktif")
                .HasColumnName("user_status");
        });

        modelBuilder.Entity<VwDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_dashboard");

            entity.Property(e => e.ActualProgress)
                .HasColumnType("NUMERIC")
                .HasColumnName("actual_progress");
            entity.Property(e => e.Bac)
                .HasColumnType("NUMERIC")
                .HasColumnName("bac");
            entity.Property(e => e.BaselineFinishDate)
                .HasColumnType("DATE")
                .HasColumnName("baseline_finish_date");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.ForecastFinishDate)
                .HasColumnType("DATE")
                .HasColumnName("forecast_finish_date");
            entity.Property(e => e.LatestEvmPeriod).HasColumnName("latest_evm_period");
            entity.Property(e => e.ManualHealth).HasColumnName("manual_health");
            entity.Property(e => e.OpenActionCount).HasColumnName("open_action_count");
            entity.Property(e => e.OpenIssueCount).HasColumnName("open_issue_count");
            entity.Property(e => e.OpenMilestoneCount).HasColumnName("open_milestone_count");
            entity.Property(e => e.OpenRiskCount).HasColumnName("open_risk_count");
            entity.Property(e => e.PlannedProgress)
                .HasColumnType("NUMERIC")
                .HasColumnName("planned_progress");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.ProjectStatus).HasColumnName("project_status");
        });

        modelBuilder.Entity<VwEvm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_evm");

            entity.Property(e => e.Ac)
                .HasColumnType("NUMERIC")
                .HasColumnName("ac");
            entity.Property(e => e.Bac)
                .HasColumnType("NUMERIC")
                .HasColumnName("bac");
            entity.Property(e => e.Cpi).HasColumnName("cpi");
            entity.Property(e => e.Cv).HasColumnName("cv");
            entity.Property(e => e.Eac).HasColumnName("eac");
            entity.Property(e => e.Ev)
                .HasColumnType("NUMERIC")
                .HasColumnName("ev");
            entity.Property(e => e.EvmRecordId).HasColumnName("evm_record_id");
            entity.Property(e => e.Period).HasColumnName("period");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.Pv)
                .HasColumnType("NUMERIC")
                .HasColumnName("pv");
            entity.Property(e => e.Spi).HasColumnName("spi");
            entity.Property(e => e.Sv).HasColumnName("sv");
            entity.Property(e => e.Vac).HasColumnName("vac");
        });

        modelBuilder.Entity<VwPir>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_pir");

            entity.Property(e => e.CompletedWork).HasColumnName("completed_work");
            entity.Property(e => e.Delays).HasColumnName("delays");
            entity.Property(e => e.ExecutiveSummary).HasColumnName("executive_summary");
            entity.Property(e => e.ManagementExpectations).HasColumnName("management_expectations");
            entity.Property(e => e.ManualHealth).HasColumnName("manual_health");
            entity.Property(e => e.NextPeriodPlan).HasColumnName("next_period_plan");
            entity.Property(e => e.Period).HasColumnName("period");
            entity.Property(e => e.PirReportId).HasColumnName("pir_report_id");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.PublishedAt)
                .HasColumnType("DATETIME")
                .HasColumnName("published_at");
            entity.Property(e => e.PublishedByUserId).HasColumnName("published_by_user_id");
            entity.Property(e => e.ReportDate)
                .HasColumnType("DATE")
                .HasColumnName("report_date");
            entity.Property(e => e.ReportStatus).HasColumnName("report_status");
        });

        modelBuilder.Entity<VwRisk>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_risk");

            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.RiskCategory).HasColumnName("risk_category");
            entity.Property(e => e.RiskDueDate)
                .HasColumnType("DATE")
                .HasColumnName("risk_due_date");
            entity.Property(e => e.RiskHealth).HasColumnName("risk_health");
            entity.Property(e => e.RiskId).HasColumnName("risk_id");
            entity.Property(e => e.RiskImpact).HasColumnName("risk_impact");
            entity.Property(e => e.RiskOwnerFullName).HasColumnName("risk_owner_full_name");
            entity.Property(e => e.RiskOwnerUserId).HasColumnName("risk_owner_user_id");
            entity.Property(e => e.RiskMitigation).HasColumnName("risk_mitigation");
            entity.Property(e => e.RiskProbability).HasColumnName("risk_probability");
            entity.Property(e => e.RiskScore).HasColumnName("risk_score");
            entity.Property(e => e.RiskStatus).HasColumnName("risk_status");
            entity.Property(e => e.RiskTitle).HasColumnName("risk_title");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
