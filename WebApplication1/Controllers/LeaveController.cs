using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class LeaveController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public LeaveController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // ======================================================
        // INDEX - VIEW ALL LEAVE REQUESTS
        // ======================================================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            List<LeaveRequest> leaveRequests;

            if (isSystemAdmin || isHRAdmin)
            {
                leaveRequests = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .Include(l => l.Leave)
                    .OrderByDescending(l => l.RequestId)
                    .ToListAsync();
            }
            else if (isLineManager)
            {
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
                teamIds.Add(employeeId);

                leaveRequests = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .Include(l => l.Leave)
                    .Where(l => teamIds.Contains(l.EmployeeId ?? 0))
                    .OrderByDescending(l => l.RequestId)
                    .ToListAsync();
            }
            else
            {
                leaveRequests = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .Include(l => l.Leave)
                    .Where(l => l.EmployeeId == employeeId)
                    .OrderByDescending(l => l.RequestId)
                    .ToListAsync();
            }

            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CurrentEmployeeId = employeeId;

            ViewBag.TotalRequests = leaveRequests.Count;
            ViewBag.PendingRequests = leaveRequests.Count(l => l.Status == "Pending");
            ViewBag.ApprovedRequests = leaveRequests.Count(l => l.Status == "Approved");
            ViewBag.RejectedRequests = leaveRequests.Count(l => l.Status == "Rejected");

            return View(leaveRequests);
        }

        // ======================================================
        // (a) SUBMIT LEAVE REQUEST - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var leaveTypes = await _context.Leaves.ToListAsync();
            ViewBag.LeaveTypes = leaveTypes;

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            ViewBag.Employee = employee;

            var leaveBalances = new Dictionary<int, LeaveBalanceInfo>();

            foreach (var leave in leaveTypes)
            {
                var entitlement = await _context.LeaveEntitlements
                    .FirstOrDefaultAsync(le => le.EmployeeId == employeeId && le.LeaveTypeId == leave.LeaveId);

                var totalAllowed = entitlement?.Entitlement ?? 0;

                var usedDays = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == employeeId &&
                                l.LeaveId == leave.LeaveId &&
                                l.Status == "Approved" &&
                                l.ApprovalTiming.HasValue &&
                                l.ApprovalTiming.Value.Year == DateTime.Now.Year)
                    .SumAsync(l => l.Duration ?? 0);

                var remaining = (int)totalAllowed - usedDays;

                leaveBalances[leave.LeaveId] = new LeaveBalanceInfo
                {
                    LeaveType = leave.LeaveType ?? "Unknown",
                    TotalAllowed = (int)totalAllowed,
                    UsedDays = usedDays,
                    RemainingDays = remaining > 0 ? remaining : 0
                };
            }

            ViewBag.LeaveBalances = leaveBalances;

            return View();
        }

        // ======================================================
        // (a) SUBMIT LEAVE REQUEST - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int LeaveId, DateTime StartDate, DateTime EndDate, string? Justification, IFormFile? Attachment)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            if (StartDate > EndDate)
            {
                TempData["Error"] = "End date cannot be before start date.";
                return RedirectToAction("Create");
            }

            if (StartDate < DateTime.Today)
            {
                TempData["Error"] = "Cannot request leave for past dates.";
                return RedirectToAction("Create");
            }

            int duration = 0;
            for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    duration++;
                }
            }

            if (duration <= 0)
            {
                TempData["Error"] = "Leave duration must be at least 1 working day.";
                return RedirectToAction("Create");
            }

            var leaveType = await _context.Leaves.FirstOrDefaultAsync(l => l.LeaveId == LeaveId);
            if (leaveType == null)
            {
                TempData["Error"] = "Invalid leave type selected.";
                return RedirectToAction("Create");
            }

            var entitlement = await _context.LeaveEntitlements
                .FirstOrDefaultAsync(le => le.EmployeeId == employeeId && le.LeaveTypeId == LeaveId);

            var totalAllowed = entitlement?.Entitlement ?? 0;

            var usedDays = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId &&
                            l.LeaveId == LeaveId &&
                            l.Status == "Approved" &&
                            l.ApprovalTiming.HasValue &&
                            l.ApprovalTiming.Value.Year == DateTime.Now.Year)
                .SumAsync(l => l.Duration ?? 0);

            var remainingDays = (int)totalAllowed - usedDays;

            if (duration > remainingDays && leaveType.LeaveType != "Unpaid Leave" && totalAllowed > 0)
            {
                TempData["Error"] = $"Insufficient leave balance. You have {remainingDays} days remaining for {leaveType.LeaveType}.";
                return RedirectToAction("Create");
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveId = LeaveId,
                Duration = duration,
                Justification = Justification,
                Status = "Pending",
                ApprovalTiming = null
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            if (Attachment != null && Attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "leave");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{employeeId}_{DateTime.Now:yyyyMMddHHmmss}_{Attachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }

                var leaveDocument = new LeaveDocument
                {
                    LeaveRequestId = leaveRequest.RequestId,
                    FilePath = $"/uploads/leave/{uniqueFileName}",
                    UploadedAt = DateTime.Now
                };

                _context.LeaveDocuments.Add(leaveDocument);
                await _context.SaveChangesAsync();
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee?.ManagerId.HasValue == true)
            {
                await SendNotification(
                    employee.ManagerId.Value,
                    $"{employee.FullName} has submitted a leave request for {duration} day(s) ({leaveType.LeaveType}).",
                    "Leave",
                    "Normal"
                );
            }

            TempData["Success"] = $"Leave request submitted successfully for {duration} day(s)!";
            return RedirectToAction("MyLeaveRequests");
        }

        // ======================================================
        // (b) VIEW MY LEAVE REQUESTS / HISTORY
        // ======================================================
        public async Task<IActionResult> MyLeaveRequests()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var myRequests = await _context.LeaveRequests
                .Include(l => l.Leave)
                .Include(l => l.LeaveDocuments)
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.RequestId)
                .ToListAsync();

            ViewBag.TotalRequests = myRequests.Count;
            ViewBag.PendingRequests = myRequests.Count(l => l.Status == "Pending");
            ViewBag.ApprovedRequests = myRequests.Count(l => l.Status == "Approved");
            ViewBag.RejectedRequests = myRequests.Count(l => l.Status == "Rejected");

            return View(myRequests);
        }

        // ======================================================
        // (b) VIEW LEAVE BALANCE
        // ======================================================
        public async Task<IActionResult> MyBalance()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            ViewBag.Employee = employee;

            var leaveTypes = await _context.Leaves.ToListAsync();
            var balances = new List<LeaveBalanceViewModel>();

            foreach (var leave in leaveTypes)
            {
                var entitlement = await _context.LeaveEntitlements
                    .FirstOrDefaultAsync(le => le.EmployeeId == employeeId && le.LeaveTypeId == leave.LeaveId);

                var totalAllowed = (int)(entitlement?.Entitlement ?? 0);

                var usedDays = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == employeeId &&
                                l.LeaveId == leave.LeaveId &&
                                l.Status == "Approved" &&
                                l.ApprovalTiming.HasValue &&
                                l.ApprovalTiming.Value.Year == DateTime.Now.Year)
                    .SumAsync(l => l.Duration ?? 0);

                var pendingDays = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == employeeId &&
                                l.LeaveId == leave.LeaveId &&
                                l.Status == "Pending")
                    .SumAsync(l => l.Duration ?? 0);

                var remaining = totalAllowed - usedDays;

                balances.Add(new LeaveBalanceViewModel
                {
                    LeaveId = leave.LeaveId,
                    LeaveType = leave.LeaveType ?? "Unknown",
                    TotalAllowed = totalAllowed,
                    UsedDays = usedDays,
                    PendingDays = pendingDays,
                    RemainingDays = remaining > 0 ? remaining : 0,
                    Description = leave.LeaveDescription
                });
            }

            return View(balances);
        }

        // ======================================================
        // VIEW LEAVE REQUEST DETAILS
        // ======================================================
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .Include(l => l.LeaveDocuments)
                .FirstOrDefaultAsync(l => l.RequestId == id);

            if (leaveRequest == null)
                return NotFound();

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                if (isLineManager)
                {
                    var isTeamMember = await _context.Employees
                        .AnyAsync(e => e.EmployeeId == leaveRequest.EmployeeId && e.ManagerId == employeeId);

                    if (!isTeamMember && leaveRequest.EmployeeId != employeeId)
                    {
                        TempData["Error"] = "Access denied.";
                        return RedirectToAction("Index");
                    }
                }
                else if (leaveRequest.EmployeeId != employeeId)
                {
                    TempData["Error"] = "Access denied.";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.CanApprove = (isSystemAdmin || isHRAdmin || isLineManager) &&
                                 leaveRequest.Status == "Pending" &&
                                 leaveRequest.EmployeeId != employeeId;
            ViewBag.CanOverride = (isSystemAdmin || isHRAdmin) && leaveRequest.Status != "Pending";

            return View(leaveRequest);
        }

        // ======================================================
        // APPROVE LEAVE REQUEST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int approverId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .FirstOrDefaultAsync(l => l.RequestId == id);

            if (leaveRequest == null)
                return NotFound();

            if (leaveRequest.Status != "Pending")
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction("Index");
            }

            leaveRequest.Status = "Approved";
            leaveRequest.ApprovalTiming = DateTime.Now;

            await _context.SaveChangesAsync();

            // Sync with attendance
            await SyncLeaveWithAttendance(leaveRequest);

            if (leaveRequest.EmployeeId.HasValue)
            {
                await SendNotification(
                    leaveRequest.EmployeeId.Value,
                    $"Your leave request for {leaveRequest.Duration} day(s) has been approved.",
                    "Leave",
                    "Normal"
                );
            }

            TempData["Success"] = "Leave request approved successfully!";
            return RedirectToAction("Index");
        }

        // ======================================================
        // REJECT LEAVE REQUEST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? RejectionReason)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int approverId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .FirstOrDefaultAsync(l => l.RequestId == id);

            if (leaveRequest == null)
                return NotFound();

            if (leaveRequest.Status != "Pending")
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction("Index");
            }

            leaveRequest.Status = "Rejected";
            leaveRequest.ApprovalTiming = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(RejectionReason))
            {
                leaveRequest.Justification = (leaveRequest.Justification ?? "") + " [Rejected: " + RejectionReason + "]";
            }

            await _context.SaveChangesAsync();

            if (leaveRequest.EmployeeId.HasValue)
            {
                var message = $"Your leave request for {leaveRequest.Duration} day(s) has been rejected.";
                if (!string.IsNullOrWhiteSpace(RejectionReason))
                {
                    message += $" Reason: {RejectionReason}";
                }

                await SendNotification(
                    leaveRequest.EmployeeId.Value,
                    message,
                    "Leave",
                    "High"
                );
            }

            TempData["Success"] = "Leave request rejected.";
            return RedirectToAction("Index");
        }

        // ======================================================
        // CANCEL LEAVE REQUEST (by employee)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.RequestId == id && l.EmployeeId == employeeId);

            if (leaveRequest == null)
            {
                TempData["Error"] = "Leave request not found.";
                return RedirectToAction("MyLeaveRequests");
            }

            if (leaveRequest.Status != "Pending")
            {
                TempData["Error"] = "Only pending requests can be cancelled.";
                return RedirectToAction("MyLeaveRequests");
            }

            leaveRequest.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request cancelled successfully.";
            return RedirectToAction("MyLeaveRequests");
        }

        // ======================================================
        // TEAM LEAVE CALENDAR (for Managers)
        // ======================================================
        public async Task<IActionResult> TeamCalendar()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isLineManager && !isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only Managers can view team calendar.";
                return RedirectToAction("Index");
            }

            List<LeaveRequest> approvedLeaves;

            if (isSystemAdmin || isHRAdmin)
            {
                approvedLeaves = await _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.Leave)
                    .Where(l => l.Status == "Approved")
                    .OrderByDescending(l => l.ApprovalTiming)
                    .Take(100)
                    .ToListAsync();
            }
            else
            {
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();

                approvedLeaves = await _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.Leave)
                    .Where(l => teamIds.Contains(l.EmployeeId ?? 0) && l.Status == "Approved")
                    .OrderByDescending(l => l.ApprovalTiming)
                    .ToListAsync();
            }

            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;

            return View(approvedLeaves);
        }

        // ======================================================
        // MANAGER: VIEW TEAM PENDING REQUESTS
        // ======================================================
        public async Task<IActionResult> TeamPendingRequests()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isLineManager && !isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only Managers can view team pending requests.";
                return RedirectToAction("Index");
            }

            List<LeaveRequest> pendingRequests;

            if (isSystemAdmin || isHRAdmin)
            {
                pendingRequests = await _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.Leave)
                    .Where(l => l.Status == "Pending")
                    .OrderBy(l => l.RequestId)
                    .ToListAsync();
            }
            else
            {
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == managerId && e.IsActive == true)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();

                pendingRequests = await _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.Leave)
                    .Where(l => teamIds.Contains(l.EmployeeId ?? 0) && l.Status == "Pending")
                    .OrderBy(l => l.RequestId)
                    .ToListAsync();
            }

            var teamMembers = isSystemAdmin || isHRAdmin
                ? await _context.Employees.Where(e => e.IsActive == true).ToListAsync()
                : await _context.Employees.Where(e => e.ManagerId == managerId && e.IsActive == true).ToListAsync();

            ViewBag.TeamMembers = teamMembers;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.PendingCount = pendingRequests.Count;

            var leavePatterns = new Dictionary<int, EmployeeLeavePattern>();
            foreach (var request in pendingRequests)
            {
                if (request.EmployeeId.HasValue && !leavePatterns.ContainsKey(request.EmployeeId.Value))
                {
                    leavePatterns[request.EmployeeId.Value] = await GetEmployeeLeavePattern(request.EmployeeId.Value);
                }
            }
            ViewBag.LeavePatterns = leavePatterns;

            return View(pendingRequests);
        }

        // ======================================================
        // MANAGER: APPROVE WITH COMMENTS
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithComments(int id, string? ApprovalComments)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int approverId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .FirstOrDefaultAsync(l => l.RequestId == id);

            if (leaveRequest == null)
                return NotFound();

            if (leaveRequest.Status != "Pending")
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction("TeamPendingRequests");
            }

            leaveRequest.Status = "Approved";
            leaveRequest.ApprovalTiming = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(ApprovalComments))
            {
                leaveRequest.Justification = (leaveRequest.Justification ?? "") + " [Manager Note: " + ApprovalComments + "]";
            }

            await _context.SaveChangesAsync();

            // Sync with attendance
            await SyncLeaveWithAttendance(leaveRequest);

            if (leaveRequest.EmployeeId.HasValue)
            {
                var message = $"Your leave request for {leaveRequest.Duration} day(s) ({leaveRequest.Leave?.LeaveType}) has been approved.";
                if (!string.IsNullOrWhiteSpace(ApprovalComments))
                {
                    message += $" Note: {ApprovalComments}";
                }

                await SendNotification(
                    leaveRequest.EmployeeId.Value,
                    message,
                    "Leave",
                    "Normal"
                );
            }

            TempData["Success"] = $"Leave request for {leaveRequest.Employee?.FullName} approved successfully!";
            return RedirectToAction("TeamPendingRequests");
        }

        // ======================================================
        // MANAGER: FLAG IRREGULAR LEAVE PATTERNS
        // ======================================================
        public async Task<IActionResult> LeavePatterns()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isLineManager && !isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only Managers can view leave patterns.";
                return RedirectToAction("Index");
            }

            List<Employee> teamMembers;

            if (isSystemAdmin || isHRAdmin)
            {
                teamMembers = await _context.Employees
                    .Include(e => e.Department)
                    .Where(e => e.IsActive == true)
                    .ToListAsync();
            }
            else
            {
                teamMembers = await _context.Employees
                    .Include(e => e.Department)
                    .Where(e => e.ManagerId == managerId && e.IsActive == true)
                    .ToListAsync();
            }

            var patterns = new List<EmployeeLeavePatternViewModel>();

            foreach (var employee in teamMembers)
            {
                var pattern = await GetEmployeeLeavePattern(employee.EmployeeId);

                patterns.Add(new EmployeeLeavePatternViewModel
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.FullName ?? "Unknown",
                    Department = employee.Department?.DepartmentName ?? "N/A",
                    TotalLeaveDays = pattern.TotalLeaveDays,
                    MondayLeaves = pattern.MondayLeaves,
                    FridayLeaves = pattern.FridayLeaves,
                    ConsecutiveShortLeaves = pattern.ConsecutiveShortLeaves,
                    LastMinuteRequests = pattern.LastMinuteRequests,
                    SickLeavesThisYear = pattern.SickLeavesThisYear,
                    PatternFlags = pattern.PatternFlags,
                    RiskLevel = pattern.RiskLevel,
                    IsFlagged = pattern.IsFlagged
                });
            }

            patterns = patterns.OrderByDescending(p => p.IsFlagged)
                               .ThenByDescending(p => p.RiskLevel)
                               .ThenBy(p => p.EmployeeName)
                               .ToList();

            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.TotalEmployees = patterns.Count;
            ViewBag.FlaggedEmployees = patterns.Count(p => p.IsFlagged);

            return View(patterns);
        }

        // ======================================================
        // MANAGER: VIEW EMPLOYEE LEAVE DETAIL PATTERN
        // ======================================================
        public async Task<IActionResult> EmployeeLeaveDetail(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isLineManager && !isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            if (!isSystemAdmin && !isHRAdmin)
            {
                if (employee.ManagerId != managerId)
                {
                    TempData["Error"] = "Access denied. This employee is not in your team.";
                    return RedirectToAction("LeavePatterns");
                }
            }

            var leaveRequests = await _context.LeaveRequests
                .Include(l => l.Leave)
                .Where(l => l.EmployeeId == id)
                .OrderByDescending(l => l.RequestId)
                .ToListAsync();

            var pattern = await GetEmployeeLeavePattern(id);

            var leaveTypes = await _context.Leaves.ToListAsync();
            var balances = new List<LeaveBalanceViewModel>();

            foreach (var leave in leaveTypes)
            {
                var entitlement = await _context.LeaveEntitlements
                    .FirstOrDefaultAsync(le => le.EmployeeId == id && le.LeaveTypeId == leave.LeaveId);

                var totalAllowed = (int)(entitlement?.Entitlement ?? 0);

                var usedDays = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == id &&
                                l.LeaveId == leave.LeaveId &&
                                l.Status == "Approved" &&
                                l.ApprovalTiming.HasValue &&
                                l.ApprovalTiming.Value.Year == DateTime.Now.Year)
                    .SumAsync(l => l.Duration ?? 0);

                balances.Add(new LeaveBalanceViewModel
                {
                    LeaveId = leave.LeaveId,
                    LeaveType = leave.LeaveType ?? "Unknown",
                    TotalAllowed = totalAllowed,
                    UsedDays = usedDays,
                    RemainingDays = totalAllowed - usedDays > 0 ? totalAllowed - usedDays : 0
                });
            }

            ViewBag.Employee = employee;
            ViewBag.LeaveRequests = leaveRequests;
            ViewBag.Pattern = pattern;
            ViewBag.Balances = balances;

            return View();
        }

        // ======================================================
        // MANAGER: FLAG EMPLOYEE (Send note to HR)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlagEmployee(int employeeId, string? FlagReason, bool SendToHR)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            var managerName = HttpContext.Session.GetString("EmployeeName") ?? "Manager";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isLineManager && !isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("LeavePatterns");
            }

            if (SendToHR)
            {
                var message = $"Leave Pattern Alert: {employee.FullName} has been flagged by {managerName}. Reason: {FlagReason ?? "No reason provided"}";

                var hrAdminRoleIds = await _context.EmployeeRoles
                    .Where(er => er.RoleId == 1 || er.RoleId == 2)
                    .Select(er => er.EmployeeId)
                    .Distinct()
                    .ToListAsync();

                var adminsToNotify = await _context.Employees
                    .Where(e => hrAdminRoleIds.Contains(e.EmployeeId) && e.IsActive == true && e.EmployeeId != managerId)
                    .ToListAsync();

                foreach (var admin in adminsToNotify)
                {
                    await SendNotification(admin.EmployeeId, message, "Leave", "High");
                }

                if (adminsToNotify.Any())
                {
                    TempData["Success"] = $"Employee {employee.FullName} has been flagged and {adminsToNotify.Count} admin(s) have been notified.";
                }
                else
                {
                    TempData["Success"] = $"Employee {employee.FullName} has been flagged. (No other admins found to notify)";
                }
            }
            else
            {
                TempData["Success"] = $"Note recorded for {employee.FullName}.";
            }

            return RedirectToAction("EmployeeLeaveDetail", new { id = employeeId });
        }

        // ======================================================
        // HR ADMIN: MANAGE LEAVE TYPES
        // ======================================================
        public async Task<IActionResult> LeaveTypes()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only HR Admins can manage leave types.";
                return RedirectToAction("Index");
            }

            var leaveTypes = await _context.Leaves
                .Include(l => l.LeaveEntitlements)
                .Include(l => l.LeaveRequests)
                .ToListAsync();

            var stats = new Dictionary<int, LeaveTypeStats>();
            foreach (var leave in leaveTypes)
            {
                var totalEntitlements = leave.LeaveEntitlements.Count;
                var totalRequests = leave.LeaveRequests.Count;
                var approvedRequests = leave.LeaveRequests.Count(r => r.Status == "Approved");
                var pendingRequests = leave.LeaveRequests.Count(r => r.Status == "Pending");
                var totalDaysUsed = leave.LeaveRequests.Where(r => r.Status == "Approved").Sum(r => r.Duration ?? 0);

                stats[leave.LeaveId] = new LeaveTypeStats
                {
                    TotalEntitlements = totalEntitlements,
                    TotalRequests = totalRequests,
                    ApprovedRequests = approvedRequests,
                    PendingRequests = pendingRequests,
                    TotalDaysUsed = totalDaysUsed
                };
            }

            ViewBag.Stats = stats;
            ViewBag.IsSystemAdmin = isSystemAdmin;

            return View(leaveTypes);
        }

        // ======================================================
        // HR ADMIN: CREATE LEAVE TYPE - GET
        // ======================================================
        [HttpGet]
        public IActionResult CreateLeaveType()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            return View();
        }

        // ======================================================
        // HR ADMIN: CREATE LEAVE TYPE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLeaveType(string LeaveTypeName, string? LeaveDescription, int DefaultEntitlement, bool AssignToAll)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(LeaveTypeName))
            {
                TempData["Error"] = "Leave type name is required.";
                return RedirectToAction("CreateLeaveType");
            }

            var exists = await _context.Leaves.AnyAsync(l => l.LeaveType == LeaveTypeName);
            if (exists)
            {
                TempData["Error"] = "A leave type with this name already exists.";
                return RedirectToAction("CreateLeaveType");
            }

            var newLeaveType = new Leave
            {
                LeaveType = LeaveTypeName,
                LeaveDescription = LeaveDescription
            };

            _context.Leaves.Add(newLeaveType);
            await _context.SaveChangesAsync();

            if (AssignToAll && DefaultEntitlement > 0)
            {
                var activeEmployees = await _context.Employees
                    .Where(e => e.IsActive == true)
                    .ToListAsync();

                foreach (var emp in activeEmployees)
                {
                    var entitlement = new LeaveEntitlement
                    {
                        EmployeeId = emp.EmployeeId,
                        LeaveTypeId = newLeaveType.LeaveId,
                        Entitlement = DefaultEntitlement
                    };
                    _context.LeaveEntitlements.Add(entitlement);
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Leave type '{LeaveTypeName}' created and assigned to {activeEmployees.Count} employees with {DefaultEntitlement} days entitlement.";
            }
            else
            {
                TempData["Success"] = $"Leave type '{LeaveTypeName}' created successfully.";
            }

            return RedirectToAction("LeaveTypes");
        }

        // ======================================================
        // HR ADMIN: EDIT LEAVE TYPE - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> EditLeaveType(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveType = await _context.Leaves
                .Include(l => l.LeaveEntitlements)
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leaveType == null)
                return NotFound();

            return View(leaveType);
        }

        // ======================================================
        // HR ADMIN: EDIT LEAVE TYPE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLeaveType(int id, string LeaveTypeName, string? LeaveDescription)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveType = await _context.Leaves.FirstOrDefaultAsync(l => l.LeaveId == id);
            if (leaveType == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(LeaveTypeName))
            {
                TempData["Error"] = "Leave type name is required.";
                return RedirectToAction("EditLeaveType", new { id });
            }

            leaveType.LeaveType = LeaveTypeName;
            leaveType.LeaveDescription = LeaveDescription;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Leave type '{LeaveTypeName}' updated successfully.";
            return RedirectToAction("LeaveTypes");
        }

        // ======================================================
        // HR ADMIN: DELETE LEAVE TYPE
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLeaveType(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveType = await _context.Leaves
                .Include(l => l.LeaveRequests)
                .Include(l => l.LeaveEntitlements)
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leaveType == null)
                return NotFound();

            if (leaveType.LeaveRequests.Any())
            {
                TempData["Error"] = $"Cannot delete '{leaveType.LeaveType}' - it has {leaveType.LeaveRequests.Count} leave request(s) associated with it.";
                return RedirectToAction("LeaveTypes");
            }

            _context.LeaveEntitlements.RemoveRange(leaveType.LeaveEntitlements);
            _context.Leaves.Remove(leaveType);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Leave type '{leaveType.LeaveType}' deleted successfully.";
            return RedirectToAction("LeaveTypes");
        }

        // ======================================================
        // HR ADMIN: MANAGE ENTITLEMENTS - LIST ALL EMPLOYEES
        // ======================================================
        public async Task<IActionResult> ManageEntitlements(string? search, int? departmentId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only HR Admins can manage entitlements.";
                return RedirectToAction("Index");
            }

            var query = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.IsActive == true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => (e.FirstName + " " + e.LastName).Contains(search) ||
                                         (e.Email != null && e.Email.Contains(search)));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            var employees = await query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();

            var leaveTypes = await _context.Leaves.ToListAsync();
            var entitlements = await _context.LeaveEntitlements.ToListAsync();
            var entitlementDict = entitlements
                .GroupBy(e => e.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();

            ViewBag.LeaveTypes = leaveTypes;
            ViewBag.Entitlements = entitlementDict;
            ViewBag.Departments = departments;
            ViewBag.Search = search;
            ViewBag.DepartmentId = departmentId;

            return View(employees);
        }

        // ======================================================
        // HR ADMIN: EMPLOYEE ENTITLEMENTS - VIEW/EDIT SINGLE EMPLOYEE
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> EmployeeEntitlements(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            var leaveTypes = await _context.Leaves.ToListAsync();
            var entitlements = await _context.LeaveEntitlements
                .Where(le => le.EmployeeId == id)
                .ToListAsync();

            var usageDict = new Dictionary<int, int>();
            foreach (var leave in leaveTypes)
            {
                var used = await _context.LeaveRequests
                    .Where(lr => lr.EmployeeId == id &&
                                 lr.LeaveId == leave.LeaveId &&
                                 lr.Status == "Approved" &&
                                 lr.ApprovalTiming.HasValue &&
                                 lr.ApprovalTiming.Value.Year == DateTime.Now.Year)
                    .SumAsync(lr => lr.Duration ?? 0);
                usageDict[leave.LeaveId] = used;
            }

            ViewBag.Employee = employee;
            ViewBag.LeaveTypes = leaveTypes;
            ViewBag.Entitlements = entitlements.ToDictionary(e => e.LeaveTypeId, e => e.Entitlement ?? 0);
            ViewBag.Usage = usageDict;

            return View();
        }

        // ======================================================
        // HR ADMIN: UPDATE EMPLOYEE ENTITLEMENTS - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEntitlements(int employeeId, Dictionary<int, int> entitlements)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee == null)
                return NotFound();

            foreach (var entry in entitlements)
            {
                var leaveTypeId = entry.Key;
                var days = entry.Value;

                var existing = await _context.LeaveEntitlements
                    .FirstOrDefaultAsync(le => le.EmployeeId == employeeId && le.LeaveTypeId == leaveTypeId);

                if (existing != null)
                {
                    existing.Entitlement = days;
                }
                else if (days > 0)
                {
                    var newEntitlement = new LeaveEntitlement
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = leaveTypeId,
                        Entitlement = days
                    };
                    _context.LeaveEntitlements.Add(newEntitlement);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Leave entitlements updated for {employee.FullName}.";
            return RedirectToAction("EmployeeEntitlements", new { id = employeeId });
        }

        // ======================================================
        // HR ADMIN: BULK UPDATE ENTITLEMENTS
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateEntitlements(int leaveTypeId, int entitlementDays, int? departmentId, bool allEmployees)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var query = _context.Employees.Where(e => e.IsActive == true);

            if (!allEmployees && departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            var employees = await query.ToListAsync();
            int updatedCount = 0;

            foreach (var emp in employees)
            {
                var existing = await _context.LeaveEntitlements
                    .FirstOrDefaultAsync(le => le.EmployeeId == emp.EmployeeId && le.LeaveTypeId == leaveTypeId);

                if (existing != null)
                {
                    existing.Entitlement = entitlementDays;
                }
                else
                {
                    var newEntitlement = new LeaveEntitlement
                    {
                        EmployeeId = emp.EmployeeId,
                        LeaveTypeId = leaveTypeId,
                        Entitlement = entitlementDays
                    };
                    _context.LeaveEntitlements.Add(newEntitlement);
                }
                updatedCount++;
            }

            await _context.SaveChangesAsync();

            var leaveType = await _context.Leaves.FirstOrDefaultAsync(l => l.LeaveId == leaveTypeId);
            TempData["Success"] = $"Updated {leaveType?.LeaveType ?? "leave"} entitlement to {entitlementDays} days for {updatedCount} employees.";
            return RedirectToAction("ManageEntitlements");
        }

        // ======================================================
        // HR ADMIN: OVERRIDE LEAVE DECISION - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> OverrideDecision(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only HR Admins can override decisions.";
                return RedirectToAction("Index");
            }

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                    .ThenInclude(e => e.Department)
                .Include(l => l.Leave)
                .Include(l => l.LeaveDocuments)
                .FirstOrDefaultAsync(l => l.RequestId == id);

            if (leaveRequest == null)
                return NotFound();

            return View(leaveRequest);
        }

        // ======================================================
        // HR ADMIN: OVERRIDE LEAVE DECISION - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OverrideDecision(int id, string NewStatus, string? OverrideReason, bool SyncAttendance)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int hrAdminId = HttpContext.Session.GetInt32("EmployeeId").Value;
            var hrAdminName = HttpContext.Session.GetString("EmployeeName") ?? "HR Admin";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .FirstOrDefaultAsync(l => l.RequestId == id);

            if (leaveRequest == null)
                return NotFound();

            string oldStatus = leaveRequest.Status ?? "Unknown";

            leaveRequest.Status = NewStatus;
            leaveRequest.ApprovalTiming = DateTime.Now;

            var overrideNote = $" [HR Override by {hrAdminName} on {DateTime.Now:yyyy-MM-dd}: Changed from {oldStatus} to {NewStatus}";
            if (!string.IsNullOrWhiteSpace(OverrideReason))
            {
                overrideNote += $" - Reason: {OverrideReason}";
            }
            overrideNote += "]";
            leaveRequest.Justification = (leaveRequest.Justification ?? "") + overrideNote;

            await _context.SaveChangesAsync();

            if (SyncAttendance && NewStatus == "Approved" && leaveRequest.EmployeeId.HasValue)
            {
                await SyncLeaveWithAttendance(leaveRequest);
            }

            if (leaveRequest.EmployeeId.HasValue)
            {
                var message = $"Your leave request #{leaveRequest.RequestId} has been {NewStatus.ToLower()} by HR Admin.";
                if (!string.IsNullOrWhiteSpace(OverrideReason))
                {
                    message += $" Reason: {OverrideReason}";
                }
                await SendNotification(leaveRequest.EmployeeId.Value, message, "Leave", "High");
            }

            TempData["Success"] = $"Leave request #{id} has been overridden to '{NewStatus}'.";
            return RedirectToAction("Index");
        }

        // ======================================================
        // HR ADMIN: SPECIAL LEAVE REPORT
        // ======================================================
        public async Task<IActionResult> SpecialLeaveReport()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var specialKeywords = new[] { "Maternity", "Paternity", "Bereavement", "Medical", "Compassionate", "Study", "Sabbatical" };

            var specialLeaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                    .ThenInclude(e => e.Department)
                .Include(l => l.Leave)
                .Where(l => l.Leave != null && specialKeywords.Any(k => l.Leave.LeaveType.Contains(k)))
                .OrderByDescending(l => l.RequestId)
                .ToListAsync();

            var longLeaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                    .ThenInclude(e => e.Department)
                .Include(l => l.Leave)
                .Where(l => l.Duration >= 5 && !specialKeywords.Any(k => l.Leave != null && l.Leave.LeaveType.Contains(k)))
                .OrderByDescending(l => l.Duration)
                .Take(20)
                .ToListAsync();

            ViewBag.SpecialLeaves = specialLeaves;
            ViewBag.LongLeaves = longLeaves;
            ViewBag.TotalSpecial = specialLeaves.Count;
            ViewBag.PendingSpecial = specialLeaves.Count(l => l.Status == "Pending");
            ViewBag.ApprovedSpecial = specialLeaves.Count(l => l.Status == "Approved");

            return View();
        }

        // ======================================================
        // HELPER: SYNC LEAVE WITH ATTENDANCE
        // ======================================================
        private async Task SyncLeaveWithAttendance(LeaveRequest leaveRequest)
        {
            if (!leaveRequest.EmployeeId.HasValue || !leaveRequest.Duration.HasValue)
                return;

            try
            {
                // Check if there's already an attendance record for today
                var today = DateTime.Today;
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeId == leaveRequest.EmployeeId &&
                                              a.EntryTime.HasValue &&
                                              a.EntryTime.Value.Date == today);

                if (existingAttendance == null)
                {
                    // Create a new attendance record marking the employee as on leave
                    var attendance = new Attendance
                    {
                        EmployeeId = leaveRequest.EmployeeId.Value,
                        EntryTime = DateTime.Today,
                        ExitTime = DateTime.Today,
                        Duration = 0,
                        LoginMethod = "Leave",
                        LogoutMethod = "Leave"
                    };
                    _context.Attendances.Add(attendance);
                    await _context.SaveChangesAsync();

                    // Log the attendance sync
                    var log = new AttendanceLog
                    {
                        AttendanceId = attendance.AttendanceId,
                        Actor = HttpContext.Session.GetInt32("EmployeeId"),
                        Timestamp = DateTime.Now,
                        Reason = $"Auto-synced from approved leave request #{leaveRequest.RequestId} ({leaveRequest.Leave?.LeaveType}) - {leaveRequest.Duration} day(s)"
                    };
                    _context.AttendanceLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update existing attendance to reflect leave
                    existingAttendance.LoginMethod = "Leave";
                    existingAttendance.LogoutMethod = "Leave";

                    // Log the update
                    var log = new AttendanceLog
                    {
                        AttendanceId = existingAttendance.AttendanceId,
                        Actor = HttpContext.Session.GetInt32("EmployeeId"),
                        Timestamp = DateTime.Now,
                        Reason = $"Updated to leave status from approved leave request #{leaveRequest.RequestId}"
                    };
                    _context.AttendanceLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
            }
            catch (System.Exception ex)
            {
                // Log error but don't fail the approval
                Console.WriteLine($"Error syncing leave with attendance: {ex.Message}");
            }
        }

        // ======================================================
        // HELPER: Get Employee Leave Pattern Analysis
        // ======================================================
        private async Task<EmployeeLeavePattern> GetEmployeeLeavePattern(int employeeId)
        {
            var pattern = new EmployeeLeavePattern();
            var currentYear = DateTime.Now.Year;

            var leaveRequests = await _context.LeaveRequests
                .Include(l => l.Leave)
                .Where(l => l.EmployeeId == employeeId &&
                            l.Status == "Approved" &&
                            l.ApprovalTiming.HasValue &&
                            l.ApprovalTiming.Value.Year == currentYear)
                .ToListAsync();

            pattern.TotalLeaveDays = leaveRequests.Sum(l => l.Duration ?? 0);

            foreach (var leave in leaveRequests)
            {
                if (leave.ApprovalTiming.HasValue)
                {
                    var dayOfWeek = leave.ApprovalTiming.Value.DayOfWeek;

                    if (dayOfWeek == DayOfWeek.Monday)
                        pattern.MondayLeaves++;

                    if (dayOfWeek == DayOfWeek.Friday)
                        pattern.FridayLeaves++;
                }

                if (leave.Duration <= 2)
                    pattern.ConsecutiveShortLeaves++;

                if (leave.Leave?.LeaveType?.Contains("Sick") == true)
                    pattern.SickLeavesThisYear++;
            }

            var flags = new List<string>();

            if (pattern.MondayLeaves >= 3)
                flags.Add("Frequent Monday absences");

            if (pattern.FridayLeaves >= 3)
                flags.Add("Frequent Friday absences");

            if (pattern.MondayLeaves + pattern.FridayLeaves >= 5)
                flags.Add("Long weekend pattern detected");

            if (pattern.ConsecutiveShortLeaves >= 5)
                flags.Add("Multiple short leave instances");

            if (pattern.SickLeavesThisYear >= 5)
                flags.Add("High sick leave usage");

            if (pattern.TotalLeaveDays >= 15)
                flags.Add("High overall leave usage");

            pattern.PatternFlags = flags;
            pattern.IsFlagged = flags.Count > 0;

            pattern.RiskLevel = Math.Min(100,
                (pattern.MondayLeaves * 10) +
                (pattern.FridayLeaves * 10) +
                (pattern.ConsecutiveShortLeaves * 5) +
                (pattern.SickLeavesThisYear * 8) +
                (flags.Count * 15));

            return pattern;
        }

        // ======================================================
        // HELPER: Send Notification
        // ======================================================
        private async Task SendNotification(int employeeId, string message, string notificationType, string urgency)
        {
            try
            {
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
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }

    // ======================================================
    // VIEW MODELS
    // ======================================================
    public class LeaveBalanceInfo
    {
        public string LeaveType { get; set; } = "";
        public int TotalAllowed { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; }
    }

    public class LeaveBalanceViewModel
    {
        public int LeaveId { get; set; }
        public string LeaveType { get; set; } = "";
        public int TotalAllowed { get; set; }
        public int UsedDays { get; set; }
        public int PendingDays { get; set; }
        public int RemainingDays { get; set; }
        public string? Description { get; set; }
    }

    public class EmployeeLeavePattern
    {
        public int TotalLeaveDays { get; set; }
        public int MondayLeaves { get; set; }
        public int FridayLeaves { get; set; }
        public int ConsecutiveShortLeaves { get; set; }
        public int LastMinuteRequests { get; set; }
        public int SickLeavesThisYear { get; set; }
        public List<string> PatternFlags { get; set; } = new List<string>();
        public int RiskLevel { get; set; }
        public bool IsFlagged { get; set; }
    }

    public class EmployeeLeavePatternViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public int TotalLeaveDays { get; set; }
        public int MondayLeaves { get; set; }
        public int FridayLeaves { get; set; }
        public int ConsecutiveShortLeaves { get; set; }
        public int LastMinuteRequests { get; set; }
        public int SickLeavesThisYear { get; set; }
        public List<string> PatternFlags { get; set; } = new List<string>();
        public int RiskLevel { get; set; }
        public bool IsFlagged { get; set; }
    }

    public class LeaveTypeStats
    {
        public int TotalEntitlements { get; set; }
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int PendingRequests { get; set; }
        public int TotalDaysUsed { get; set; }
    }
}