using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ShiftSchedule
{
    public int ShiftId { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int? BreakDuration { get; set; }

    public DateOnly? ShiftDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();

    public virtual ICollection<ShiftCycleAssignment> ShiftCycleAssignments { get; set; } = new List<ShiftCycleAssignment>();
}
