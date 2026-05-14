using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LeaveEntitlement
{
    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public decimal? Entitlement { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Leave LeaveType { get; set; } = null!;
}
