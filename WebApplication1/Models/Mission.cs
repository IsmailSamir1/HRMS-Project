using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Mission
{
    public int MissionId { get; set; }

    public string? Destination { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public int? EmployeeId { get; set; }

    public int? ManagerId { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Employee? Manager { get; set; }
}
