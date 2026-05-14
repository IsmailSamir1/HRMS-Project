using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ShiftCycle
{
    public int CycleId { get; set; }

    public string? CycleName { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<ShiftCycleAssignment> ShiftCycleAssignments { get; set; } = new List<ShiftCycleAssignment>();
}
