using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class RolePermission
{
    public int RoleId { get; set; }

    public string PermissionName { get; set; } = null!;

    public string? AllowedAction { get; set; }

    public virtual Role Role { get; set; } = null!;
}
