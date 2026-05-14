using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Reimbursement
{
    public int ReimbursementId { get; set; }

    public string? Type { get; set; }

    public string? ClaimType { get; set; }

    public DateOnly? ApprovalDate { get; set; }

    public string? CurrentStatus { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Employee? Employee { get; set; }
}
