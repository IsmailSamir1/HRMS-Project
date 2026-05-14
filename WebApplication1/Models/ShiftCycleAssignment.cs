using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ShiftCycleAssignment
{
    public int CycleId { get; set; }

    public int ShiftId { get; set; }

    public int? OrderNumber { get; set; }

    public virtual ShiftCycle Cycle { get; set; } = null!;

    public virtual ShiftSchedule Shift { get; set; } = null!;
}
