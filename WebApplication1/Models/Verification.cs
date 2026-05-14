using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Verification
{
    public int VerificationId { get; set; }

    public string? VerificationType { get; set; }

    public string? Issuer { get; set; }

    public DateOnly? IssueDate { get; set; }

    public int? ExpiryPeriod { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
