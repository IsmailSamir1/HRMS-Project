using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ContractSalaryType
{
    public int SalaryTypeId { get; set; }

    public decimal? ContractValue { get; set; }

    public string? InstallmentDetails { get; set; }

    public virtual SalaryType SalaryType { get; set; } = null!;
}
