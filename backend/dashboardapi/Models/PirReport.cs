using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class PirReport
{
    public string PirReportId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string Period { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public string ExecutiveSummary { get; set; } = null!;

    public string CompletedWork { get; set; } = null!;

    public string? Delays { get; set; }

    public string NextPeriodPlan { get; set; } = null!;

    public string? ManagementExpectations { get; set; }

    public string ManualHealth { get; set; } = null!;

    public string ReportStatus { get; set; } = null!;

    public string? PublishedByUserId { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User? PublishedByUser { get; set; }

    public virtual User UpdatedByUser { get; set; } = null!;
}
