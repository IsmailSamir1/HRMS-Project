using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LineManager
{
    public int EmployeeId { get; set; }

    public int? TeamSize { get; set; }

    public string? SupervisedDepartments { get; set; }

    public decimal? ApprovalLimit { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
