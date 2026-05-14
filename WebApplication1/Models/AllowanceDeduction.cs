using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AllowanceDeduction
{
    public int AdId { get; set; }

    public int? PayrollId { get; set; }

    public int? EmployeeId { get; set; }

    public string? Type { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public int? Duration { get; set; }

    public string? Timezone { get; set; }

    public virtual Currency? CurrencyNavigation { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Payroll? Payroll { get; set; }
}
