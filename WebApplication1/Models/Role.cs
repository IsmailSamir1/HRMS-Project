using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Purpose { get; set; }

    public virtual ICollection<ApprovalWorkflowStep> ApprovalWorkflowSteps { get; set; } = new List<ApprovalWorkflowStep>();

    public virtual ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
