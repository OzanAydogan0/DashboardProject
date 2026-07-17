using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class VwPir
{
    public string? PirReportId { get; set; }

    public string? ProjectId { get; set; }

    public string? ProjectCode { get; set; }

    public string? ProjectName { get; set; }

    public string? Period { get; set; }

    public DateTime? ReportDate { get; set; }

    public string? ExecutiveSummary { get; set; }

    public string? CompletedWork { get; set; }

    public string? Delays { get; set; }

    public string? NextPeriodPlan { get; set; }

    public string? ManagementExpectations { get; set; }

    public string? ManualHealth { get; set; }

    public string? ReportStatus { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? PublishedByUserId { get; set; }
}
