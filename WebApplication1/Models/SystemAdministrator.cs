using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SystemAdministrator
{
    public int EmployeeId { get; set; }

    public int? SystemPrivilegeLevel { get; set; }

    public string? ConfigurableFields { get; set; }

    public string? AuditVisibilityScope { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
