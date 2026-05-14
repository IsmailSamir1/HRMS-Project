using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Contract
{
    public int ContractId { get; set; }

    public string Type { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? CurrentState { get; set; }

    public virtual ConsultantContract? ConsultantContract { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual FullTimeContract? FullTimeContract { get; set; }

    public virtual InternshipContract? InternshipContract { get; set; }

    public virtual PartTimeContract? PartTimeContract { get; set; }

    public virtual ICollection<Termination> Terminations { get; set; } = new List<Termination>();
}
