using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class OrgChartController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public OrgChartController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // ======================================================
        // ORGANIZATION CHART - MAIN VIEW
        // ======================================================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            ViewBag.Departments = departments;

            ViewBag.TotalEmployees = await _context.Employees.CountAsync(e => e.IsActive == true);
            ViewBag.TotalDepartments = departments.Count;
            ViewBag.TotalManagers = await _context.Employees
                .Where(e => e.IsActive == true)
                .Where(e => _context.Employees.Any(emp => emp.ManagerId == e.EmployeeId))
                .CountAsync();

            return View();
        }

        // ======================================================
        // API: GET ORG CHART DATA (JSON)
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetChartData(int? departmentId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Unauthorized();

            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.IsActive == true);

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            var employees = await query.ToListAsync();

            // Build hierarchical structure
            var nodes = new List<object>();

            // Find root nodes (employees without managers or whose managers are not in the filtered list)
            var employeeIds = employees.Select(e => e.EmployeeId).ToHashSet();
            var rootEmployees = employees.Where(e => !e.ManagerId.HasValue || !employeeIds.Contains(e.ManagerId.Value)).ToList();

            foreach (var root in rootEmployees)
            {
                nodes.Add(BuildNode(root, employees));
            }

            return Json(nodes);
        }

        private object BuildNode(Employee employee, List<Employee> allEmployees)
        {
            var children = allEmployees.Where(e => e.ManagerId == employee.EmployeeId).ToList();
            var childNodes = children.Select(c => BuildNode(c, allEmployees)).ToList();

            // Determine node color based on role
            string color = "#667eea"; // Default
            if (_context.EmployeeRoles.Any(er => er.EmployeeId == employee.EmployeeId && er.RoleId == 1))
                color = "#e74c3c"; // System Admin - Red
            else if (_context.EmployeeRoles.Any(er => er.EmployeeId == employee.EmployeeId && er.RoleId == 2))
                color = "#f39c12"; // HR Admin - Orange
            else if (children.Any())
                color = "#3498db"; // Manager - Blue

            return new
            {
                id = employee.EmployeeId,
                name = employee.FullName ?? "Unknown",
                title = employee.Position?.PositionTitle ?? "Employee",
                department = employee.Department?.DepartmentName ?? "N/A",
                image = employee.ProfileImage ?? "/images/default-avatar.png",
                color = color,
                teamSize = children.Count,
                children = childNodes
            };
        }

        // ======================================================
        // API: GET EMPLOYEE CARD DATA
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetEmployeeCard(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Unauthorized();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Manager)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            var teamSize = await _context.Employees.CountAsync(e => e.ManagerId == id && e.IsActive == true);

            var roles = employee.EmployeeRoles.Select(er => er.Role?.RoleName).Where(r => r != null).ToList();

            return Json(new
            {
                id = employee.EmployeeId,
                name = employee.FullName,
                email = employee.Email,
                phone = employee.Phone,
                department = employee.Department?.DepartmentName,
                position = employee.Position?.PositionTitle,
                manager = employee.Manager?.FullName,
                hireDate = employee.HireDate?.ToString("MMM dd, yyyy"),
                teamSize = teamSize,
                profileImage = employee.ProfileImage,
                roles = roles,
                isActive = employee.IsActive
            });
        }

        // ======================================================
        // API: GET FLAT LIST FOR SEARCH
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.IsActive == true &&
                           (e.FullName.Contains(query) ||
                            e.Email.Contains(query) ||
                            e.Department.DepartmentName.Contains(query)))
                .Take(10)
                .Select(e => new
                {
                    id = e.EmployeeId,
                    name = e.FullName,
                    department = e.Department.DepartmentName,
                    position = e.Position.PositionTitle
                })
                .ToListAsync();

            return Json(employees);
        }
    }
}