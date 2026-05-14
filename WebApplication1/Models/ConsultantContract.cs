using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ConsultantContract
{
    public int ContractId { get; set; }

    public string? ProjectScope { get; set; }

    public decimal? Fees { get; set; }

    public string? PaymentSchedule { get; set; }

    public virtual Contract Contract { get; set; } = null!;
}
