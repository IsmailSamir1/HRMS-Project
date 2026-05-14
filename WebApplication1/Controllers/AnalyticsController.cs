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
    // Helper classes for analytics data
    public class DepartmentStatistic
    {
        public string DepartmentName { get; set; }
        public int NumberOfEmployees { get; set; }
    }

    public class ComplianceReportResult
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string EmploymentStatus { get; set; }
        public string AccountStatus { get; set; }
        public string CountryOfBirth { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? PositionId { get; set; }
        public string PositionTitle { get; set; }
    }

    public class AnalyticsController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public AnalyticsController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // =========================
        // ANALYTICS DASHBOARD (HR ADMIN ONLY)
        // =========================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is HR Admin ONLY
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can access analytics.";
                return RedirectToAction("Index", "Home");
            }

            // Get summary statistics
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            ViewBag.ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive == true);
            ViewBag.TotalDepartments = await _context.Departments.CountAsync();
            ViewBag.TotalPositions = await _context.Positions.CountAsync();

            return View();
        }

        // =========================
        // (a) DEPARTMENT-WISE EMPLOYEE STATISTICS
        // Uses stored procedure: GetDepartmentEmployeeStats
        // =========================
        public async Task<IActionResult> DepartmentStats()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is HR Admin ONLY
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can access analytics.";
                return RedirectToAction("Index", "Home");
            }

            var departmentStats = new List<DepartmentStatistic>();

            // Call stored procedure: GetDepartmentEmployeeStats
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC GetDepartmentEmployeeStats";
                command.CommandType = CommandType.Text;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        departmentStats.Add(new DepartmentStatistic
                        {
                            DepartmentName = reader["department_name"]?.ToString() ?? "Unknown",
                            NumberOfEmployees = reader["NumberOfEmployees"] != DBNull.Value
                                ? Convert.ToInt32(reader["NumberOfEmployees"])
                                : 0
                        });
                    }
                }
            }

            await connection.CloseAsync();

            // Get additional stats for the view
            ViewBag.TotalEmployees = departmentStats.Sum(d => d.NumberOfEmployees);
            ViewBag.TotalDepartments = departmentStats.Count;

            return View(departmentStats);
        }

        // =========================
        // (b) COMPLIANCE / DIVERSITY REPORTS
        // Uses stored procedure: GenerateProfileReport
        // =========================
        [HttpGet]
        public async Task<IActionResult> ComplianceReport()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is HR Admin ONLY
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can access analytics.";
                return RedirectToAction("Index", "Home");
            }

            // Load dropdown data for filters
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Positions = await _context.Positions.ToListAsync();
            ViewBag.Countries = await _context.Employees
                .Where(e => e.CountryOfBirth != null)
                .Select(e => e.CountryOfBirth)
                .Distinct()
                .ToListAsync();

            return View(new List<ComplianceReportResult>());
        }

        [HttpPost]
        public async Task<IActionResult> ComplianceReport(string FilterField, string FilterValue, string ReportType)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is HR Admin ONLY
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can access analytics.";
                return RedirectToAction("Index", "Home");
            }

            // Load dropdown data for filters
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Positions = await _context.Positions.ToListAsync();
            ViewBag.Countries = await _context.Employees
                .Where(e => e.CountryOfBirth != null)
                .Select(e => e.CountryOfBirth)
                .Distinct()
                .ToListAsync();

            var results = new List<ComplianceReportResult>();

            if (string.IsNullOrEmpty(FilterField) || string.IsNullOrEmpty(FilterValue))
            {
                ViewBag.Error = "Please select a filter field and value.";
                return View(results);
            }

            // Call stored procedure: GenerateProfileReport
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "EXEC GenerateProfileReport @FilterField, @FilterValue";
                command.CommandType = CommandType.Text;

                var paramFilterField = command.CreateParameter();
                paramFilterField.ParameterName = "@FilterField";
                paramFilterField.Value = FilterField;
                command.Parameters.Add(paramFilterField);

                var paramFilterValue = command.CreateParameter();
                paramFilterValue.ParameterName = "@FilterValue";
                paramFilterValue.Value = FilterValue;
                command.Parameters.Add(paramFilterValue);

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var result = new ComplianceReportResult
                            {
                                EmployeeId = reader["employee_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["employee_id"])
                                    : 0,
                                FullName = reader["full_name"]?.ToString() ?? "",
                                Email = reader["email"]?.ToString() ?? "",
                                Phone = reader["phone"]?.ToString() ?? "",
                                EmploymentStatus = reader["employment_status"]?.ToString() ?? "",
                                AccountStatus = reader["account_status"]?.ToString() ?? "",
                                CountryOfBirth = reader["country_of_birth"]?.ToString() ?? "",
                                DepartmentId = reader["department_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["department_id"])
                                    : null,
                                PositionId = reader["position_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["position_id"])
                                    : null
                            };

                            results.Add(result);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ViewBag.Error = "Error generating report: " + ex.Message;
                }
            }

            await connection.CloseAsync();

            // Get department and position names
            var departments = await _context.Departments.ToListAsync();
            var positions = await _context.Positions.ToListAsync();

            foreach (var result in results)
            {
                if (result.DepartmentId.HasValue)
                {
                    result.DepartmentName = departments
                        .FirstOrDefault(d => d.DepartmentId == result.DepartmentId)?.DepartmentName ?? "";
                }
                if (result.PositionId.HasValue)
                {
                    result.PositionTitle = positions
                        .FirstOrDefault(p => p.PositionId == result.PositionId)?.PositionTitle ?? "";
                }
            }

            ViewBag.FilterField = FilterField;
            ViewBag.FilterValue = FilterValue;
            ViewBag.ReportType = ReportType;
            ViewBag.ResultCount = results.Count;

            return View(results);
        }

        // =========================
        // DIVERSITY REPORT (Shortcut)
        // =========================
        [HttpGet]
        public async Task<IActionResult> DiversityReport()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            // Check if user is HR Admin ONLY
            if (HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Administrators can access analytics.";
                return RedirectToAction("Index", "Home");
            }

            // Get diversity statistics by country
            var diversityByCountry = await _context.Employees
                .Where(e => e.CountryOfBirth != null)
                .GroupBy(e => e.CountryOfBirth)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .ToListAsync();

            // Get diversity statistics by department
            var diversityByDepartment = await _context.Employees
                .Where(e => e.DepartmentId != null)
                .GroupBy(e => e.DepartmentId)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Get departments for names
            var departments = await _context.Departments.ToListAsync();

            ViewBag.DiversityByCountry = diversityByCountry;
            ViewBag.DiversityByDepartment = diversityByDepartment;
            ViewBag.Departments = departments;
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();

            return View();
        }
    }
}