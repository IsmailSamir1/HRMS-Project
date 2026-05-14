using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Termination
{
    public int TerminationId { get; set; }

    public DateOnly? Date { get; set; }

    public string? Reason { get; set; }

    public int? ContractId { get; set; }

    public virtual Contract? Contract { get; set; }
}
