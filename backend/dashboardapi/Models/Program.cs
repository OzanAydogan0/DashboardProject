using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class Program
{
    public string ProgramId { get; set; } = null!;

    public string ProgramName { get; set; } = null!;

    public string? ProgramDescription { get; set; }

    public string ProgramStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
