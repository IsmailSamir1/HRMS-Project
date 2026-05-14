using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Leave
{
    public int LeaveId { get; set; }

    public string LeaveType { get; set; } = null!;

    public string? LeaveDescription { get; set; }

    public virtual HolidayLeave? HolidayLeave { get; set; }

    public virtual ICollection<LeaveEntitlement> LeaveEntitlements { get; set; } = new List<LeaveEntitlement>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public virtual ProbationLeave? ProbationLeave { get; set; }

    public virtual SickLeave? SickLeave { get; set; }

    public virtual VacationLeave? VacationLeave { get; set; }
}
