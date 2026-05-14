using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ShiftAssignment
{
    public int AssignmentId { get; set; }

    public int? EmployeeId { get; set; }

    public int? ShiftId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ShiftSchedule? Shift { get; set; }
}
