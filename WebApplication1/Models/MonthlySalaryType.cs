using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class MonthlySalaryType
{
    public int SalaryTypeId { get; set; }

    public string? TaxRule { get; set; }

    public string? ContributionScheme { get; set; }

    public virtual SalaryType SalaryType { get; set; } = null!;
}
