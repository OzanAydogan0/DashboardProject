using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class ProjectUser
{
    public string ProjectUserId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string AssignedByUserId { get; set; } = null!;

    public string AssignmentStatus { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User AssignedByUser { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
