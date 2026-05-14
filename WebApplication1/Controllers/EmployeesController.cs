using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public EmployeesController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var hrmsDatabasesContext = _context.Employees.Include(e => e.Contract).Include(e => e.Department).Include(e => e.Manager).Include(e => e.PayGradeNavigation).Include(e => e.Position).Include(e => e.SalaryType).Include(e => e.TaxForm);
            return View(await hrmsDatabasesContext.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // Access control: only admins/managers or self (FROM DOCUMENT 4)
            var sessionId = HttpContext.Session.GetInt32("EmployeeId");
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHrAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (sessionId == null)
                return RedirectToAction("Login", "Account");

            // Allow admins/managers to see anyone, employees only themselves
            if (!isSystemAdmin && !isHrAdmin && !isLineManager && sessionId != id)
            {
                TempData["Error"] = "You are not allowed to view this profile.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Contract)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.PayGradeNavigation)
                .Include(e => e.Position)
                .Include(e => e.SalaryType)
                .Include(e => e.TaxForm)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            ViewData["ContractId"] = new SelectList(_context.Contracts, "ContractId", "ContractId");
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentId");
            ViewData["ManagerId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId");
            ViewData["PayGrade"] = new SelectList(_context.PayGrades, "PayGradeId", "PayGradeId");
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionId");
            ViewData["SalaryTypeId"] = new SelectList(_context.SalaryTypes, "SalaryTypeId", "SalaryTypeId");
            ViewData["TaxFormId"] = new SelectList(_context.TaxForms, "TaxFormId", "TaxFormId");
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,FirstName,LastName,FullName,NationalId,DateOfBirth,CountryOfBirth,Phone,Email,Address,EmergencyContactName,EmergencyContactPhone,Relationship,Biography,ProfileImage,EmploymentProgress,AccountStatus,EmploymentStatus,HireDate,IsActive,ProfileCompletion,DepartmentId,PositionId,ManagerId,ContractId,TaxFormId,SalaryTypeId,PayGrade")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractId"] = new SelectList(_context.Contracts, "ContractId", "ContractId", employee.ContractId);
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentId", employee.DepartmentId);
            ViewData["ManagerId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", employee.ManagerId);
            ViewData["PayGrade"] = new SelectList(_context.PayGrades, "PayGradeId", "PayGradeId", employee.PayGrade);
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionId", employee.PositionId);
            ViewData["SalaryTypeId"] = new SelectList(_context.SalaryTypes, "SalaryTypeId", "SalaryTypeId", employee.SalaryTypeId);
            ViewData["TaxFormId"] = new SelectList(_context.TaxForms, "TaxFormId", "TaxFormId", employee.TaxFormId);
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewData["ContractId"] = new SelectList(_context.Contracts, "ContractId", "ContractId", employee.ContractId);
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentId", employee.DepartmentId);
            ViewData["ManagerId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", employee.ManagerId);
            ViewData["PayGrade"] = new SelectList(_context.PayGrades, "PayGradeId", "PayGradeId", employee.PayGrade);
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionId", employee.PositionId);
            ViewData["SalaryTypeId"] = new SelectList(_context.SalaryTypes, "SalaryTypeId", "SalaryTypeId", employee.SalaryTypeId);
            ViewData["TaxFormId"] = new SelectList(_context.TaxForms, "TaxFormId", "TaxFormId", employee.TaxFormId);
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,FirstName,LastName,FullName,NationalId,DateOfBirth,CountryOfBirth,Phone,Email,Address,EmergencyContactName,EmergencyContactPhone,Relationship,Biography,ProfileImage,EmploymentProgress,AccountStatus,EmploymentStatus,HireDate,IsActive,ProfileCompletion,DepartmentId,PositionId,ManagerId,ContractId,TaxFormId,SalaryTypeId,PayGrade")] Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractId"] = new SelectList(_context.Contracts, "ContractId", "ContractId", employee.ContractId);
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentId", employee.DepartmentId);
            ViewData["ManagerId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeId", employee.ManagerId);
            ViewData["PayGrade"] = new SelectList(_context.PayGrades, "PayGradeId", "PayGradeId", employee.PayGrade);
            ViewData["PositionId"] = new SelectList(_context.Positions, "PositionId", "PositionId", employee.PositionId);
            ViewData["SalaryTypeId"] = new SelectList(_context.SalaryTypes, "SalaryTypeId", "SalaryTypeId", employee.SalaryTypeId);
            ViewData["TaxFormId"] = new SelectList(_context.TaxForms, "TaxFormId", "TaxFormId", employee.TaxFormId);
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Contract)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.PayGradeNavigation)
                .Include(e => e.Position)
                .Include(e => e.SalaryType)
                .Include(e => e.TaxForm)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}