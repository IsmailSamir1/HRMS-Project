
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ContractsController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public ContractsController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(string filter = "Active") // Default to Active/Expiring
        {
            // Security Check
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Admins can view contracts.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Contracts
                .Include(c => c.Employees)
                .AsQueryable();

            // Apply Filters and Sorting
            switch (filter)
            {
                case "All":
                    query = query.OrderByDescending(c => c.StartDate);
                    break;
                case "Expiring":
                    // Contracts expiring in the next 30 days
                    var today = DateOnly.FromDateTime(DateTime.Now);
                    var next30Days = today.AddDays(30);
                    query = query
                        .Where(c => c.EndDate.HasValue && c.EndDate > today && c.EndDate <= next30Days && c.CurrentState == "Active")
                        .OrderBy(c => c.EndDate);
                    break;
                case "Active":
                default:
                    // Active contracts first, then by Start Date
                    query = query
                        .Where(c => c.CurrentState == "Active")
                        .OrderByDescending(c => c.StartDate);
                    break;
            }

            ViewBag.CurrentFilter = filter;
            var contracts = await query.ToListAsync();

            return View(contracts);
        }

        // GET: Contracts/Create
        public IActionResult Create()
        {
            // Security Check
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Access denied. Only HR Admins can create contracts.";
                return RedirectToAction("Index", "Home");
            }

            // Populate Employee Dropdown (only employees without current contracts or allowing updates)
            // For simplicity, we list all employees, but in a real app might filter
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName");

            return View();
        }



        // GET: Contracts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Security Check
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                return RedirectToAction("Index", "Home");
            }

            var contract = await _context.Contracts
                .Include(c => c.Employees)
                .FirstOrDefaultAsync(m => m.ContractId == id);

            if (contract == null) return NotFound();

            return View(contract);
        }

        // POST: Contracts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractId,Type,StartDate,EndDate,CurrentState")] Contract contract)
        {
            if (id != contract.ContractId) return NotFound();

            // Security Check
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Auto-update status if renewed
                    if (contract.EndDate.HasValue && contract.EndDate.Value > DateOnly.FromDateTime(DateTime.Now))
                    {
                        if (contract.CurrentState == "Expired")
                        {
                            contract.CurrentState = "Active";
                        }
                    }

                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Contract updated successfully.";

                    // Notify Employee (find who owns this contract)
                    var emp = await _context.Employees.FirstOrDefaultAsync(e => e.ContractId == contract.ContractId);
                    if (emp != null)
                    {
                        await NotifyEmployee(emp.EmployeeId, "Your employment contract details have been updated.", "Medium");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Contracts.Any(e => e.ContractId == contract.ContractId))
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
            return View(contract);
        }

        // GET: Contracts/Renew/5
        public async Task<IActionResult> Renew(int? id)
        {
            if (id == null) return NotFound();

            // Security Check
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                return RedirectToAction("Index", "Home");
            }

            var oldContract = await _context.Contracts
                .Include(c => c.Employees)
                .FirstOrDefaultAsync(m => m.ContractId == id);

            if (oldContract == null) return NotFound();

            // Prepare new contract model with defaults from old one
            var newContract = new Contract
            {
                Type = oldContract.Type,
                CurrentState = "Active",
                // Set default dates (e.g., start tomorrow)
                StartDate = oldContract.EndDate.HasValue ? oldContract.EndDate.Value.AddDays(1) : DateOnly.FromDateTime(DateTime.Now),
            };

            // Pass old contract ID via ViewBag or TempData to link employee later
            ViewBag.OldContractId = oldContract.ContractId;
            ViewBag.EmployeeName = oldContract.Employees.FirstOrDefault()?.FullName ?? "Unknown";
            ViewBag.EmployeeId = oldContract.Employees.FirstOrDefault()?.EmployeeId;

            return View(newContract);
        }

        // POST: Contracts/Renew
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew([Bind("Type,StartDate,EndDate")] Contract contract, int OldContractId, int? EmployeeId)
        {
            // Security Check
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // 1. Create New Contract
                contract.CurrentState = "Active";
                _context.Add(contract);
                await _context.SaveChangesAsync();

                // 2. Update Employee to point to NEW Contract
                if (EmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FindAsync(EmployeeId);
                    if (employee != null)
                    {
                        employee.ContractId = contract.ContractId;
                        _context.Update(employee);
                        await _context.SaveChangesAsync();
                    }
                }

                // 3. Mark Old Contract as 'Renewed' (Optional, but good for history)
                var oldContract = await _context.Contracts.FindAsync(OldContractId);
                if (oldContract != null)
                {
                    oldContract.CurrentState = "Renewed"; // Or 'Expired'
                    _context.Update(oldContract);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Contract renewed successfully. Employee profile updated.";

                // Notify Employee
                if (EmployeeId.HasValue)
                {
                    await NotifyEmployee(EmployeeId.Value, "Your employment contract has been renewed.", "High");
                }

                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Type,StartDate,EndDate")] Contract contract, int? EmployeeId)
        {
            // Security Check
            if (HttpContext.Session.GetString("IsHRAdmin") != "True" &&
                HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // 1. Set default status
                contract.CurrentState = "Active";

                // 2. Add Contract to DB
                _context.Add(contract);
                await _context.SaveChangesAsync(); // Save to get the generated ContractId

                // 3. Assign Contract to Employee if selected
                if (EmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FindAsync(EmployeeId);
                    if (employee != null)
                    {
                        employee.ContractId = contract.ContractId;
                        _context.Update(employee);
                        await _context.SaveChangesAsync();

                        // Notify Employee
                        await NotifyEmployee(EmployeeId.Value, "A new employment contract has been created for you.", "High");
                    }
                }

                TempData["Success"] = "Contract created and assigned successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName", EmployeeId);
            return View(contract);
        }

        // Helper Method for Notifications
        private async Task NotifyEmployee(int employeeId, string message, string urgency)
        {
            try
            {
                // 1. Create Notification
                var notification = new Notification
                {
                    MessageContent = message,
                    Timestamp = DateTime.Now,
                    Urgency = urgency,
                    ReadStatus = false,
                    NotificationType = "Contract"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 2. Link to Employee
                var empNotification = new EmployeeNotification
                {
                    EmployeeId = employeeId,
                    NotificationId = notification.NotificationId,
                    DeliveryStatus = "Delivered",
                    DeliveredAt = DateTime.Now
                };
                _context.EmployeeNotifications.Add(empNotification);
                await _context.SaveChangesAsync();
            }
            catch (System.Exception ex)
            {
                // Log error but don't stop the main flow
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }
}
