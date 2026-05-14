using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ProbationLeave
{
    public int LeaveId { get; set; }

    public DateOnly? EligibilityStartDate { get; set; }

    public int? ProbationPeriod { get; set; }

    public virtual Leave Leave { get; set; } = null!;
}
