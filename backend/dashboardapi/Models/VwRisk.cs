using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class VwRisk
{
    public string? RiskId { get; set; }

    public string? ProjectId { get; set; }

    public string? ProjectCode { get; set; }

    public string? ProjectName { get; set; }

    public string? RiskTitle { get; set; }

    public string? RiskCategory { get; set; }

    public int? RiskProbability { get; set; }

    public int? RiskImpact { get; set; }

    public int? RiskScore { get; set; }

    public string? RiskStatus { get; set; }

    public DateTime? RiskDueDate { get; set; }

    public string? RiskOwnerUserId { get; set; }

    public string? RiskOwnerFullName { get; set; }

    public byte[]? RiskHealth { get; set; }
}
