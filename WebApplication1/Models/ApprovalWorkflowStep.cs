using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ApprovalWorkflowStep
{
    public int WorkflowId { get; set; }

    public int StepNumber { get; set; }

    public int? RoleId { get; set; }

    public string? ActionRequired { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ApprovalWorkflow Workflow { get; set; } = null!;
}
