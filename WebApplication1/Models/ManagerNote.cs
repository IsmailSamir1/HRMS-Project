using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class ManagerNote
{
    public int NoteId { get; set; }

    public int? EmployeeId { get; set; }

    public int? ManagerId { get; set; }

    public string? NoteContent { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Employee? Manager { get; set; }
}
