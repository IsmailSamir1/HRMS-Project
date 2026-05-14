using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ApprovalWorkflow
{
    public int WorkflowId { get; set; }

    public string? WorkflowType { get; set; }

    public decimal? ThresholdAmount { get; set; }

    public string? ApproverRole { get; set; }

    public int? CreatedBy { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<ApprovalWorkflowStep> ApprovalWorkflowSteps { get; set; } = new List<ApprovalWorkflowStep>();
}
