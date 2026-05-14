using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class CalendarController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public CalendarController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // ======================================================
        // CALENDAR VIEW - MAIN PAGE
        // ======================================================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Get departments for filter
            var departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            ViewBag.Departments = departments;

            // Get employees for filter (based on role)
            List<Employee> employees;
            if (isSystemAdmin || isHRAdmin)
            {
                employees = await _context.Employees
                    .Where(e => e.IsActive == true)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
            }
            else if (isLineManager)
            {
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
                teamIds.Add(employeeId);

                employees = await _context.Employees
                    .Where(e => teamIds.Contains(e.EmployeeId) && e.IsActive == true)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
            }
            else
            {
                employees = await _context.Employees
                    .Where(e => e.EmployeeId == employeeId)
                    .ToListAsync();
            }

            ViewBag.Employees = employees;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CurrentEmployeeId = employeeId;

            return View();
        }

        // ======================================================
        // API: GET CALENDAR EVENTS (JSON)
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime? start, DateTime? end, int? departmentId, int? employeeId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Unauthorized();

            int currentEmployeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            var events = new List<object>();

            // Determine which employees to include
            List<int> allowedEmployeeIds;
            if (isSystemAdmin || isHRAdmin)
            {
                if (employeeId.HasValue)
                    allowedEmployeeIds = new List<int> { employeeId.Value };
                else if (departmentId.HasValue)
                    allowedEmployeeIds = await _context.Employees
                        .Where(e => e.DepartmentId == departmentId && e.IsActive == true)
                        .Select(e => e.EmployeeId)
                        .ToListAsync();
                else
                    allowedEmployeeIds = await _context.Employees
                        .Where(e => e.IsActive == true)
                        .Select(e => e.EmployeeId)
                        .ToListAsync();
            }
            else if (isLineManager)
            {
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == currentEmployeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
                teamIds.Add(currentEmployeeId);

                if (employeeId.HasValue && teamIds.Contains(employeeId.Value))
                    allowedEmployeeIds = new List<int> { employeeId.Value };
                else
                    allowedEmployeeIds = teamIds;
            }
            else
            {
                allowedEmployeeIds = new List<int> { currentEmployeeId };
            }

            // ========== LEAVE EVENTS ==========
            List<LeaveRequest> leaveRequests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .Where(l => l.EmployeeId.HasValue &&
                            allowedEmployeeIds.Contains(l.EmployeeId.Value) &&
                            (l.Status == "Approved" || l.Status == "Pending"))
                .ToListAsync();

            foreach (var leave in leaveRequests)
            {
                if (leave.ApprovalTiming.HasValue && leave.Duration.HasValue)
                {
                    var startDate = leave.ApprovalTiming.Value.Date;
                    var endDate = startDate.AddDays(leave.Duration.Value);

                    // Filter by date range if provided
                    if (start.HasValue && endDate < start.Value) continue;
                    if (end.HasValue && startDate > end.Value) continue;

                    string color = leave.Status == "Approved" ? "#e74c3c" : "#f39c12";
                    string leaveType = leave.Leave?.LeaveType ?? "Leave";

                    events.Add(new
                    {
                        id = $"leave_{leave.RequestId}",
                        title = $"{leave.Employee?.FullName} - {leaveType}",
                        start = startDate.ToString("yyyy-MM-dd"),
                        end = endDate.ToString("yyyy-MM-dd"),
                        color = color,
                        textColor = "#ffffff",
                        extendedProps = new
                        {
                            type = "leave",
                            status = leave.Status,
                            employeeName = leave.Employee?.FullName,
                            leaveType = leaveType,
                            duration = leave.Duration,
                            justification = leave.Justification
                        }
                    });
                }
            }

            // ========== SHIFT EVENTS ==========
            var shiftAssignments = await _context.ShiftAssignments
                .Include(sa => sa.Employee)
                .Include(sa => sa.Shift)
                .Where(sa => sa.EmployeeId.HasValue &&
                             allowedEmployeeIds.Contains(sa.EmployeeId.Value) &&
                             sa.Status == "Active")
                .ToListAsync();

            foreach (var assignment in shiftAssignments)
            {
                if (assignment.StartDate.HasValue && assignment.Shift != null)
                {
                    var startDate = assignment.StartDate.Value.ToDateTime(TimeOnly.MinValue);
                    var endDate = assignment.EndDate.HasValue
                        ? assignment.EndDate.Value.ToDateTime(TimeOnly.MinValue).AddDays(1)
                        : startDate.AddMonths(1);

                    // Filter by date range if provided
                    if (start.HasValue && endDate < start.Value) continue;
                    if (end.HasValue && startDate > end.Value) continue;

                    // Color based on shift type
                    string color = assignment.Shift.Type switch
                    {
                        "Morning" => "#3498db",
                        "Evening" => "#9b59b6",
                        "Night" => "#2c3e50",
                        "Split" => "#1abc9c",
                        _ => "#667eea"
                    };

                    events.Add(new
                    {
                        id = $"shift_{assignment.AssignmentId}",
                        title = $"{assignment.Employee?.FullName} - {assignment.Shift.Name}",
                        start = startDate.ToString("yyyy-MM-dd"),
                        end = endDate.ToString("yyyy-MM-dd"),
                        color = color,
                        textColor = "#ffffff",
                        extendedProps = new
                        {
                            type = "shift",
                            shiftName = assignment.Shift.Name,
                            shiftType = assignment.Shift.Type,
                            employeeName = assignment.Employee?.FullName,
                            startTime = assignment.Shift.StartTime?.ToString("hh\\:mm"),
                            endTime = assignment.Shift.EndTime?.ToString("hh\\:mm")
                        }
                    });
                }
            }

            return Json(events);
        }

        // ======================================================
        // API: GET EVENT DETAILS
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetEventDetails(string eventId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Unauthorized();

            if (string.IsNullOrEmpty(eventId))
                return BadRequest();

            var parts = eventId.Split('_');
            if (parts.Length != 2)
                return BadRequest();

            string type = parts[0];
            if (!int.TryParse(parts[1], out int id))
                return BadRequest();

            if (type == "leave")
            {
                var leave = await _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.Leave)
                    .FirstOrDefaultAsync(l => l.RequestId == id);

                if (leave == null)
                    return NotFound();

                return Json(new
                {
                    type = "leave",
                    id = leave.RequestId,
                    employeeName = leave.Employee?.FullName,
                    department = leave.Employee?.Department?.DepartmentName,
                    leaveType = leave.Leave?.LeaveType,
                    status = leave.Status,
                    duration = leave.Duration,
                    startDate = leave.ApprovalTiming?.ToString("MMM dd, yyyy"),
                    justification = leave.Justification
                });
            }
            else if (type == "shift")
            {
                var assignment = await _context.ShiftAssignments
                    .Include(sa => sa.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(sa => sa.Shift)
                    .FirstOrDefaultAsync(sa => sa.AssignmentId == id);

                if (assignment == null)
                    return NotFound();

                return Json(new
                {
                    type = "shift",
                    id = assignment.AssignmentId,
                    shiftId = assignment.ShiftId,
                    employeeName = assignment.Employee?.FullName,
                    department = assignment.Employee?.Department?.DepartmentName,
                    shiftName = assignment.Shift?.Name,
                    shiftType = assignment.Shift?.Type,
                    startTime = assignment.Shift?.StartTime?.ToString("hh\\:mm tt"),
                    endTime = assignment.Shift?.EndTime?.ToString("hh\\:mm tt"),
                    startDate = assignment.StartDate?.ToString("MMM dd, yyyy"),
                    endDate = assignment.EndDate?.ToString("MMM dd, yyyy") ?? "Ongoing",
                    status = assignment.Status
                });
            }

            return NotFound();
        }

        // ======================================================
        // MY CALENDAR - PERSONAL VIEW
        // ======================================================
        public async Task<IActionResult> MyCalendar()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            ViewBag.Employee = employee;

            return View();
        }

        // ======================================================
        // API: GET MY EVENTS ONLY
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetMyEvents(DateTime? start, DateTime? end)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Unauthorized();

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            return await GetEvents(start, end, null, employeeId);
        }
    }
}