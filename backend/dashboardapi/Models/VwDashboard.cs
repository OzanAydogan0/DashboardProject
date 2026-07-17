using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class VwDashboard
{
    public string? ProjectId { get; set; }

    public string? ProjectCode { get; set; }

    public string? ProjectName { get; set; }

    public string? ProjectStatus { get; set; }

    public string? ManualHealth { get; set; }

    public decimal? PlannedProgress { get; set; }

    public decimal? ActualProgress { get; set; }

    public DateTime? BaselineFinishDate { get; set; }

    public DateTime? ForecastFinishDate { get; set; }

    public decimal? Bac { get; set; }

    public string? Currency { get; set; }

    public byte[]? OpenRiskCount { get; set; }

    public byte[]? OpenIssueCount { get; set; }

    public byte[]? OpenActionCount { get; set; }

    public byte[]? OpenMilestoneCount { get; set; }

    public byte[]? LatestEvmPeriod { get; set; }
}
