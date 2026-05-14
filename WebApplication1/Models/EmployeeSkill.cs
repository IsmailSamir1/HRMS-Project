using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class EmployeeSkill
{
    public int EmployeeId { get; set; }

    public int SkillId { get; set; }

    public string? ProficiencyLevel { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
