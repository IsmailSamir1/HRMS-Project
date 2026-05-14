using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Currency
{
    public string CurrencyCode { get; set; } = null!;

    public string CurrencyName { get; set; } = null!;

    public decimal ExchangeRate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<AllowanceDeduction> AllowanceDeductions { get; set; } = new List<AllowanceDeduction>();

    public virtual ICollection<SalaryType> SalaryTypes { get; set; } = new List<SalaryType>();
}
