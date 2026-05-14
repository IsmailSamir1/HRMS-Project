using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LeaveDocument
{
    public int DocumentId { get; set; }

    public int? LeaveRequestId { get; set; }

    public string? FilePath { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual LeaveRequest? LeaveRequest { get; set; }
}
