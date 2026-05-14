using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LeavePolicy
{
    public int PolicyId { get; set; }

    public string? Name { get; set; }

    public string? Purpose { get; set; }

    public string? EligibilityRules { get; set; }

    public int? NoticePeriod { get; set; }

    public string? SpecialLeaveType { get; set; }

    public bool? ResetOnNewYear { get; set; }
}
