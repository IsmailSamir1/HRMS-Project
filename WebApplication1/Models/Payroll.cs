using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Payroll
{
    public int PayrollId { get; set; }

    public int? EmployeeId { get; set; }

    public decimal? Taxes { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public decimal? BaseAmount { get; set; }

    public decimal? Adjustments { get; set; }

    public decimal? Contributions { get; set; }

    public decimal? ActualPay { get; set; }

    public decimal? NetSalary { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public virtual ICollection<AllowanceDeduction> AllowanceDeductions { get; set; } = new List<AllowanceDeduction>();

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<PayrollLog> PayrollLogs { get; set; } = new List<PayrollLog>();

    public virtual ICollection<PayrollPeriod> PayrollPeriods { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<PayrollPolicy> Policies { get; set; } = new List<PayrollPolicy>();
}
