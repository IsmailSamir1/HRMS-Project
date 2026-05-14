using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AttendanceLog
{
    public int AttendanceLogId { get; set; }

    public int? AttendanceId { get; set; }

    public int? Actor { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? Reason { get; set; }

    public virtual Attendance? Attendance { get; set; }
}
