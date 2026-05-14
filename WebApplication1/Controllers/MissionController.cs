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
    public class MissionController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public MissionController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // =========================
        // (a) EMPLOYEES: VIEW ASSIGNED MISSIONS
        // Everyone can view their own missions
        // HR Admin & System Admin can view all missions
        // Line Manager can view their team's missions
        // =========================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            List<Mission> missions;

            if (isHRAdmin || isSystemAdmin)
            {
                // HR Admin and System Admin can see ALL missions
                missions = await _context.Missions
                    .Include(m => m.Employee)
                    .Include(m => m.Manager)
                    .OrderByDescending(m => m.StartDate)
                    .ToListAsync();

                ViewBag.ViewType = "All Missions";
            }
            else if (isLineManager)
            {
                // Line Manager can see their own missions + their team's missions
                missions = await _context.Missions
                    .Include(m => m.Employee)
                    .Include(m => m.Manager)
                    .Where(m => m.EmployeeId == employeeId || m.ManagerId == employeeId)
                    .OrderByDescending(m => m.StartDate)
                    .ToListAsync();

                ViewBag.ViewType = "My Missions & Team Missions";
            }
            else
            {
                // Regular employee can only see their own missions
                missions = await _context.Missions
                    .Include(m => m.Employee)
                    .Include(m => m.Manager)
                    .Where(m => m.EmployeeId == employeeId)
                    .OrderByDescending(m => m.StartDate)
                    .ToListAsync();

                ViewBag.ViewType = "My Missions";
            }

            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CurrentEmployeeId = employeeId;

            return View(missions);
        }

        // =========================
        // (a) EMPLOYEES: VIEW MISSION DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == id);

            if (mission == null)
                return NotFound();

            // Access control: Employee can only see their own, Manager can see team's, Admin can see all
            if (!isHRAdmin && !isSystemAdmin)
            {
                if (mission.EmployeeId != employeeId && mission.ManagerId != employeeId)
                {
                    TempData["Error"] = "You are not authorized to view this mission.";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CurrentEmployeeId = employeeId;

            return View(mission);
        }

        // =========================
        // (c) HR ADMIN: ASSIGN MISSION TO EMPLOYEE - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Only HR Admin can assign missions
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can assign missions.";
                return RedirectToAction("Index");
            }

            // Get all active employees for dropdown
            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            // Get all line managers for dropdown
            var lineManagerIds = await _context.LineManagers
                .Select(lm => lm.EmployeeId)
                .ToListAsync();

            ViewBag.Managers = await _context.Employees
                .Where(e => lineManagerIds.Contains(e.EmployeeId) && e.IsActive == true)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            return View();
        }

        // =========================
        // (c) HR ADMIN: ASSIGN MISSION TO EMPLOYEE - POST
        // SENDS NOTIFICATIONS TO EMPLOYEE AND MANAGER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Destination, DateTime? StartDate, DateTime? EndDate, int EmployeeId, int? ManagerId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Only HR Admin can assign missions
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can assign missions.";
                return RedirectToAction("Index");
            }

            // Validation
            if (string.IsNullOrWhiteSpace(Destination))
            {
                ViewBag.Error = "Destination is required.";
                await LoadDropdowns();
                return View();
            }

            if (!StartDate.HasValue)
            {
                ViewBag.Error = "Start date is required.";
                await LoadDropdowns();
                return View();
            }

            if (EndDate.HasValue && EndDate < StartDate)
            {
                ViewBag.Error = "End date cannot be before start date.";
                await LoadDropdowns();
                return View();
            }

            if (EmployeeId == 0)
            {
                ViewBag.Error = "Please select an employee.";
                await LoadDropdowns();
                return View();
            }

            // Create mission with "Pending" status
            var mission = new Mission
            {
                Destination = Destination,
                StartDate = DateOnly.FromDateTime(StartDate.Value),
                EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null,
                Status = "Pending",
                EmployeeId = EmployeeId,
                ManagerId = ManagerId
            };

            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();

            // Get employee name for notification
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);
            string employeeName = employee?.FullName ?? "Employee";

            // ========================================
            // SEND NOTIFICATION TO EMPLOYEE
            // ========================================
            await SendNotification(
                EmployeeId,
                $"You have been assigned a new mission to '{Destination}' starting on {StartDate.Value:MMM dd, yyyy}.",
                "Mission",
                "Normal"
            );

            // ========================================
            // SEND NOTIFICATION TO MANAGER (if assigned)
            // ========================================
            if (ManagerId.HasValue)
            {
                var manager = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == ManagerId.Value);
                string managerName = manager?.FullName ?? "Manager";

                await SendNotification(
                    ManagerId.Value,
                    $"A new mission requires your approval: {employeeName} has been assigned to travel to '{Destination}' starting on {StartDate.Value:MMM dd, yyyy}. Please review and approve/reject.",
                    "Mission",
                    "High"
                );
            }

            TempData["Success"] = "Mission assigned successfully! Notifications sent.";
            return RedirectToAction("Index");
        }

        // =========================
        // (c) HR ADMIN: EDIT MISSION - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Only HR Admin can edit missions
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can edit missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == id);

            if (mission == null)
                return NotFound();

            await LoadDropdowns();
            return View(mission);
        }

        // =========================
        // (c) HR ADMIN: EDIT MISSION - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int MissionId, string Destination, DateTime? StartDate, DateTime? EndDate, string Status, int EmployeeId, int? ManagerId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Only HR Admin can edit missions
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can edit missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions.FirstOrDefaultAsync(m => m.MissionId == MissionId);
            if (mission == null)
                return NotFound();

            // Validation
            if (string.IsNullOrWhiteSpace(Destination))
            {
                ViewBag.Error = "Destination is required.";
                await LoadDropdowns();
                return View(mission);
            }

            if (!StartDate.HasValue)
            {
                ViewBag.Error = "Start date is required.";
                await LoadDropdowns();
                return View(mission);
            }

            if (EndDate.HasValue && EndDate < StartDate)
            {
                ViewBag.Error = "End date cannot be before start date.";
                await LoadDropdowns();
                return View(mission);
            }

            // Track changes for notifications
            int? oldManagerId = mission.ManagerId;
            int? oldEmployeeId = mission.EmployeeId;
            string oldStatus = mission.Status;

            // Update mission
            mission.Destination = Destination;
            mission.StartDate = DateOnly.FromDateTime(StartDate.Value);
            mission.EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null;
            mission.Status = Status;
            mission.EmployeeId = EmployeeId;
            mission.ManagerId = ManagerId;

            await _context.SaveChangesAsync();

            // Get employee name
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);
            string employeeName = employee?.FullName ?? "Employee";

            // ========================================
            // SEND NOTIFICATION IF EMPLOYEE CHANGED
            // ========================================
            if (oldEmployeeId != EmployeeId)
            {
                await SendNotification(
                    EmployeeId,
                    $"You have been assigned a mission to '{Destination}' starting on {StartDate.Value:MMM dd, yyyy}.",
                    "Mission",
                    "Normal"
                );
            }

            // ========================================
            // SEND NOTIFICATION IF NEW MANAGER ASSIGNED
            // ========================================
            if (ManagerId.HasValue && oldManagerId != ManagerId)
            {
                await SendNotification(
                    ManagerId.Value,
                    $"A mission requires your approval: {employeeName} is assigned to travel to '{Destination}'. Please review and approve/reject.",
                    "Mission",
                    "High"
                );
            }

            // ========================================
            // SEND NOTIFICATION IF STATUS CHANGED
            // ========================================
            if (oldStatus != Status && oldEmployeeId == EmployeeId)
            {
                string urgency = (Status == "Approved" || Status == "Completed") ? "Normal" : "High";
                await SendNotification(
                    EmployeeId,
                    $"Your mission to '{Destination}' status has been changed to '{Status}'.",
                    "Mission",
                    urgency
                );
            }

            TempData["Success"] = "Mission updated successfully!";
            return RedirectToAction("Index");
        }

        // =========================
        // (b) LINE MANAGER: APPROVE MISSION - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Only Line Manager can approve missions
            if (!isLineManager)
            {
                TempData["Error"] = "Access denied. Only Line Managers can approve missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == id);

            if (mission == null)
                return NotFound();

            // Check if this manager is assigned to this mission
            if (mission.ManagerId != managerId)
            {
                TempData["Error"] = "You are not authorized to approve this mission.";
                return RedirectToAction("Index");
            }

            // Check if mission is pending
            if (mission.Status != "Pending")
            {
                TempData["Error"] = "This mission has already been processed.";
                return RedirectToAction("Index");
            }

            return View(mission);
        }

        // =========================
        // (b) LINE MANAGER: APPROVE MISSION - POST
        // SENDS NOTIFICATION TO EMPLOYEE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveConfirm(int MissionId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Only Line Manager can approve missions
            if (!isLineManager)
            {
                TempData["Error"] = "Access denied. Only Line Managers can approve missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == MissionId);

            if (mission == null)
                return NotFound();

            // Check if this manager is assigned to this mission
            if (mission.ManagerId != managerId)
            {
                TempData["Error"] = "You are not authorized to approve this mission.";
                return RedirectToAction("Index");
            }

            // Update status to Approved
            mission.Status = "Approved";
            await _context.SaveChangesAsync();

            // Get manager name for notification
            var manager = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == managerId);
            string managerName = manager?.FullName ?? "Your manager";

            // ========================================
            // SEND NOTIFICATION TO EMPLOYEE
            // ========================================
            if (mission.EmployeeId.HasValue)
            {
                await SendNotification(
                    mission.EmployeeId.Value,
                    $"Great news! Your mission to '{mission.Destination}' has been APPROVED by {managerName}. Start date: {mission.StartDate?.ToString("MMM dd, yyyy")}.",
                    "Mission",
                    "Normal"
                );
            }

            TempData["Success"] = $"Mission to '{mission.Destination}' for {mission.Employee?.FullName} has been approved! Employee notified.";
            return RedirectToAction("Index");
        }

        // =========================
        // (b) LINE MANAGER: REJECT MISSION - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Only Line Manager can reject missions
            if (!isLineManager)
            {
                TempData["Error"] = "Access denied. Only Line Managers can reject missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == id);

            if (mission == null)
                return NotFound();

            // Check if this manager is assigned to this mission
            if (mission.ManagerId != managerId)
            {
                TempData["Error"] = "You are not authorized to reject this mission.";
                return RedirectToAction("Index");
            }

            // Check if mission is pending
            if (mission.Status != "Pending")
            {
                TempData["Error"] = "This mission has already been processed.";
                return RedirectToAction("Index");
            }

            return View(mission);
        }

        // =========================
        // (b) LINE MANAGER: REJECT MISSION - POST
        // SENDS NOTIFICATION TO EMPLOYEE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectConfirm(int MissionId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Only Line Manager can reject missions
            if (!isLineManager)
            {
                TempData["Error"] = "Access denied. Only Line Managers can reject missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == MissionId);

            if (mission == null)
                return NotFound();

            // Check if this manager is assigned to this mission
            if (mission.ManagerId != managerId)
            {
                TempData["Error"] = "You are not authorized to reject this mission.";
                return RedirectToAction("Index");
            }

            // Update status to Rejected
            mission.Status = "Rejected";
            await _context.SaveChangesAsync();

            // Get manager name for notification
            var manager = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == managerId);
            string managerName = manager?.FullName ?? "Your manager";

            // ========================================
            // SEND NOTIFICATION TO EMPLOYEE
            // ========================================
            if (mission.EmployeeId.HasValue)
            {
                await SendNotification(
                    mission.EmployeeId.Value,
                    $"Your mission to '{mission.Destination}' has been REJECTED by {managerName}. Please contact your manager for more details.",
                    "Mission",
                    "High"
                );
            }

            TempData["Success"] = $"Mission to '{mission.Destination}' for {mission.Employee?.FullName} has been rejected. Employee notified.";
            return RedirectToAction("Index");
        }

        // =========================
        // (c) HR ADMIN: DELETE MISSION - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Only HR Admin can delete missions
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can delete missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .Include(m => m.Manager)
                .FirstOrDefaultAsync(m => m.MissionId == id);

            if (mission == null)
                return NotFound();

            return View(mission);
        }

        // =========================
        // (c) HR ADMIN: DELETE MISSION - POST
        // SENDS NOTIFICATION TO EMPLOYEE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirm(int MissionId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Only HR Admin can delete missions
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can delete missions.";
                return RedirectToAction("Index");
            }

            var mission = await _context.Missions
                .Include(m => m.Employee)
                .FirstOrDefaultAsync(m => m.MissionId == MissionId);

            if (mission == null)
                return NotFound();

            // Store info for notification before deleting
            int? employeeId = mission.EmployeeId;
            string destination = mission.Destination;

            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            // ========================================
            // SEND NOTIFICATION TO EMPLOYEE
            // ========================================
            if (employeeId.HasValue)
            {
                await SendNotification(
                    employeeId.Value,
                    $"Your mission to '{destination}' has been CANCELLED by HR. Please contact HR for more information.",
                    "Mission",
                    "High"
                );
            }

            TempData["Success"] = "Mission deleted successfully! Employee notified.";
            return RedirectToAction("Index");
        }

        // =========================
        // HELPER: Load Dropdowns
        // =========================
        private async Task LoadDropdowns()
        {
            // Get all active employees for dropdown
            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            // Get all line managers for dropdown
            var lineManagerIds = await _context.LineManagers
                .Select(lm => lm.EmployeeId)
                .ToListAsync();

            ViewBag.Managers = await _context.Employees
                .Where(e => lineManagerIds.Contains(e.EmployeeId) && e.IsActive == true)
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }

        // =========================
        // HELPER: Send Notification
        // Creates notification in database
        // =========================
        private async Task SendNotification(int employeeId, string message, string notificationType, string urgency)
        {
            try
            {
                // Create the notification
                var notification = new Notification
                {
                    MessageContent = message,
                    Timestamp = DateTime.Now,
                    Urgency = urgency,
                    ReadStatus = false,
                    NotificationType = notificationType
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Link notification to employee
                var employeeNotification = new EmployeeNotification
                {
                    EmployeeId = employeeId,
                    NotificationId = notification.NotificationId,
                    DeliveryStatus = "Delivered",
                    DeliveredAt = DateTime.Now
                };

                _context.EmployeeNotifications.Add(employeeNotification);
                await _context.SaveChangesAsync();
            }
            catch (System.Exception ex)
            {
                // Log error but don't break the main operation
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }
}