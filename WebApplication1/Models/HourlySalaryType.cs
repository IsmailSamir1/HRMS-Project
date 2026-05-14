using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class HourlySalaryType
{
    public int SalaryTypeId { get; set; }

    public decimal? HourlyRate { get; set; }

    public int? MaxMonthlyHours { get; set; }

    public virtual SalaryType SalaryType { get; set; } = null!;
}
