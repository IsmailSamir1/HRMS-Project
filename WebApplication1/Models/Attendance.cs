using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int? EmployeeId { get; set; }

    public int? ShiftId { get; set; }

    public DateTime? EntryTime { get; set; }

    public DateTime? ExitTime { get; set; }

    public int? Duration { get; set; }

    public string? LoginMethod { get; set; }

    public string? LogoutMethod { get; set; }

    public int? ExceptionId { get; set; }

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<AttendanceSource> AttendanceSources { get; set; } = new List<AttendanceSource>();

    public virtual Employee? Employee { get; set; }

    public virtual Exception? Exception { get; set; }

    public virtual ShiftSchedule? Shift { get; set; }
}
