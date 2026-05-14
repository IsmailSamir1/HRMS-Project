using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string? MessageContent { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? Urgency { get; set; }

    public bool? ReadStatus { get; set; }

    public string? NotificationType { get; set; }

    public virtual ICollection<EmployeeNotification> EmployeeNotifications { get; set; } = new List<EmployeeNotification>();
}
