using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class PayrollLog
{
    public int PayrollLogId { get; set; }

    public int? PayrollId { get; set; }

    public int? Actor { get; set; }

    public DateTime? ChangeDate { get; set; }

    public string? ModificationType { get; set; }

    public virtual Payroll? Payroll { get; set; }
}
