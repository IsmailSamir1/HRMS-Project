using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using WebApplication1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    // Helper class for real-time notifications (not stored in database)
    public class RealTimeNotification
    {
        public string MessageContent { get; set; }
        public string NotificationType { get; set; }
        public string Urgency { get; set; }
        public DateTime? Timestamp { get; set; }
        public bool IsRealTime { get; set; } = true;
    }

    public class NotificationsController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public NotificationsController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // =========================
        // (c) ALL EMPLOYEES: VIEW THEIR NOTIFICATIONS
        // =========================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var notifications = await _context.EmployeeNotifications
                .Include(en => en.Notification)
                .Where(en => en.EmployeeId == employeeId)
                .OrderByDescending(en => en.Notification.Timestamp)
                .ToListAsync();

            var realTimeNotifications = await GetRealTimeNotifications(employeeId);
            ViewBag.RealTimeNotifications = realTimeNotifications;

            return View(notifications);
        }

        // =========================
        // GET NOTIFICATIONS (for navbar dropdown)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Json(new List<object>());

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Get database notifications
            var dbNotifications = await _context.EmployeeNotifications
                .Include(en => en.Notification)
                .Where(en => en.EmployeeId == employeeId)
                .OrderByDescending(en => en.Notification.Timestamp)
                .Take(10)
                .ToListAsync();

            var dbItems = dbNotifications.Select(en => new {
                id = en.Notification.NotificationId,
                title = en.Notification.NotificationType ?? "Notification",
                message = en.Notification.MessageContent ?? "",
                timestamp = en.Notification.Timestamp,
                timeAgo = GetTimeAgo(en.Notification.Timestamp),
                isRead = en.Notification.ReadStatus ?? false,
                urgency = en.Notification.Urgency ?? "Normal",
                link = "/Notifications"
            }).ToList();

            // Get real-time notifications
            var realTimeNotifications = await GetRealTimeNotifications(employeeId);
            var realTimeItems = realTimeNotifications.Take(5).Select(n => new {
                id = 0,
                title = n.NotificationType ?? "Notification",
                message = n.MessageContent ?? "",
                timestamp = n.Timestamp,
                timeAgo = GetTimeAgo(n.Timestamp),
                isRead = false,
                urgency = n.Urgency ?? "Normal",
                link = "/Notifications"
            }).ToList();

            // Combine: real-time first, then database notifications
            var allNotifications = new List<object>();
            allNotifications.AddRange(realTimeItems);
            allNotifications.AddRange(dbItems);

            return Json(allNotifications.Take(10));
        }

        // =========================
        // GET USER NOTIFICATIONS (API)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null) return Unauthorized();

            var notifications = await _context.EmployeeNotifications
                .Include(en => en.Notification)
                .Where(en => en.EmployeeId == employeeId && (en.Notification.ReadStatus == false || en.Notification.ReadStatus == null))
                .OrderByDescending(en => en.Notification.Timestamp)
                .Select(en => new {
                    en.Notification.NotificationId,
                    en.Notification.MessageContent,
                    en.Notification.Timestamp,
                    en.Notification.Urgency,
                    en.Notification.NotificationType
                })
                .ToListAsync();

            return Json(notifications);
        }

        // =========================
        // GET UNREAD COUNT (for navbar badge)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Json(0);

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var dbCount = await _context.EmployeeNotifications
                .Include(en => en.Notification)
                .Where(en => en.EmployeeId == employeeId && en.Notification.ReadStatus == false)
                .CountAsync();

            var realTimeNotifications = await GetRealTimeNotifications(employeeId);
            var realTimeCount = realTimeNotifications.Count;

            return Json(dbCount + realTimeCount);
        }

        // =========================
        // MARK NOTIFICATION AS READ
        // =========================
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

            if (notification != null)
            {
                notification.ReadStatus = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // =========================
        // MARK NOTIFICATION AS READ (API)
        // =========================
        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsReadApi(int id)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null) return Unauthorized();

            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.ReadStatus = true;
                await _context.SaveChangesAsync();
                return Ok();
            }

            return NotFound();
        }

        // =========================
        // MARK ALL AS READ
        // =========================
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var employeeNotifications = await _context.EmployeeNotifications
                .Include(en => en.Notification)
                .Where(en => en.EmployeeId == employeeId)
                .ToListAsync();

            foreach (var en in employeeNotifications)
            {
                if (en.Notification != null)
                {
                    en.Notification.ReadStatus = true;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "All notifications marked as read.";

            return RedirectToAction("Index");
        }

        // =========================
        // (b) LINE MANAGER ONLY: SEND NOTIFICATION TO TEAM - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Send()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsLineManager") != "True")
            {
                TempData["Error"] = "Access denied. Only Line Managers can send team notifications.";
                return RedirectToAction("Index", "Home");
            }

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var teamMembers = await _context.Employees
                .Where(e => e.ManagerId == managerId && e.IsActive == true)
                .ToListAsync();

            ViewBag.TeamMembers = teamMembers;

            return View();
        }

        // =========================
        // (b) LINE MANAGER ONLY: SEND NOTIFICATION TO TEAM - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string MessageContent, string Urgency, string NotificationType, int[] SelectedEmployees, bool SendToAll)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsLineManager") != "True")
            {
                TempData["Error"] = "Access denied. Only Line Managers can send team notifications.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(MessageContent))
            {
                ViewBag.Error = "Please enter a message.";
                int mgrId = HttpContext.Session.GetInt32("EmployeeId").Value;
                ViewBag.TeamMembers = await _context.Employees
                    .Where(e => e.ManagerId == mgrId && e.IsActive == true)
                    .ToListAsync();
                return View();
            }

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;

            List<int> recipientIds = new List<int>();

            if (SendToAll)
            {
                recipientIds = await _context.Employees
                    .Where(e => e.ManagerId == managerId && e.IsActive == true)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
            }
            else if (SelectedEmployees != null && SelectedEmployees.Length > 0)
            {
                recipientIds = SelectedEmployees.ToList();
            }

            if (recipientIds.Count == 0)
            {
                ViewBag.Error = "Please select at least one team member.";
                ViewBag.TeamMembers = await _context.Employees
                    .Where(e => e.ManagerId == managerId && e.IsActive == true)
                    .ToListAsync();
                return View();
            }

            var notification = new Notification
            {
                MessageContent = MessageContent,
                Timestamp = DateTime.Now,
                Urgency = Urgency ?? "Normal",
                ReadStatus = false,
                NotificationType = NotificationType ?? "TeamMessage"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            foreach (int empId in recipientIds)
            {
                var empNotification = new EmployeeNotification
                {
                    EmployeeId = empId,
                    NotificationId = notification.NotificationId,
                    DeliveryStatus = "Delivered",
                    DeliveredAt = DateTime.Now
                };
                _context.EmployeeNotifications.Add(empNotification);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Notification sent to {recipientIds.Count} team member(s) successfully!";
            return RedirectToAction("Index");
        }

        // =========================
        // HELPER: Get Time Ago String
        // =========================
        private static string GetTimeAgo(DateTime? timestamp)
        {
            if (!timestamp.HasValue)
                return "Just now";

            var timeSpan = DateTime.Now - timestamp.Value;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";

            return timestamp.Value.ToString("MMM dd");
        }

        // =========================
        // HELPER: Get Real-Time Notifications
        // =========================
        private async Task<List<RealTimeNotification>> GetRealTimeNotifications(int employeeId)
        {
            var notifications = new List<RealTimeNotification>();

            var contractNotifications = await CheckContractExpiration(employeeId);
            notifications.AddRange(contractNotifications);

            var leaveNotifications = await CheckLeaveStatus(employeeId);
            notifications.AddRange(leaveNotifications);

            var shiftNotifications = await CheckShiftAssignments(employeeId);
            notifications.AddRange(shiftNotifications);

            var missionNotifications = await CheckMissionUpdates(employeeId);
            notifications.AddRange(missionNotifications);

            return notifications.OrderByDescending(n => n.Timestamp).ToList();
        }

        // =========================
        // CHECK CONTRACT EXPIRATION
        // =========================
        private async Task<List<RealTimeNotification>> CheckContractExpiration(int employeeId)
        {
            var notifications = new List<RealTimeNotification>();

            var employee = await _context.Employees
                .Include(e => e.Contract)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee?.Contract != null && employee.Contract.EndDate.HasValue)
            {
                var endDate = employee.Contract.EndDate.Value;
                var today = DateOnly.FromDateTime(DateTime.Now);
                var daysUntilExpiry = (endDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                if (daysUntilExpiry <= 0)
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your contract has EXPIRED on {endDate:MMM dd, yyyy}. Please contact HR immediately.",
                        NotificationType = "Contract",
                        Urgency = "High",
                        Timestamp = DateTime.Now
                    });
                }
                else if (daysUntilExpiry <= 7)
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"URGENT: Your contract expires in {daysUntilExpiry} day(s) on {endDate:MMM dd, yyyy}.",
                        NotificationType = "Contract",
                        Urgency = "High",
                        Timestamp = DateTime.Now
                    });
                }
                else if (daysUntilExpiry <= 30)
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your contract will expire in {daysUntilExpiry} days on {endDate:MMM dd, yyyy}. Please contact HR for renewal.",
                        NotificationType = "Contract",
                        Urgency = "Normal",
                        Timestamp = DateTime.Now
                    });
                }
            }

            return notifications;
        }

        // =========================
        // CHECK LEAVE STATUS
        // =========================
        private async Task<List<RealTimeNotification>> CheckLeaveStatus(int employeeId)
        {
            var notifications = new List<RealTimeNotification>();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.Leave)
                .Where(lr => lr.EmployeeId == employeeId &&
                             lr.ApprovalTiming.HasValue &&
                             lr.ApprovalTiming.Value >= thirtyDaysAgo)
                .ToListAsync();

            foreach (var request in leaveRequests)
            {
                string leaveType = request.Leave?.LeaveType ?? "Leave";

                if (request.Status == "Approved")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your {leaveType} request for {request.Duration} day(s) has been APPROVED.",
                        NotificationType = "Leave",
                        Urgency = "Normal",
                        Timestamp = request.ApprovalTiming
                    });
                }
                else if (request.Status == "Rejected")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your {leaveType} request for {request.Duration} day(s) has been REJECTED.",
                        NotificationType = "Leave",
                        Urgency = "High",
                        Timestamp = request.ApprovalTiming
                    });
                }
                else if (request.Status == "Pending")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your {leaveType} request for {request.Duration} day(s) is PENDING approval.",
                        NotificationType = "Leave",
                        Urgency = "Normal",
                        Timestamp = request.ApprovalTiming ?? DateTime.Now
                    });
                }
            }

            return notifications;
        }

        // =========================
        // CHECK SHIFT ASSIGNMENTS
        // =========================
        private async Task<List<RealTimeNotification>> CheckShiftAssignments(int employeeId)
        {
            var notifications = new List<RealTimeNotification>();

            var today = DateOnly.FromDateTime(DateTime.Now);

            var shiftAssignments = await _context.ShiftAssignments
                .Include(sa => sa.Shift)
                .Where(sa => sa.EmployeeId == employeeId &&
                             sa.EndDate.HasValue &&
                             sa.EndDate.Value >= today)
                .ToListAsync();

            foreach (var assignment in shiftAssignments)
            {
                string shiftName = assignment.Shift?.Name ?? "Shift";
                string shiftType = assignment.Shift?.Type ?? "";

                if (assignment.Status == "Assigned")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"You have been assigned to '{shiftName}' ({shiftType}) from {assignment.StartDate:MMM dd} to {assignment.EndDate:MMM dd, yyyy}.",
                        NotificationType = "Shift",
                        Urgency = "Normal",
                        Timestamp = DateTime.Now
                    });
                }
                else if (assignment.Status == "Reassigned")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your shift has been REASSIGNED to '{shiftName}' ({shiftType}).",
                        NotificationType = "Shift",
                        Urgency = "High",
                        Timestamp = DateTime.Now
                    });
                }
            }

            return notifications;
        }

        // =========================
        // CHECK MISSION UPDATES
        // =========================
        private async Task<List<RealTimeNotification>> CheckMissionUpdates(int employeeId)
        {
            var notifications = new List<RealTimeNotification>();

            var missions = await _context.Missions
                .Where(m => m.EmployeeId == employeeId)
                .ToListAsync();

            foreach (var mission in missions)
            {
                if (mission.Status == "Planned" && mission.StartDate.HasValue)
                {
                    var daysUntilStart = (mission.StartDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;

                    if (daysUntilStart <= 7 && daysUntilStart >= 0)
                    {
                        notifications.Add(new RealTimeNotification
                        {
                            MessageContent = $"Upcoming Mission: You have a planned mission to '{mission.Destination}' starting in {daysUntilStart} day(s) on {mission.StartDate:MMM dd, yyyy}.",
                            NotificationType = "Mission",
                            Urgency = daysUntilStart <= 2 ? "High" : "Normal",
                            Timestamp = DateTime.Now
                        });
                    }
                }
                else if (mission.Status == "Completed")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your mission to '{mission.Destination}' has been marked as COMPLETED.",
                        NotificationType = "Mission",
                        Urgency = "Normal",
                        Timestamp = mission.EndDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now
                    });
                }
                else if (mission.Status == "Cancelled")
                {
                    notifications.Add(new RealTimeNotification
                    {
                        MessageContent = $"Your mission to '{mission.Destination}' has been CANCELLED.",
                        NotificationType = "Mission",
                        Urgency = "High",
                        Timestamp = DateTime.Now
                    });
                }
            }

            return notifications;
        }
    }
}