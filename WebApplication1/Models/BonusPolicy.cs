using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class BonusPolicy
{
    public int PolicyId { get; set; }

    public string? BonusType { get; set; }

    public string? EligibilityCriteria { get; set; }

    public virtual PayrollPolicy Policy { get; set; } = null!;
}
