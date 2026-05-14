using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        // Default policy values (can be overridden by database)
        private const int DEFAULT_GRACE_PERIOD_MINS = 15;
        private const decimal DEFAULT_LATENESS_DEDUCTION_RATE = 0.5m; // 0.5% per minute late
        private const int DEFAULT_SHORT_TIME_THRESHOLD_MINS = 30; // Less than 30 mins early = short time

        public AttendanceController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // ======================================================
        // (a) INDEX - VIEW ATTENDANCE RECORDS
        // Everyone can view their own, Managers see team, Admins see all
        // ======================================================
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Default date range: current month
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue)
                endDate = DateTime.Now;

            List<Attendance> attendanceRecords;

            if (isSystemAdmin || isHRAdmin)
            {
                // Admins see all attendance
                attendanceRecords = await _context.Attendances
                    .Include(a => a.Employee)
                    .Include(a => a.Shift)
                    .Include(a => a.Exception)
                    .Where(a => a.EntryTime >= startDate && a.EntryTime <= endDate.Value.AddDays(1))
                    .OrderByDescending(a => a.EntryTime)
                    .ToListAsync();
            }
            else if (isLineManager)
            {
                // Managers see their team + own
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
                teamIds.Add(employeeId);

                attendanceRecords = await _context.Attendances
                    .Include(a => a.Employee)
                    .Include(a => a.Shift)
                    .Include(a => a.Exception)
                    .Where(a => teamIds.Contains(a.EmployeeId ?? 0) &&
                                a.EntryTime >= startDate && a.EntryTime <= endDate.Value.AddDays(1))
                    .OrderByDescending(a => a.EntryTime)
                    .ToListAsync();
            }
            else
            {
                // Regular employees see only their own
                attendanceRecords = await _context.Attendances
                    .Include(a => a.Employee)
                    .Include(a => a.Shift)
                    .Include(a => a.Exception)
                    .Where(a => a.EmployeeId == employeeId &&
                                a.EntryTime >= startDate && a.EntryTime <= endDate.Value.AddDays(1))
                    .OrderByDescending(a => a.EntryTime)
                    .ToListAsync();
            }

            // Get today's attendance for current user
            var today = DateTime.Today;
            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId &&
                                          a.EntryTime.HasValue &&
                                          a.EntryTime.Value.Date == today);

            ViewBag.TodayAttendance = todayAttendance;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CurrentEmployeeId = employeeId;

            // Statistics
            ViewBag.TotalRecords = attendanceRecords.Count;
            ViewBag.PresentDays = attendanceRecords.Count(a => a.EntryTime.HasValue);
            ViewBag.LateDays = attendanceRecords.Count(a => IsLate(a));
            ViewBag.EarlyDepartures = attendanceRecords.Count(a => IsEarlyDeparture(a));

            return View(attendanceRecords);
        }

        // ======================================================
        // (a) CLOCK IN - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> ClockIn()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Check if already clocked in today
            var today = DateTime.Today;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId &&
                                          a.EntryTime.HasValue &&
                                          a.EntryTime.Value.Date == today);

            if (existingAttendance != null)
            {
                TempData["Error"] = "You have already clocked in today.";
                return RedirectToAction("Index");
            }

            // Get current shift assignment
            var currentShift = await GetCurrentShiftForEmployee(employeeId);
            ViewBag.CurrentShift = currentShift;

            return View();
        }

        // ======================================================
        // (a) CLOCK IN - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockIn(string LoginMethod, string? Notes)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Check if already clocked in today
            var today = DateTime.Today;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId &&
                                          a.EntryTime.HasValue &&
                                          a.EntryTime.Value.Date == today);

            if (existingAttendance != null)
            {
                TempData["Error"] = "You have already clocked in today.";
                return RedirectToAction("Index");
            }

            // Get current shift
            var currentShift = await GetCurrentShiftForEmployee(employeeId);

            // Create attendance record
            var attendance = new Attendance
            {
                EmployeeId = employeeId,
                ShiftId = currentShift?.ShiftId,
                EntryTime = DateTime.Now,
                LoginMethod = LoginMethod ?? "Web",
                Duration = 0
            };

            // Check for lateness and apply exception if needed
            if (currentShift != null && currentShift.StartTime.HasValue)
            {
                var shiftStart = today.Add(currentShift.StartTime.Value.ToTimeSpan());
                var gracePeriod = await GetGracePeriodMinutes();
                var lateThreshold = shiftStart.AddMinutes(gracePeriod);

                if (DateTime.Now > lateThreshold)
                {
                    // Employee is late - create exception
                    var lateMinutes = (int)(DateTime.Now - shiftStart).TotalMinutes;
                    var exception = new WebApplication1.Models.Exception
                    {
                        Name = $"Late Arrival - {lateMinutes} minutes",
                        Category = "Lateness",
                        Date = DateOnly.FromDateTime(today),
                        Status = "Applied"
                    };
                    _context.Exceptions.Add(exception);
                    await _context.SaveChangesAsync();

                    attendance.ExceptionId = exception.ExceptionId;
                }
            }

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Create attendance log
            await CreateAttendanceLog(attendance.AttendanceId, employeeId, "Clock In" + (Notes != null ? $": {Notes}" : ""));

            TempData["Success"] = $"Successfully clocked in at {DateTime.Now:hh:mm tt}!";
            return RedirectToAction("Index");
        }

        // ======================================================
        // (a) CLOCK OUT - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> ClockOut()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Get today's attendance
            var today = DateTime.Today;
            var attendance = await _context.Attendances
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId &&
                                          a.EntryTime.HasValue &&
                                          a.EntryTime.Value.Date == today);

            if (attendance == null)
            {
                TempData["Error"] = "You haven't clocked in today. Please clock in first.";
                return RedirectToAction("Index");
            }

            if (attendance.ExitTime.HasValue)
            {
                TempData["Error"] = "You have already clocked out today.";
                return RedirectToAction("Index");
            }

            ViewBag.Attendance = attendance;
            ViewBag.ClockInTime = attendance.EntryTime;

            return View();
        }

        // ======================================================
        // (a) CLOCK OUT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockOut(string LogoutMethod, string? Notes)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Get today's attendance
            var today = DateTime.Today;
            var attendance = await _context.Attendances
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId &&
                                          a.EntryTime.HasValue &&
                                          a.EntryTime.Value.Date == today);

            if (attendance == null)
            {
                TempData["Error"] = "You haven't clocked in today.";
                return RedirectToAction("Index");
            }

            if (attendance.ExitTime.HasValue)
            {
                TempData["Error"] = "You have already clocked out today.";
                return RedirectToAction("Index");
            }

            attendance.ExitTime = DateTime.Now;
            attendance.LogoutMethod = LogoutMethod ?? "Web";

            // Calculate duration in minutes
            if (attendance.EntryTime.HasValue)
            {
                attendance.Duration = (int)(attendance.ExitTime.Value - attendance.EntryTime.Value).TotalMinutes;
            }

            // Check for early departure and apply exception if needed
            if (attendance.Shift != null && attendance.Shift.EndTime.HasValue)
            {
                var shiftEnd = today.Add(attendance.Shift.EndTime.Value.ToTimeSpan());
                var shortTimeThreshold = shiftEnd.AddMinutes(-DEFAULT_SHORT_TIME_THRESHOLD_MINS);

                if (DateTime.Now < shortTimeThreshold)
                {
                    // Employee leaving early - create exception
                    var earlyMinutes = (int)(shiftEnd - DateTime.Now).TotalMinutes;
                    var exception = new WebApplication1.Models.Exception
                    {
                        Name = $"Early Departure - {earlyMinutes} minutes",
                        Category = "Early Departure",
                        Date = DateOnly.FromDateTime(today),
                        Status = "Applied"
                    };
                    _context.Exceptions.Add(exception);
                    await _context.SaveChangesAsync();

                    // If no existing exception, assign this one
                    if (!attendance.ExceptionId.HasValue)
                    {
                        attendance.ExceptionId = exception.ExceptionId;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Create attendance log
            await CreateAttendanceLog(attendance.AttendanceId, employeeId, "Clock Out" + (Notes != null ? $": {Notes}" : ""));

            var workedHours = attendance.Duration.HasValue ? attendance.Duration.Value / 60.0 : 0;
            TempData["Success"] = $"Successfully clocked out at {DateTime.Now:hh:mm tt}! You worked {workedHours:F1} hours today.";
            return RedirectToAction("Index");
        }

        // ======================================================
        // (b) REQUEST CORRECTION - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> RequestCorrection()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Get recent attendance for dropdown
            var recentAttendance = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.EntryTime)
                .Take(30)
                .ToListAsync();

            ViewBag.RecentAttendance = recentAttendance;

            return View();
        }

        // ======================================================
        // (b) REQUEST CORRECTION - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCorrection(DateTime Date, string CorrectionType, string Reason)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            // Validation
            if (string.IsNullOrWhiteSpace(CorrectionType))
            {
                ViewBag.Error = "Please select a correction type.";
                ViewBag.RecentAttendance = await _context.Attendances
                    .Where(a => a.EmployeeId == employeeId)
                    .OrderByDescending(a => a.EntryTime)
                    .Take(30)
                    .ToListAsync();
                return View();
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                ViewBag.Error = "Please provide a reason for the correction request.";
                ViewBag.RecentAttendance = await _context.Attendances
                    .Where(a => a.EmployeeId == employeeId)
                    .OrderByDescending(a => a.EntryTime)
                    .Take(30)
                    .ToListAsync();
                return View();
            }

            // Check for existing pending request for same date
            var existingRequest = await _context.AttendanceCorrectionRequests
                .FirstOrDefaultAsync(r => r.EmployeeId == employeeId &&
                                          r.Date == DateOnly.FromDateTime(Date) &&
                                          r.Status == "Pending");

            if (existingRequest != null)
            {
                ViewBag.Error = "You already have a pending correction request for this date.";
                ViewBag.RecentAttendance = await _context.Attendances
                    .Where(a => a.EmployeeId == employeeId)
                    .OrderByDescending(a => a.EntryTime)
                    .Take(30)
                    .ToListAsync();
                return View();
            }

            var request = new AttendanceCorrectionRequest
            {
                EmployeeId = employeeId,
                Date = DateOnly.FromDateTime(Date),
                CorrectionType = CorrectionType,
                Reason = Reason,
                Status = "Pending",
                RecordedBy = null
            };

            _context.AttendanceCorrectionRequests.Add(request);
            await _context.SaveChangesAsync();

            // Notify manager
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee?.ManagerId.HasValue == true)
            {
                await SendNotification(
                    employee.ManagerId.Value,
                    $"{employee.FullName} has submitted an attendance correction request for {Date:MMM dd, yyyy}.",
                    "Attendance",
                    "Normal"
                );
            }

            TempData["Success"] = "Attendance correction request submitted successfully!";
            return RedirectToAction("MyCorrectionRequests");
        }

        // ======================================================
        // VIEW MY CORRECTION REQUESTS
        // ======================================================
        public async Task<IActionResult> MyCorrectionRequests()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var requests = await _context.AttendanceCorrectionRequests
                .Include(r => r.RecordedByNavigation)
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return View(requests);
        }

        // ======================================================
        // MANAGE CORRECTION REQUESTS (Manager/Admin)
        // ======================================================
        public async Task<IActionResult> CorrectionRequests()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            List<AttendanceCorrectionRequest> requests;

            if (isSystemAdmin || isHRAdmin)
            {
                // Admins see all requests
                requests = await _context.AttendanceCorrectionRequests
                    .Include(r => r.Employee)
                    .Include(r => r.RecordedByNavigation)
                    .OrderByDescending(r => r.Date)
                    .ToListAsync();
            }
            else
            {
                // Managers see their team's requests
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();

                requests = await _context.AttendanceCorrectionRequests
                    .Include(r => r.Employee)
                    .Include(r => r.RecordedByNavigation)
                    .Where(r => teamIds.Contains(r.EmployeeId ?? 0))
                    .OrderByDescending(r => r.Date)
                    .ToListAsync();
            }

            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;

            return View(requests);
        }

        // ======================================================
        // APPROVE CORRECTION REQUEST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCorrection(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var request = await _context.AttendanceCorrectionRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
                return NotFound();

            request.Status = "Approved";
            request.RecordedBy = employeeId;

            await _context.SaveChangesAsync();

            // Notify employee
            if (request.EmployeeId.HasValue)
            {
                await SendNotification(
                    request.EmployeeId.Value,
                    $"Your attendance correction request for {request.Date?.ToString("MMM dd, yyyy")} has been approved.",
                    "Attendance",
                    "Normal"
                );
            }

            TempData["Success"] = "Correction request approved successfully!";
            return RedirectToAction("CorrectionRequests");
        }

        // ======================================================
        // REJECT CORRECTION REQUEST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCorrection(int id, string? RejectionReason)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var request = await _context.AttendanceCorrectionRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
                return NotFound();

            request.Status = "Rejected";
            request.RecordedBy = employeeId;

            await _context.SaveChangesAsync();

            // Notify employee
            if (request.EmployeeId.HasValue)
            {
                var message = $"Your attendance correction request for {request.Date?.ToString("MMM dd, yyyy")} has been rejected.";
                if (!string.IsNullOrWhiteSpace(RejectionReason))
                {
                    message += $" Reason: {RejectionReason}";
                }

                await SendNotification(
                    request.EmployeeId.Value,
                    message,
                    "Attendance",
                    "High"
                );
            }

            TempData["Success"] = "Correction request rejected.";
            return RedirectToAction("CorrectionRequests");
        }

        // ======================================================
        // (c) SYNC LEAVE WITH ATTENDANCE - System Admin Only
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> SyncLeave()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can sync leave with attendance.";
                return RedirectToAction("Index");
            }

            // Get approved leave requests that don't have corresponding attendance
            var approvedLeaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .Where(l => l.Status == "Approved")
                .OrderByDescending(l => l.ApprovalTiming)
                .Take(50)
                .ToListAsync();

            ViewBag.ApprovedLeaves = approvedLeaves;

            return View();
        }

        // ======================================================
        // (c) SYNC LEAVE WITH ATTENDANCE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncLeave(DateTime StartDate, DateTime EndDate)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            int syncedCount = 0;

            // Get all approved leave requests in date range
            var approvedLeaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.Leave)
                .Where(l => l.Status == "Approved")
                .ToListAsync();

            foreach (var leave in approvedLeaves)
            {
                if (leave.EmployeeId == null || leave.Duration == null)
                    continue;

                // Check for each day in leave period
                var leaveStart = leave.ApprovalTiming?.Date ?? StartDate;
                for (int i = 0; i < leave.Duration; i++)
                {
                    var leaveDate = leaveStart.AddDays(i);

                    if (leaveDate < StartDate || leaveDate > EndDate)
                        continue;

                    // Check if attendance record already exists
                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.EmployeeId == leave.EmployeeId &&
                                                  a.EntryTime.HasValue &&
                                                  a.EntryTime.Value.Date == leaveDate);

                    if (existingAttendance == null)
                    {
                        // Create exception for leave
                        var exception = new WebApplication1.Models.Exception
                        {
                            Name = $"On Leave - {leave.Leave?.LeaveType ?? "Leave"}",
                            Category = "Leave",
                            Date = DateOnly.FromDateTime(leaveDate),
                            Status = "Approved"
                        };
                        _context.Exceptions.Add(exception);
                        await _context.SaveChangesAsync();

                        // Create attendance record marking as on leave
                        var attendance = new Attendance
                        {
                            EmployeeId = leave.EmployeeId,
                            EntryTime = leaveDate.AddHours(9), // Placeholder
                            ExitTime = leaveDate.AddHours(17), // Placeholder
                            Duration = 480, // 8 hours
                            LoginMethod = "Leave",
                            LogoutMethod = "Leave",
                            ExceptionId = exception.ExceptionId
                        };
                        _context.Attendances.Add(attendance);
                        syncedCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully synced {syncedCount} leave records with attendance!";
            return RedirectToAction("SyncLeave");
        }

        // ======================================================
        // (d) SYNC OFFLINE ATTENDANCE - API Endpoint
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> SyncOffline([FromBody] List<OfflineAttendanceDto> offlineRecords)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Json(new { success = false, message = "Not authenticated" });

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            int syncedCount = 0;
            var errors = new List<string>();

            foreach (var record in offlineRecords)
            {
                try
                {
                    // Check if record already exists for this date
                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.EmployeeId == employeeId &&
                                                  a.EntryTime.HasValue &&
                                                  a.EntryTime.Value.Date == record.Date.Date);

                    if (existingAttendance == null && record.ClockIn.HasValue)
                    {
                        var attendance = new Attendance
                        {
                            EmployeeId = employeeId,
                            EntryTime = record.ClockIn,
                            ExitTime = record.ClockOut,
                            LoginMethod = "Offline Sync",
                            LogoutMethod = record.ClockOut.HasValue ? "Offline Sync" : null,
                            Duration = record.ClockOut.HasValue && record.ClockIn.HasValue
                                ? (int)(record.ClockOut.Value - record.ClockIn.Value).TotalMinutes
                                : 0
                        };

                        _context.Attendances.Add(attendance);
                        syncedCount++;
                    }
                    else if (existingAttendance != null && !existingAttendance.ExitTime.HasValue && record.ClockOut.HasValue)
                    {
                        // Update with clock out time
                        existingAttendance.ExitTime = record.ClockOut;
                        existingAttendance.LogoutMethod = "Offline Sync";
                        existingAttendance.Duration = (int)(record.ClockOut.Value - existingAttendance.EntryTime!.Value).TotalMinutes;
                        syncedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    errors.Add($"Error syncing record for {record.Date:yyyy-MM-dd}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                syncedCount = syncedCount,
                errors = errors
            });
        }

        // ======================================================
        // (f) TEAM ATTENDANCE SUMMARY - Managers Only
        // ======================================================
        public async Task<IActionResult> TeamSummary(DateTime? startDate, DateTime? endDate)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isLineManager && !isSystemAdmin && !isHRAdmin)
            {
                TempData["Error"] = "Access denied. Only Managers can view team attendance summary.";
                return RedirectToAction("Index");
            }

            // Default date range: current month
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue)
                endDate = DateTime.Now;

            List<Employee> teamMembers;

            if (isSystemAdmin || isHRAdmin)
            {
                teamMembers = await _context.Employees
                    .Where(e => e.IsActive == true)
                    .ToListAsync();
            }
            else
            {
                teamMembers = await _context.Employees
                    .Where(e => e.ManagerId == employeeId && e.IsActive == true)
                    .ToListAsync();
            }

            var summaryList = new List<TeamAttendanceSummary>();

            foreach (var member in teamMembers)
            {
                var attendanceRecords = await _context.Attendances
                    .Include(a => a.Exception)
                    .Where(a => a.EmployeeId == member.EmployeeId &&
                                a.EntryTime >= startDate &&
                                a.EntryTime <= endDate.Value.AddDays(1))
                    .ToListAsync();

                var summary = new TeamAttendanceSummary
                {
                    EmployeeId = member.EmployeeId,
                    EmployeeName = member.FullName ?? "Unknown",
                    TotalDays = (int)(endDate.Value - startDate.Value).TotalDays + 1,
                    PresentDays = attendanceRecords.Count(a => a.EntryTime.HasValue),
                    LateDays = attendanceRecords.Count(a => IsLate(a)),
                    EarlyDepartures = attendanceRecords.Count(a => IsEarlyDeparture(a)),
                    LeaveDays = attendanceRecords.Count(a => a.Exception?.Category == "Leave"),
                    TotalHoursWorked = attendanceRecords.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value) / 60.0,
                    AverageHoursPerDay = attendanceRecords.Any(a => a.Duration.HasValue)
                        ? attendanceRecords.Where(a => a.Duration.HasValue).Average(a => a.Duration!.Value) / 60.0
                        : 0
                };

                summary.AbsentDays = summary.TotalDays - summary.PresentDays - summary.LeaveDays;
                if (summary.AbsentDays < 0) summary.AbsentDays = 0;

                // Calculate weekdays only
                int weekdays = 0;
                for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                        weekdays++;
                }
                summary.TotalDays = weekdays;
                summary.AbsentDays = Math.Max(0, weekdays - summary.PresentDays - summary.LeaveDays);

                summaryList.Add(summary);
            }

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;

            return View(summaryList);
        }

        // ======================================================
        // VIEW ATTENDANCE DETAILS
        // ======================================================
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .Include(a => a.Shift)
                .Include(a => a.Exception)
                .Include(a => a.AttendanceLogs)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null)
                return NotFound();

            // Check access
            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin)
            {
                if (isLineManager)
                {
                    var isTeamMember = await _context.Employees
                        .AnyAsync(e => e.EmployeeId == attendance.EmployeeId && e.ManagerId == employeeId);

                    if (!isTeamMember && attendance.EmployeeId != employeeId)
                    {
                        TempData["Error"] = "Access denied.";
                        return RedirectToAction("Index");
                    }
                }
                else if (attendance.EmployeeId != employeeId)
                {
                    TempData["Error"] = "Access denied.";
                    return RedirectToAction("Index");
                }
            }

            // Get grace period and calculate status
            var gracePeriod = await GetGracePeriodMinutes();
            ViewBag.GracePeriod = gracePeriod;
            ViewBag.IsLate = IsLate(attendance);
            ViewBag.IsEarlyDeparture = IsEarlyDeparture(attendance);

            return View(attendance);
        }

        // ======================================================
        // HELPER METHODS
        // ======================================================

        private async Task<ShiftSchedule?> GetCurrentShiftForEmployee(int employeeId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Get active shift assignment for employee
            var assignment = await _context.ShiftAssignments
                .Include(sa => sa.Shift)
                .Where(sa => sa.EmployeeId == employeeId &&
                             sa.Status == "Active" &&
                             (!sa.StartDate.HasValue || sa.StartDate <= today) &&
                             (!sa.EndDate.HasValue || sa.EndDate >= today))
                .FirstOrDefaultAsync();

            return assignment?.Shift;
        }

        private async Task<int> GetGracePeriodMinutes()
        {
            var policy = await _context.LatenessPolicies.FirstOrDefaultAsync();
            return policy?.GracePeriodMins ?? DEFAULT_GRACE_PERIOD_MINS;
        }

        private bool IsLate(Attendance attendance)
        {
            if (!attendance.EntryTime.HasValue || attendance.Shift?.StartTime == null)
                return false;

            var shiftStart = attendance.EntryTime.Value.Date.Add(attendance.Shift.StartTime.Value.ToTimeSpan());
            var gracePeriod = TimeSpan.FromMinutes(DEFAULT_GRACE_PERIOD_MINS);

            return attendance.EntryTime.Value > shiftStart.Add(gracePeriod);
        }

        private bool IsEarlyDeparture(Attendance attendance)
        {
            if (!attendance.ExitTime.HasValue || attendance.Shift?.EndTime == null)
                return false;

            var shiftEnd = attendance.ExitTime.Value.Date.Add(attendance.Shift.EndTime.Value.ToTimeSpan());
            var threshold = TimeSpan.FromMinutes(DEFAULT_SHORT_TIME_THRESHOLD_MINS);

            return attendance.ExitTime.Value < shiftEnd.Subtract(threshold);
        }

        private async Task CreateAttendanceLog(int attendanceId, int actorId, string reason)
        {
            var log = new AttendanceLog
            {
                AttendanceId = attendanceId,
                Actor = actorId,
                Timestamp = DateTime.Now,
                Reason = reason
            };

            _context.AttendanceLogs.Add(log);
            await _context.SaveChangesAsync();
        }

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
    // DTO CLASSES
    // ======================================================

    public class OfflineAttendanceDto
    {
        public DateTime Date { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
    }

    public class TeamAttendanceSummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int EarlyDepartures { get; set; }
        public int LeaveDays { get; set; }
        public double TotalHoursWorked { get; set; }
        public double AverageHoursPerDay { get; set; }
    }
}