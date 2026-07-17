using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class AuditLog
{
    public string AuditLogId { get; set; } = null!;

    public string? UserId { get; set; }

    public string EntityName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? IpAddress { get; set; }

    public virtual User? User { get; set; }
}
