using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class VacationLeave
{
    public int LeaveId { get; set; }

    public int? CarryOverDays { get; set; }

    public int? ApprovingManager { get; set; }

    public virtual Leave Leave { get; set; } = null!;
}
