using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class PayrollPeriod
{
    public int PayrollPeriodId { get; set; }

    public int? PayrollId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public virtual Payroll? Payroll { get; set; }
}
