using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System; // Required for Math.Round
using System.Threading.Tasks; // Required for async/await Task

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public HomeController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // =========================
        // DASHBOARD (ALL LOGGED USERS)
        // (Merged logic from Source A and B, Source A is more comprehensive)
        // =========================
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.EmployeeName = HttpContext.Session.GetString("EmployeeName");
            ViewBag.IsSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            ViewBag.IsHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            ViewBag.IsLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Calculate statistics for dashboard (from Source A)
            var totalEmployees = _context.Employees.Count();
            var activeEmployees = _context.Employees.Count(e => e.IsActive == true || e.IsActive == null);
            var totalDepartments = _context.Departments.Count();
            var avgProfileCompletion = _context.Employees
                .Select(e => (double?)e.ProfileCompletion)
                .Average() ?? 0;

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.AvgProfileCompletion = Math.Round(avgProfileCompletion, 1);

            // Department distribution for chart (from Source A)
            var departmentData = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.DepartmentId.HasValue)
                .GroupBy(e => e.Department.DepartmentName)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.DepartmentLabels = departmentData.Select(d => d.Department).ToList();
            ViewBag.DepartmentCounts = departmentData.Select(d => d.Count).ToList();

            // Role distribution for chart (from Source A)
            var roleData = _context.EmployeeRoles
                .Include(er => er.Role)
                .GroupBy(er => er.Role.RoleName)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.RoleLabels = roleData.Select(r => r.Role.Replace("_", " ")).ToList();
            ViewBag.RoleCounts = roleData.Select(r => r.Count).ToList();

            return View();
        }

        // =========================
        // HR ADMIN: VIEW ALL EMPLOYEES LIST
        // (Requirement d - HR Admin can edit any employee)
        // (Merged logic from Source A and B, Source A is more comprehensive with Includes)
        // =========================
        public IActionResult AllEmployees()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can view all employees.";
                return RedirectToAction("Index");
            }

            // Include department and roles for better display (from Source A)
            var employees = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .ToList();
            return View(employees);
        }

        // =========================
        // LINE MANAGER: VIEW MY TEAM (from Source A)
        // (Managers can view their team details)
        // =========================
        public IActionResult MyTeam()
        {
            var managerId = HttpContext.Session.GetInt32("EmployeeId");
            if (managerId == null)
                return RedirectToAction("Login", "Account");

            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHrAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            if (!isLineManager && !isSystemAdmin && !isHrAdmin)
            {
                TempData["Error"] = "Only managers and admins can view a team.";
                return RedirectToAction("Index");
            }

            // Include related data for detailed team view
            var team = _context.Employees
                .Where(e => e.ManagerId == managerId)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .ToList();

            return View(team);
        }

        // =========================
        // HR ADMIN: MANAGE PROFILE COMPLETENESS (from Source A)
        // (Requirement e - HR Admin can manage the completeness of Employees profile)
        // =========================
        public async Task<IActionResult> ManageProfileCompleteness()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can manage profile completeness.";
                return RedirectToAction("Index");
            }

            var employees = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Contract)
                .ToList();

            // Auto-calculate completion for all employees
            foreach (var emp in employees)
            {
                int totalFields = 15;
                int completedFields = 0;

                if (!string.IsNullOrWhiteSpace(emp.FirstName)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.LastName)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.Email)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.Phone)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.Address)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.NationalId)) completedFields++;
                if (emp.DateOfBirth.HasValue) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.CountryOfBirth)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.EmergencyContactName)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.EmergencyContactPhone)) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.Relationship)) completedFields++;
                if (emp.DepartmentId.HasValue) completedFields++;
                if (emp.PositionId.HasValue) completedFields++;
                if (emp.ContractId.HasValue) completedFields++;
                if (!string.IsNullOrWhiteSpace(emp.Biography)) completedFields++;

                int calculatedPercentage = (int)Math.Round((double)completedFields / totalFields * 100);
                emp.ProfileCompletion = calculatedPercentage;
            }

            await _context.SaveChangesAsync();

            var sortedEmployees = employees.OrderByDescending(e => e.ProfileCompletion ?? 0).ToList();
            return View(sortedEmployees);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileCompleteness(int EmployeeId, int CompletionPercentage)
        {
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can manage profile completeness.";
                return RedirectToAction("Index");
            }

            if (CompletionPercentage < 0 || CompletionPercentage > 100)
            {
                TempData["Error"] = "Completion percentage must be between 0 and 100.";
                return RedirectToAction("ManageProfileCompleteness");
            }

            var employee = await _context.Employees.FindAsync(EmployeeId);
            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("ManageProfileCompleteness");
            }

            employee.ProfileCompletion = CompletionPercentage;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Profile completion updated to {CompletionPercentage}% for {employee.FullName}.";
            return RedirectToAction("ManageProfileCompleteness");
        }

        [HttpPost]
        public async Task<IActionResult> CalculateProfileCompleteness(int EmployeeId)
        {
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can manage profile completeness.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Contract)
                .FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("ManageProfileCompleteness");
            }

            // Calculate completion based on required fields
            int totalFields = 15; // Total number of important fields
            int completedFields = 0;

            if (!string.IsNullOrWhiteSpace(employee.FirstName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.LastName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.Email)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.Phone)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.Address)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.NationalId)) completedFields++;
            if (employee.DateOfBirth.HasValue) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.CountryOfBirth)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.EmergencyContactName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.EmergencyContactPhone)) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.Relationship)) completedFields++;
            if (employee.DepartmentId.HasValue) completedFields++;
            if (employee.PositionId.HasValue) completedFields++;
            if (employee.ContractId.HasValue) completedFields++;
            if (!string.IsNullOrWhiteSpace(employee.Biography)) completedFields++;

            int calculatedPercentage = (int)Math.Round((double)completedFields / totalFields * 100);
            employee.ProfileCompletion = calculatedPercentage;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Profile completion calculated and updated to {calculatedPercentage}% for {employee.FullName} ({completedFields}/{totalFields} fields completed).";
            return RedirectToAction("ManageProfileCompleteness");
        }

        // =========================
        // PRIVACY PAGE (Identical in both sources)
        // =========================
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================
        // ERROR PAGE (Identical in both sources)
        // =========================
        public IActionResult Error()
        {
            return View();
        }
    }
}