using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AttendanceSource
{
    public int AttendanceId { get; set; }

    public int DeviceId { get; set; }

    public string? SourceType { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual Attendance Attendance { get; set; } = null!;

    public virtual Device Device { get; set; } = null!;
}
