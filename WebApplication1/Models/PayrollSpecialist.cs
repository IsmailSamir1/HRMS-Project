using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class PayrollSpecialist
{
    public int EmployeeId { get; set; }

    public string? AssignedRegion { get; set; }

    public string? ProcessingFrequency { get; set; }

    public DateOnly? LastProcessedPeriod { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
