using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class InternshipContract
{
    public int ContractId { get; set; }

    public string? Mentoring { get; set; }

    public string? Evaluation { get; set; }

    public decimal? StipendRelated { get; set; }

    public virtual Contract Contract { get; set; } = null!;
}
