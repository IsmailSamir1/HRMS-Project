using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LeaveRequest
{
    public int RequestId { get; set; }

    public int? EmployeeId { get; set; }

    public int? LeaveId { get; set; }

    public string? Justification { get; set; }

    public int? Duration { get; set; }

    public DateTime? ApprovalTiming { get; set; }

    public string? Status { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Leave? Leave { get; set; }

    public virtual ICollection<LeaveDocument> LeaveDocuments { get; set; } = new List<LeaveDocument>();
}
