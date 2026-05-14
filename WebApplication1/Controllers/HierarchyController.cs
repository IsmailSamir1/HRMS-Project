using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using WebApplication1.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    // Helper class for hierarchy data
    public class HierarchyNode
    {
        public string EmployeeName { get; set; }
        public string ManagerName { get; set; }
        public string DepartmentName { get; set; }
        public string PositionTitle { get; set; }
        public int? HierarchyLevel { get; set; }
        public int EmployeeId { get; set; }
        public int? ManagerId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class HierarchyController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public HierarchyController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // =========================
        // (a) VIEW ORGANIZATIONAL HIERARCHY
        // Uses stored procedure: ViewOrgHierarchy
        // System Admin ONLY
        // =========================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is System Admin ONLY
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can view organizational hierarchy.";
                return RedirectToAction("Index", "Home");
            }

            var hierarchyNodes = new List<HierarchyNode>();

            // Call stored procedure: ViewOrgHierarchy
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC ViewOrgHierarchy";
                command.CommandType = CommandType.Text;

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            hierarchyNodes.Add(new HierarchyNode
                            {
                                EmployeeName = reader["EmployeeName"]?.ToString() ?? "Unknown",
                                ManagerName = reader["ManagerName"]?.ToString() ?? "No Manager",
                                DepartmentName = reader["department_name"]?.ToString() ?? "No Department",
                                PositionTitle = reader["position_title"]?.ToString() ?? "No Position",
                                HierarchyLevel = reader["hierarchy_level"] != DBNull.Value
                                    ? Convert.ToInt32(reader["hierarchy_level"])
                                    : null
                            });
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ViewBag.Error = "Error loading hierarchy: " + ex.Message;
                }
            }

            await connection.CloseAsync();

            // Get additional data for navigation
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.AllEmployees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.IsActive == true)
                .ToListAsync();

            // Group by department for visual navigation
            var employeesByDepartment = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.IsActive == true && e.DepartmentId != null)
                .GroupBy(e => e.Department.DepartmentName)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.EmployeesByDepartment = employeesByDepartment;

            return View(hierarchyNodes);
        }

        // =========================
        // (b) REASSIGN EMPLOYEE - GET
        // System Admin ONLY
        // =========================
        [HttpGet]
        public async Task<IActionResult> Reassign(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is System Admin ONLY
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can reassign employees.";
                return RedirectToAction("Index", "Home");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            // Get all departments
            ViewBag.Departments = await _context.Departments.ToListAsync();

            // Get all potential managers (active employees except the employee being reassigned)
            ViewBag.Managers = await _context.Employees
                .Where(e => e.IsActive == true && e.EmployeeId != id)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            return View(employee);
        }

        // =========================
        // (b) REASSIGN EMPLOYEE - POST
        // Uses stored procedure: ReassignHierarchy
        // System Admin ONLY
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int EmployeeId, int? NewDepartmentId, int? NewManagerId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is System Admin ONLY
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only System Administrators can reassign employees.";
                return RedirectToAction("Index", "Home");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);
            if (employee == null)
                return NotFound();

            // Call stored procedure: ReassignHierarchy
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC ReassignHierarchy @EmployeeID, @NewDepartmentID, @NewManagerID";
                command.CommandType = CommandType.Text;

                var paramEmployeeId = command.CreateParameter();
                paramEmployeeId.ParameterName = "@EmployeeID";
                paramEmployeeId.Value = EmployeeId;
                command.Parameters.Add(paramEmployeeId);

                var paramDeptId = command.CreateParameter();
                paramDeptId.ParameterName = "@NewDepartmentID";
                paramDeptId.Value = NewDepartmentId.HasValue ? (object)NewDepartmentId.Value : DBNull.Value;
                command.Parameters.Add(paramDeptId);

                var paramManagerId = command.CreateParameter();
                paramManagerId.ParameterName = "@NewManagerID";
                paramManagerId.Value = NewManagerId.HasValue ? (object)NewManagerId.Value : DBNull.Value;
                command.Parameters.Add(paramManagerId);

                try
                {
                    await command.ExecuteNonQueryAsync();
                    TempData["Success"] = $"{employee.FullName} has been reassigned successfully!";
                }
                catch (System.Exception ex)
                {
                    TempData["Error"] = "Error reassigning employee: " + ex.Message;
                }
            }

            await connection.CloseAsync();

            return RedirectToAction("Index");
        }

        // =========================
        // (c) VIEW DEPARTMENT DETAILS
        // ALL USERS can access
        // =========================
        public async Task<IActionResult> Department(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
                return NotFound();

            // Get all employees in this department
            var employees = await _context.Employees
                .Include(e => e.Position)
                .Include(e => e.Manager)
                .Where(e => e.DepartmentId == id && e.IsActive == true)
                .ToListAsync();

            ViewBag.Department = department;
            ViewBag.EmployeeCount = employees.Count;
            ViewBag.IsSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";

            return View(employees);
        }

        // =========================
        // (c) VIEW MANAGER'S TEAM
        // ALL USERS can access
        // =========================
        public async Task<IActionResult> Team(int managerId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var manager = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == managerId);

            if (manager == null)
                return NotFound();

            // Get all team members reporting to this manager
            var teamMembers = await _context.Employees
                .Include(e => e.Position)
                .Include(e => e.Department)
                .Where(e => e.ManagerId == managerId && e.IsActive == true)
                .ToListAsync();

            ViewBag.Manager = manager;
            ViewBag.TeamSize = teamMembers.Count;
            ViewBag.IsSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";

            return View(teamMembers);
        }

        // =========================
        // (c) VIEW ALL DEPARTMENTS
        // ALL USERS can access
        // =========================
        public async Task<IActionResult> Departments()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var departments = await _context.Departments.ToListAsync();

            // Get employee count per department
            var employeesByDepartment = await _context.Employees
                .Where(e => e.IsActive == true && e.DepartmentId != null)
                .GroupBy(e => e.DepartmentId)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.EmployeesByDepartment = employeesByDepartment;

            return View(departments);
        }

        // =========================
        // (c) VIEW ALL MANAGERS
        // ALL USERS can access
        // =========================
        public async Task<IActionResult> Managers()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            var allEmployees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.IsActive == true)
                .ToListAsync();

            // Get only managers (employees who have team members)
            var managers = allEmployees
                .Where(e => allEmployees.Any(emp => emp.ManagerId == e.EmployeeId))
                .ToList();

            // Count team size for each manager
            var managerTeamSizes = managers.Select(m => new
            {
                Manager = m,
                TeamSize = allEmployees.Count(e => e.ManagerId == m.EmployeeId)
            }).ToList();

            ViewBag.ManagerTeamSizes = managerTeamSizes;

            return View(managers);
        }
    }
}