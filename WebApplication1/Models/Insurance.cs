using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Insurance
{
    public int InsuranceId { get; set; }

    public string? Type { get; set; }

    public decimal? ContributionRate { get; set; }

    public string? Coverage { get; set; }
}
