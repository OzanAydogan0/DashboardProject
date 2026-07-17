using System;
using System.Collections.Generic;

namespace dashboardapi.Models;

public partial class VwEvm
{
    public string? EvmRecordId { get; set; }

    public string? ProjectId { get; set; }

    public string? ProjectCode { get; set; }

    public string? ProjectName { get; set; }

    public string? Period { get; set; }

    public decimal? Bac { get; set; }

    public decimal? Pv { get; set; }

    public decimal? Ev { get; set; }

    public decimal? Ac { get; set; }

    public byte[]? Sv { get; set; }

    public byte[]? Cv { get; set; }

    public byte[]? Spi { get; set; }

    public byte[]? Cpi { get; set; }

    public byte[]? Eac { get; set; }

    public byte[]? Vac { get; set; }
}
