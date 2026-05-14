using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class FullTimeContract
{
    public int ContractId { get; set; }

    public int? LeaveEntitlement { get; set; }

    public bool? InsuranceEligibility { get; set; }

    public int? WeeklyWorkingHours { get; set; }

    public virtual Contract Contract { get; set; } = null!;
}
