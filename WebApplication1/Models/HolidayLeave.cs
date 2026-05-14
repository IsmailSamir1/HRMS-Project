using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class HolidayLeave
{
    public int LeaveId { get; set; }

    public string? HolidayName { get; set; }

    public bool? OfficialRecognition { get; set; }

    public string? RegionalScope { get; set; }

    public virtual Leave Leave { get; set; } = null!;
}
