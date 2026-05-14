using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class EmployeeRole
{
    public int EmployeeId { get; set; }

    public int RoleId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
