using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SalaryType
{
    public int SalaryTypeId { get; set; }

    public string Type { get; set; } = null!;

    public string? PaymentFrequency { get; set; }

    public string? Currency { get; set; }

    public virtual ContractSalaryType? ContractSalaryType { get; set; }

    public virtual Currency? CurrencyNavigation { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual HourlySalaryType? HourlySalaryType { get; set; }

    public virtual MonthlySalaryType? MonthlySalaryType { get; set; }
}
