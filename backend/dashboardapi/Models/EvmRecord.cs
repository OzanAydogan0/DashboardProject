using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class EvmRecord
{
    public string EvmRecordId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string Period { get; set; } = null!;

    public decimal Bac { get; set; }

    public decimal Pv { get; set; }

    public decimal Ev { get; set; }

    public decimal Ac { get; set; }

    public decimal? Sv { get; set; }

    public decimal? Cv { get; set; }

    public decimal? Spi { get; set; }

    public decimal? Cpi { get; set; }

    public decimal? Eac { get; set; }

    public decimal? Vac { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string UpdatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User UpdatedByUser { get; set; } = null!;
}
