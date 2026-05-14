using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LatenessPolicy
{
    public int PolicyId { get; set; }

    public int? GracePeriodMins { get; set; }

    public decimal? DeductionRate { get; set; }

    public virtual PayrollPolicy Policy { get; set; } = null!;
}
