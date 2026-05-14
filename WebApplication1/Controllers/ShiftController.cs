using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ShiftController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public ShiftController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // ======================================================
        // INDEX - VIEW ALL SHIFTS
        // Everyone can view shifts, but actions are role-based
        // ======================================================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var shifts = await _context.ShiftSchedules
                .Include(s => s.ShiftAssignments)
                .OrderByDescending(s => s.ShiftId)
                .ToListAsync();

            ViewBag.IsSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            ViewBag.IsHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            ViewBag.IsLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            return View(shifts);
        }

        // ======================================================
        // (a) SYSTEM ADMIN: CREATE SHIFT TYPES - GET
        // ======================================================
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can create shift types.";
                return RedirectToAction("Index");
            }

            return View();
        }

        // ======================================================
        // (a) SYSTEM ADMIN: CREATE SHIFT TYPES - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Name, string Type, TimeSpan? StartTime, TimeSpan? EndTime, int? BreakDuration, DateTime? ShiftDate)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can create shift types.";
                return RedirectToAction("Index");
            }

            // Validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                ViewBag.Error = "Shift name is required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(Type))
            {
                ViewBag.Error = "Shift type is required.";
                return View();
            }

            if (!StartTime.HasValue || !EndTime.HasValue)
            {
                ViewBag.Error = "Start time and end time are required.";
                return View();
            }

            var shift = new ShiftSchedule
            {
                Name = Name,
                Type = Type,
                StartTime = TimeOnly.FromTimeSpan(StartTime.Value),
                EndTime = TimeOnly.FromTimeSpan(EndTime.Value),
                BreakDuration = BreakDuration ?? 0,
                ShiftDate = ShiftDate.HasValue ? DateOnly.FromDateTime(ShiftDate.Value) : null,
                Status = "Active"
            };

            _context.ShiftSchedules.Add(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Shift '{Name}' created successfully!";
            return RedirectToAction("Index");
        }

        // ======================================================
        // VIEW SHIFT DETAILS
        // ======================================================
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var shift = await _context.ShiftSchedules
                .Include(s => s.ShiftAssignments)
                    .ThenInclude(sa => sa.Employee)
                .Include(s => s.ShiftCycleAssignments)
                    .ThenInclude(sca => sca.Cycle)
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null)
                return NotFound();

            ViewBag.IsSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            ViewBag.IsHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            ViewBag.IsLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            return View(shift);
        }

        // ======================================================
        // (d) SYSTEM ADMIN: EDIT SHIFT - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can edit shifts.";
                return RedirectToAction("Index");
            }

            var shift = await _context.ShiftSchedules.FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null)
                return NotFound();

            return View(shift);
        }

        // ======================================================
        // (d) SYSTEM ADMIN: EDIT SHIFT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ShiftId, string Name, string Type, TimeSpan? StartTime, TimeSpan? EndTime, int? BreakDuration, DateTime? ShiftDate, string Status)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can edit shifts.";
                return RedirectToAction("Index");
            }

            var shift = await _context.ShiftSchedules.FirstOrDefaultAsync(s => s.ShiftId == ShiftId);

            if (shift == null)
                return NotFound();

            // Validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                ViewBag.Error = "Shift name is required.";
                return View(shift);
            }

            shift.Name = Name;
            shift.Type = Type;
            shift.StartTime = StartTime.HasValue ? TimeOnly.FromTimeSpan(StartTime.Value) : null;
            shift.EndTime = EndTime.HasValue ? TimeOnly.FromTimeSpan(EndTime.Value) : null;
            shift.BreakDuration = BreakDuration;
            shift.ShiftDate = ShiftDate.HasValue ? DateOnly.FromDateTime(ShiftDate.Value) : null;
            shift.Status = Status;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Shift '{Name}' updated successfully!";
            return RedirectToAction("Index");
        }

        // ======================================================
        // SYSTEM ADMIN: DELETE SHIFT - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can delete shifts.";
                return RedirectToAction("Index");
            }

            var shift = await _context.ShiftSchedules
                .Include(s => s.ShiftAssignments)
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null)
                return NotFound();

            return View(shift);
        }

        // ======================================================
        // SYSTEM ADMIN: DELETE SHIFT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirm(int ShiftId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can delete shifts.";
                return RedirectToAction("Index");
            }

            var shift = await _context.ShiftSchedules
                .Include(s => s.ShiftAssignments)
                .FirstOrDefaultAsync(s => s.ShiftId == ShiftId);

            if (shift == null)
                return NotFound();

            // Check if shift has assignments
            if (shift.ShiftAssignments.Any())
            {
                TempData["Error"] = "Cannot delete shift with active assignments. Please remove assignments first.";
                return RedirectToAction("Index");
            }

            _context.ShiftSchedules.Remove(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift deleted successfully!";
            return RedirectToAction("Index");
        }

        // ======================================================
        // (b) HR ADMIN: CONFIGURE SPLIT SHIFTS - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> ConfigureSplit()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can configure split shifts.";
                return RedirectToAction("Index");
            }

            var splitShifts = await _context.ShiftSchedules
                .Where(s => s.Type == "Split")
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(splitShifts);
        }

        // ======================================================
        // (b) HR ADMIN: CREATE SPLIT SHIFT - GET
        // ======================================================
        [HttpGet]
        public IActionResult CreateSplit()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can configure split shifts.";
                return RedirectToAction("Index");
            }

            return View();
        }

        // ======================================================
        // (b) HR ADMIN: CREATE SPLIT SHIFT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSplit(string Name, TimeSpan? StartTime1, TimeSpan? EndTime1, TimeSpan? StartTime2, TimeSpan? EndTime2, int? BreakDuration)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can configure split shifts.";
                return RedirectToAction("Index");
            }

            // Validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                ViewBag.Error = "Shift name is required.";
                return View();
            }

            if (!StartTime1.HasValue || !EndTime1.HasValue || !StartTime2.HasValue || !EndTime2.HasValue)
            {
                ViewBag.Error = "All time fields are required for split shifts.";
                return View();
            }

            // Create split shift (store first part times, break duration represents gap)
            var shift = new ShiftSchedule
            {
                Name = Name,
                Type = "Split",
                StartTime = TimeOnly.FromTimeSpan(StartTime1.Value),
                EndTime = TimeOnly.FromTimeSpan(EndTime2.Value), // End of second part
                BreakDuration = BreakDuration ?? (int)(StartTime2.Value - EndTime1.Value).TotalMinutes,
                Status = "Active"
            };

            _context.ShiftSchedules.Add(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Split shift '{Name}' created successfully!";
            return RedirectToAction("ConfigureSplit");
        }

        // ======================================================
        // (b) HR ADMIN: CONFIGURE ROTATIONAL SHIFTS - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> ConfigureRotational()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can configure rotational shifts.";
                return RedirectToAction("Index");
            }

            var cycles = await _context.ShiftCycles
                .Include(c => c.ShiftCycleAssignments)
                    .ThenInclude(sca => sca.Shift)
                .OrderBy(c => c.CycleName)
                .ToListAsync();

            return View(cycles);
        }

        // ======================================================
        // (b) HR ADMIN: CREATE SHIFT CYCLE - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> CreateCycle()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can create shift cycles.";
                return RedirectToAction("Index");
            }

            ViewBag.Shifts = await _context.ShiftSchedules
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View();
        }

        // ======================================================
        // (b) HR ADMIN: CREATE SHIFT CYCLE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCycle(string CycleName, string Description, int[] ShiftIds)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can create shift cycles.";
                return RedirectToAction("Index");
            }

            // Validation
            if (string.IsNullOrWhiteSpace(CycleName))
            {
                ViewBag.Error = "Cycle name is required.";
                ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").ToListAsync();
                return View();
            }

            if (ShiftIds == null || ShiftIds.Length == 0)
            {
                ViewBag.Error = "Please select at least one shift for the cycle.";
                ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").ToListAsync();
                return View();
            }

            // Create cycle
            var cycle = new ShiftCycle
            {
                CycleName = CycleName,
                Description = Description
            };

            _context.ShiftCycles.Add(cycle);
            await _context.SaveChangesAsync();

            // Add shifts to cycle
            int order = 1;
            foreach (var shiftId in ShiftIds)
            {
                _context.ShiftCycleAssignments.Add(new ShiftCycleAssignment
                {
                    CycleId = cycle.CycleId,
                    ShiftId = shiftId,
                    OrderNumber = order++
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Shift cycle '{CycleName}' created successfully with {ShiftIds.Length} shifts!";
            return RedirectToAction("ConfigureRotational");
        }

        // ======================================================
        // (c) SYSTEM ADMIN + LINE MANAGER: ASSIGN SHIFT TO EMPLOYEE - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Assign()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied. Only System Administrators and Line Managers can assign shifts.";
                return RedirectToAction("Index");
            }

            await LoadAssignDropdowns(isSystemAdmin, isLineManager);
            return View();
        }

        // ======================================================
        // (c) SYSTEM ADMIN + LINE MANAGER: ASSIGN SHIFT TO EMPLOYEE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int EmployeeId, int ShiftId, DateTime? StartDate, DateTime? EndDate)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied. Only System Administrators and Line Managers can assign shifts.";
                return RedirectToAction("Index");
            }

            // Validation
            if (EmployeeId == 0)
            {
                ViewBag.Error = "Please select an employee.";
                await LoadAssignDropdowns(isSystemAdmin, isLineManager);
                return View();
            }

            if (ShiftId == 0)
            {
                ViewBag.Error = "Please select a shift.";
                await LoadAssignDropdowns(isSystemAdmin, isLineManager);
                return View();
            }

            if (!StartDate.HasValue)
            {
                ViewBag.Error = "Start date is required.";
                await LoadAssignDropdowns(isSystemAdmin, isLineManager);
                return View();
            }

            // Create assignment
            var assignment = new ShiftAssignment
            {
                EmployeeId = EmployeeId,
                ShiftId = ShiftId,
                StartDate = DateOnly.FromDateTime(StartDate.Value),
                EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null,
                Status = "Active"
            };

            _context.ShiftAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Get names for success message
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);
            var shift = await _context.ShiftSchedules.FirstOrDefaultAsync(s => s.ShiftId == ShiftId);

            // Send notification to employee
            await SendNotification(
                EmployeeId,
                $"You have been assigned to shift '{shift?.Name}' starting {StartDate.Value:MMM dd, yyyy}.",
                "Shift",
                "Normal"
            );

            TempData["Success"] = $"Shift '{shift?.Name}' assigned to {employee?.FullName} successfully!";
            return RedirectToAction("Assignments");
        }

        // ======================================================
        // (c) SYSTEM ADMIN + LINE MANAGER: ASSIGN SHIFT TO DEPARTMENT - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> AssignDepartment()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied. Only System Administrators and Line Managers can assign shifts.";
                return RedirectToAction("Index");
            }

            ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").OrderBy(s => s.Name).ToListAsync();

            return View();
        }

        // ======================================================
        // (c) SYSTEM ADMIN + LINE MANAGER: ASSIGN SHIFT TO DEPARTMENT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDepartment(int DepartmentId, int ShiftId, DateTime? StartDate, DateTime? EndDate)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied. Only System Administrators and Line Managers can assign shifts.";
                return RedirectToAction("Index");
            }

            // Validation
            if (DepartmentId == 0)
            {
                ViewBag.Error = "Please select a department.";
                ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
                ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").ToListAsync();
                return View();
            }

            if (ShiftId == 0)
            {
                ViewBag.Error = "Please select a shift.";
                ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
                ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").ToListAsync();
                return View();
            }

            if (!StartDate.HasValue)
            {
                ViewBag.Error = "Start date is required.";
                ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
                ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").ToListAsync();
                return View();
            }

            // Get all employees in department
            var employees = await _context.Employees
                .Where(e => e.DepartmentId == DepartmentId && e.IsActive == true)
                .ToListAsync();

            if (!employees.Any())
            {
                ViewBag.Error = "No active employees found in this department.";
                ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
                ViewBag.Shifts = await _context.ShiftSchedules.Where(s => s.Status == "Active").ToListAsync();
                return View();
            }

            var shift = await _context.ShiftSchedules.FirstOrDefaultAsync(s => s.ShiftId == ShiftId);
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == DepartmentId);

            // Create assignment for each employee
            foreach (var emp in employees)
            {
                var assignment = new ShiftAssignment
                {
                    EmployeeId = emp.EmployeeId,
                    ShiftId = ShiftId,
                    StartDate = DateOnly.FromDateTime(StartDate.Value),
                    EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null,
                    Status = "Active"
                };

                _context.ShiftAssignments.Add(assignment);

                // Send notification to each employee
                await SendNotification(
                    emp.EmployeeId,
                    $"You have been assigned to shift '{shift?.Name}' starting {StartDate.Value:MMM dd, yyyy}.",
                    "Shift",
                    "Normal"
                );
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Shift '{shift?.Name}' assigned to {employees.Count} employees in {department?.DepartmentName} department!";
            return RedirectToAction("Assignments");
        }

        // ======================================================
        // VIEW ALL SHIFT ASSIGNMENTS
        // ======================================================
        public async Task<IActionResult> Assignments()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            List<ShiftAssignment> assignments;

            if (isSystemAdmin || isHRAdmin)
            {
                // Admins see all assignments
                assignments = await _context.ShiftAssignments
                    .Include(sa => sa.Employee)
                    .Include(sa => sa.Shift)
                    .OrderByDescending(sa => sa.StartDate)
                    .ToListAsync();
            }
            else if (isLineManager)
            {
                // Line managers see their team's assignments
                var teamIds = await _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();

                teamIds.Add(employeeId); // Include self

                assignments = await _context.ShiftAssignments
                    .Include(sa => sa.Employee)
                    .Include(sa => sa.Shift)
                    .Where(sa => teamIds.Contains(sa.EmployeeId ?? 0))
                    .OrderByDescending(sa => sa.StartDate)
                    .ToListAsync();
            }
            else
            {
                // Regular employees see only their own assignments
                assignments = await _context.ShiftAssignments
                    .Include(sa => sa.Employee)
                    .Include(sa => sa.Shift)
                    .Where(sa => sa.EmployeeId == employeeId)
                    .OrderByDescending(sa => sa.StartDate)
                    .ToListAsync();
            }

            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CurrentEmployeeId = employeeId;

            return View(assignments);
        }

        // ======================================================
        // (d) SYSTEM ADMIN: EDIT ASSIGNMENT - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> EditAssignment(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can edit shift assignments.";
                return RedirectToAction("Assignments");
            }

            var assignment = await _context.ShiftAssignments
                .Include(sa => sa.Employee)
                .Include(sa => sa.Shift)
                .FirstOrDefaultAsync(sa => sa.AssignmentId == id);

            if (assignment == null)
                return NotFound();

            ViewBag.Shifts = await _context.ShiftSchedules
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(assignment);
        }

        // ======================================================
        // (d) SYSTEM ADMIN: EDIT ASSIGNMENT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(int AssignmentId, int ShiftId, DateTime? StartDate, DateTime? EndDate, string Status)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can edit shift assignments.";
                return RedirectToAction("Assignments");
            }

            var assignment = await _context.ShiftAssignments
                .Include(sa => sa.Employee)
                .FirstOrDefaultAsync(sa => sa.AssignmentId == AssignmentId);

            if (assignment == null)
                return NotFound();

            var oldShiftId = assignment.ShiftId;

            assignment.ShiftId = ShiftId;
            assignment.StartDate = StartDate.HasValue ? DateOnly.FromDateTime(StartDate.Value) : null;
            assignment.EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null;
            assignment.Status = Status;

            await _context.SaveChangesAsync();

            // If shift changed, notify employee
            if (oldShiftId != ShiftId && assignment.EmployeeId.HasValue)
            {
                var shift = await _context.ShiftSchedules.FirstOrDefaultAsync(s => s.ShiftId == ShiftId);
                await SendNotification(
                    assignment.EmployeeId.Value,
                    $"Your shift has been changed to '{shift?.Name}'.",
                    "Shift",
                    "Normal"
                );
            }

            TempData["Success"] = "Shift assignment updated successfully!";
            return RedirectToAction("Assignments");
        }

        // ======================================================
        // DELETE ASSIGNMENT - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Assignments");
            }

            var assignment = await _context.ShiftAssignments
                .Include(sa => sa.Employee)
                .Include(sa => sa.Shift)
                .FirstOrDefaultAsync(sa => sa.AssignmentId == id);

            if (assignment == null)
                return NotFound();

            return View(assignment);
        }

        // ======================================================
        // DELETE ASSIGNMENT - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignmentConfirm(int AssignmentId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Assignments");
            }

            var assignment = await _context.ShiftAssignments
                .Include(sa => sa.Employee)
                .Include(sa => sa.Shift)
                .FirstOrDefaultAsync(sa => sa.AssignmentId == AssignmentId);

            if (assignment == null)
                return NotFound();

            // Send notification
            if (assignment.EmployeeId.HasValue)
            {
                await SendNotification(
                    assignment.EmployeeId.Value,
                    $"Your shift assignment to '{assignment.Shift?.Name}' has been removed.",
                    "Shift",
                    "Normal"
                );
            }

            _context.ShiftAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift assignment deleted successfully!";
            return RedirectToAction("Assignments");
        }

        // ======================================================
        // MY SHIFTS - VIEW OWN SHIFT SCHEDULE
        // ======================================================
        public async Task<IActionResult> MyShifts()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var assignments = await _context.ShiftAssignments
                .Include(sa => sa.Shift)
                .Where(sa => sa.EmployeeId == employeeId && sa.Status == "Active")
                .OrderBy(sa => sa.StartDate)
                .ToListAsync();

            return View(assignments);
        }

        // ======================================================
        // HELPER: LOAD ASSIGN DROPDOWNS
        // ======================================================
        private async Task LoadAssignDropdowns(bool isSystemAdmin, bool isLineManager)
        {
            if (isSystemAdmin)
            {
                // System admin can see all employees
                ViewBag.Employees = await _context.Employees
                    .Where(e => e.IsActive == true)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
            }
            else if (isLineManager)
            {
                // Line manager can only see their team
                int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
                ViewBag.Employees = await _context.Employees
                    .Where(e => e.ManagerId == managerId && e.IsActive == true)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
            }

            ViewBag.Shifts = await _context.ShiftSchedules
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // ======================================================
        // HELPER: SEND NOTIFICATION
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
}