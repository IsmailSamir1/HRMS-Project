using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly HrmsDatabasesContext _dbContext;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AccountController(HrmsDatabasesContext context)
        {
            _dbContext = context;
        }

        // ======================================================
        // LOGIN — WITH LOCKOUT & REMEMBER ME
        // ======================================================
        [HttpGet]
        public IActionResult Login()
        {
            // Check for Remember Me cookie
            if (Request.Cookies.ContainsKey("RememberMe_EmployeeId"))
            {
                var cookieValue = Request.Cookies["RememberMe_EmployeeId"];
                if (int.TryParse(cookieValue, out int employeeId))
                {
                    return AutoLogin(employeeId).Result;
                }
            }

            if (HttpContext.Session.GetInt32("EmployeeId") != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "Please enter email and password.";
                return View();
            }

            Email = Email.Trim();
            Password = Password.Trim();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            string userAgent = Request.Headers["User-Agent"].ToString();

            // Find employee by email
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.Email != null && e.Email.ToLower() == Email.ToLower());

            // Check if account exists
            if (employee == null)
            {
                await LogLoginActivity(null, Email, "Failed", "Account not found", ipAddress, userAgent);
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // Check if account is locked
            if (employee.AccountStatus == "Locked")
            {
                await LogLoginActivity(employee.EmployeeId, Email, "Locked", "Account is locked", ipAddress, userAgent);
                ViewBag.Error = "Your account is locked due to multiple failed login attempts. Please use 'Forgot Password' to reset or wait 15 minutes.";
                ViewBag.ShowForgotPassword = true;
                return View();
            }

            // Check if account is inactive
            if (employee.IsActive == false)
            {
                await LogLoginActivity(employee.EmployeeId, Email, "Failed", "Account inactive", ipAddress, userAgent);
                ViewBag.Error = "Your account is inactive. Please contact HR.";
                return View();
            }

            // Validate password (NationalId)
            if (employee.NationalId != Password)
            {
                // Increment failed attempts using Biography field temporarily (or create a pattern)
                int failedAttempts = GetFailedAttempts(employee) + 1;
                SetFailedAttempts(employee, failedAttempts);

                if (failedAttempts >= MaxFailedAttempts)
                {
                    employee.AccountStatus = "Locked";
                    await LogLoginActivity(employee.EmployeeId, Email, "Locked", $"Account locked after {MaxFailedAttempts} failed attempts", ipAddress, userAgent);
                    await _dbContext.SaveChangesAsync();

                    ViewBag.Error = $"Your account has been locked after {MaxFailedAttempts} failed attempts. Please use 'Forgot Password' to reset.";
                    ViewBag.ShowForgotPassword = true;
                    return View();
                }

                await _dbContext.SaveChangesAsync();
                await LogLoginActivity(employee.EmployeeId, Email, "Failed", "Invalid password", ipAddress, userAgent);

                int remainingAttempts = MaxFailedAttempts - failedAttempts;
                ViewBag.Error = $"Invalid email or password. {remainingAttempts} attempt(s) remaining.";
                return View();
            }

            // Successful login - reset failed attempts
            SetFailedAttempts(employee, 0);
            if (employee.AccountStatus == "Locked")
            {
                employee.AccountStatus = "Active";
            }
            await _dbContext.SaveChangesAsync();

            // Set session
            HttpContext.Session.SetInt32("EmployeeId", employee.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", employee.FullName ?? "");
            HttpContext.Session.SetString("Email", employee.Email ?? "");

            HttpContext.Session.SetString(
                "IsSystemAdmin",
                await _dbContext.SystemAdministrators.AnyAsync(s => s.EmployeeId == employee.EmployeeId) ? "True" : "False");

            HttpContext.Session.SetString(
                "IsHRAdmin",
                await _dbContext.Hradministrators.AnyAsync(h => h.EmployeeId == employee.EmployeeId) ? "True" : "False");

            HttpContext.Session.SetString(
                "IsLineManager",
                await _dbContext.LineManagers.AnyAsync(l => l.EmployeeId == employee.EmployeeId) ? "True" : "False");

            // Remember Me - set cookie for 30 days
            if (RememberMe)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append("RememberMe_EmployeeId", employee.EmployeeId.ToString(), cookieOptions);
                Response.Cookies.Append("RememberMe_Token", GenerateRememberMeToken(employee), cookieOptions);
            }

            // Log successful login
            await LogLoginActivity(employee.EmployeeId, Email, "Success", null, ipAddress, userAgent);

            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // AUTO LOGIN FROM REMEMBER ME COOKIE
        // ======================================================
        private async Task<IActionResult> AutoLogin(int employeeId)
        {
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.IsActive == true && e.AccountStatus != "Locked");

            if (employee == null)
            {
                // Clear invalid cookies
                Response.Cookies.Delete("RememberMe_EmployeeId");
                Response.Cookies.Delete("RememberMe_Token");
                return View();
            }

            // Validate token
            var storedToken = Request.Cookies["RememberMe_Token"];
            var expectedToken = GenerateRememberMeToken(employee);

            if (storedToken != expectedToken)
            {
                Response.Cookies.Delete("RememberMe_EmployeeId");
                Response.Cookies.Delete("RememberMe_Token");
                return View();
            }

            // Set session
            HttpContext.Session.SetInt32("EmployeeId", employee.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", employee.FullName ?? "");
            HttpContext.Session.SetString("Email", employee.Email ?? "");

            HttpContext.Session.SetString(
                "IsSystemAdmin",
                await _dbContext.SystemAdministrators.AnyAsync(s => s.EmployeeId == employee.EmployeeId) ? "True" : "False");

            HttpContext.Session.SetString(
                "IsHRAdmin",
                await _dbContext.Hradministrators.AnyAsync(h => h.EmployeeId == employee.EmployeeId) ? "True" : "False");

            HttpContext.Session.SetString(
                "IsLineManager",
                await _dbContext.LineManagers.AnyAsync(l => l.EmployeeId == employee.EmployeeId) ? "True" : "False");

            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // FORGOT PASSWORD - GET
        // ======================================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // ======================================================
        // FORGOT PASSWORD - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            Email = Email.Trim();
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.Email != null && e.Email.ToLower() == Email.ToLower());

            // Always show success message to prevent email enumeration
            ViewBag.Success = "If an account with that email exists, a password reset link has been sent.";

            if (employee != null)
            {
                // Generate reset token (using employee ID + timestamp hash)
                string token = GenerateResetToken(employee.EmployeeId);
                string resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?token={token}&id={employee.EmployeeId}";

                // Log the reset request as a notification (for demo - in production, send email)
                var notification = new Notification
                {
                    MessageContent = $"Password reset requested. Reset link: {resetLink}",
                    Timestamp = DateTime.Now,
                    Urgency = "High",
                    ReadStatus = false,
                    NotificationType = "PasswordReset"
                };
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Link notification to employee
                _dbContext.EmployeeNotifications.Add(new EmployeeNotification
                {
                    EmployeeId = employee.EmployeeId,
                    NotificationId = notification.NotificationId,
                    DeliveryStatus = "Sent",
                    DeliveredAt = DateTime.Now
                });
                await _dbContext.SaveChangesAsync();

                // For demo purposes, show the link (remove in production)
                ViewBag.DemoResetLink = resetLink;

                // Console output for testing
                Console.WriteLine("========================================");
                Console.WriteLine("PASSWORD RESET EMAIL (DEMO)");
                Console.WriteLine($"To: {employee.Email}");
                Console.WriteLine($"Subject: HRMS Password Reset");
                Console.WriteLine($"Reset Link: {resetLink}");
                Console.WriteLine("========================================");
            }

            return View();
        }

        // ======================================================
        // RESET PASSWORD - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, int id)
        {
            if (string.IsNullOrEmpty(token) || id == 0)
            {
                ViewBag.Error = "Invalid or expired reset link.";
                return View("ForgotPassword");
            }

            // Validate token
            string expectedToken = GenerateResetToken(id);
            if (token != expectedToken)
            {
                ViewBag.Error = "Invalid or expired reset link.";
                return View("ForgotPassword");
            }

            var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null)
            {
                ViewBag.Error = "Invalid or expired reset link.";
                return View("ForgotPassword");
            }

            ViewBag.EmployeeId = id;
            ViewBag.Token = token;
            ViewBag.Email = employee.Email;

            return View();
        }

        // ======================================================
        // RESET PASSWORD - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int EmployeeId, string Token, string NewPassword, string ConfirmPassword)
        {
            ViewBag.EmployeeId = EmployeeId;
            ViewBag.Token = Token;

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ViewBag.Error = "Please enter a new password.";
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (NewPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters long.";
                return View();
            }

            // Validate token
            string expectedToken = GenerateResetToken(EmployeeId);
            if (Token != expectedToken)
            {
                ViewBag.Error = "Invalid or expired reset link. Please request a new one.";
                return View("ForgotPassword");
            }

            var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);
            if (employee == null)
            {
                ViewBag.Error = "Employee not found.";
                return View("ForgotPassword");
            }

            // Update password (NationalId field)
            employee.NationalId = NewPassword;

            // Unlock account if it was locked
            if (employee.AccountStatus == "Locked")
            {
                employee.AccountStatus = "Active";
            }

            // Reset failed attempts
            SetFailedAttempts(employee, 0);

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Your password has been reset successfully. Please login with your new password.";
            return RedirectToAction("Login");
        }

        // ======================================================
        // LOGIN ACTIVITY - VIEW (ADMIN ONLY)
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> LoginActivity()
        {
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True" &&
                HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            // Get login activity from Notifications with type "LoginActivity"
            var activities = await _dbContext.Notifications
                .Where(n => n.NotificationType == "LoginActivity")
                .OrderByDescending(n => n.Timestamp)
                .Take(100)
                .ToListAsync();

            return View(activities);
        }

        // ======================================================
        // MY LOGIN ACTIVITY - VIEW OWN
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> MyLoginActivity()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null)
                return RedirectToAction("Login");

            var activities = await _dbContext.EmployeeNotifications
                .Include(en => en.Notification)
                .Where(en => en.EmployeeId == employeeId &&
                            en.Notification.NotificationType == "LoginActivity")
                .OrderByDescending(en => en.Notification.Timestamp)
                .Take(20)
                .Select(en => en.Notification)
                .ToListAsync();

            return View("LoginActivity", activities);
        }

        // ======================================================
        // UNLOCK ACCOUNT - ADMIN ONLY
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(int id)
        {
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True" &&
                HttpContext.Session.GetString("IsHRAdmin") != "True")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("AllEmployees", "Home");
            }

            employee.AccountStatus = "Active";
            SetFailedAttempts(employee, 0);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = $"Account for {employee.FullName} has been unlocked.";
            return RedirectToAction("AllEmployees", "Home");
        }

        // ======================================================
        // SELF-REGISTRATION — SYSTEM / HR / LINE MANAGER
        // ======================================================
        [HttpGet]
        public IActionResult RegisterAdmin()
        {
            return View("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(
            string FirstName,
            string LastName,
            string Email,
            string Password,
            string Phone,
            string Address,
            string Role)
        {
            return await CreateUserWithRole(
                FirstName, LastName, Email, Password, Phone, Address, Role, false);
        }

        // ======================================================
        // SYSTEM ADMIN → CREATE EMPLOYEE (ANY ROLE)
        // ======================================================
        [HttpGet]
        public IActionResult CreateEmployee()
        {
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
                return Unauthorized();

            return View("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(
            string FirstName,
            string LastName,
            string Email,
            string Password,
            string Phone,
            string Address,
            string Role)
        {
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
                return Unauthorized();

            return await CreateUserWithRole(
                FirstName, LastName, Email, Password, Phone, Address, Role, true);
        }

        // ======================================================
        // SHARED USER CREATION LOGIC
        // ======================================================
        private async Task<IActionResult> CreateUserWithRole(
            string FirstName,
            string LastName,
            string Email,
            string Password,
            string Phone,
            string Address,
            string Role,
            bool createdBySystemAdmin)
        {
            if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(Role))
            {
                ViewBag.Error = "Please fill in all required fields.";
                return View("Create");
            }

            // Password strength validation
            if (Password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters long.";
                return View("Create");
            }

            Email = Email.Trim();

            if (await _dbContext.Employees.AnyAsync(e => e.Email == Email))
            {
                ViewBag.Error = "An account with this email already exists.";
                return View("Create");
            }

            string dbRoleName = Role switch
            {
                "System Administrator" => "SYS_ADMIN",
                "HR Administrator" => "HR_ADMIN",
                "Line Manager" => "LINE_MANAGER",
                "Employee" => "EMPLOYEE",
                _ => null
            };

            if (dbRoleName == null)
            {
                ViewBag.Error = "Invalid role selected.";
                return View("Create");
            }

            var roleEntity = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.RoleName == dbRoleName);

            if (roleEntity == null)
            {
                ViewBag.Error = "Role not found in database.";
                return View("Create");
            }

            var employee = new Employee
            {
                FirstName = FirstName,
                LastName = LastName,
                FullName = $"{FirstName} {LastName}",
                Email = Email,
                NationalId = Password,
                Phone = Phone ?? "",
                Address = Address ?? "",
                IsActive = true,
                EmploymentStatus = "Active",
                AccountStatus = "Active",
                EmploymentProgress = "onboarded"
            };

            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();

            _dbContext.EmployeeRoles.Add(new EmployeeRole
            {
                EmployeeId = employee.EmployeeId,
                RoleId = roleEntity.RoleId,
                AssignedDate = DateTime.Now
            });

            if (dbRoleName == "SYS_ADMIN")
                _dbContext.SystemAdministrators.Add(new SystemAdministrator { EmployeeId = employee.EmployeeId });
            else if (dbRoleName == "HR_ADMIN")
                _dbContext.Hradministrators.Add(new Hradministrator { EmployeeId = employee.EmployeeId });
            else if (dbRoleName == "LINE_MANAGER")
                _dbContext.LineManagers.Add(new LineManager { EmployeeId = employee.EmployeeId });

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Account created successfully.";

            return createdBySystemAdmin
                ? RedirectToAction("Index", "Home")
                : RedirectToAction("Login");
        }

        // ======================================================
        // ASSIGN ROLE - SYSTEM ADMIN ONLY
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> AssignRole(int id)
        {
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Only System Administrators can assign roles.";
                return RedirectToAction("Index", "Home");
            }

            var employee = await _dbContext.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            var currentRole = employee.EmployeeRoles.FirstOrDefault()?.Role?.RoleName;
            ViewBag.CurrentRole = currentRole;
            ViewBag.EmployeeName = employee.FullName;

            var roles = await _dbContext.Roles
                .Where(r => r.RoleName == "SYS_ADMIN" ||
                           r.RoleName == "HR_ADMIN" ||
                           r.RoleName == "LINE_MANAGER" ||
                           r.RoleName == "EMPLOYEE")
                .ToListAsync();

            ViewBag.Roles = roles;
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int EmployeeId, string NewRole)
        {
            if (HttpContext.Session.GetString("IsSystemAdmin") != "True")
            {
                TempData["Error"] = "Only System Administrators can assign roles.";
                return RedirectToAction("Index", "Home");
            }

            var employee = await _dbContext.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("AllEmployees", "Home");
            }

            if (string.IsNullOrWhiteSpace(NewRole))
            {
                TempData["Error"] = "Please select a role.";
                return RedirectToAction("AssignRole", new { id = EmployeeId });
            }

            string dbRoleName = NewRole switch
            {
                "System Administrator" => "SYS_ADMIN",
                "HR Administrator" => "HR_ADMIN",
                "Line Manager" => "LINE_MANAGER",
                "Employee" => "EMPLOYEE",
                _ => null
            };

            if (dbRoleName == null)
            {
                TempData["Error"] = "Invalid role selected.";
                return RedirectToAction("AssignRole", new { id = EmployeeId });
            }

            var newRoleEntity = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.RoleName == dbRoleName);

            if (newRoleEntity == null)
            {
                TempData["Error"] = "Role not found in database.";
                return RedirectToAction("AssignRole", new { id = EmployeeId });
            }

            var currentEmployeeRole = employee.EmployeeRoles.FirstOrDefault();
            var currentRoleName = currentEmployeeRole?.Role?.RoleName;

            if (currentRoleName != dbRoleName)
            {
                if (currentEmployeeRole != null)
                {
                    _dbContext.EmployeeRoles.Remove(currentEmployeeRole);
                }

                if (currentRoleName == "SYS_ADMIN")
                {
                    var sysAdmin = await _dbContext.SystemAdministrators
                        .FirstOrDefaultAsync(s => s.EmployeeId == EmployeeId);
                    if (sysAdmin != null)
                        _dbContext.SystemAdministrators.Remove(sysAdmin);
                }
                else if (currentRoleName == "HR_ADMIN")
                {
                    var hrAdmin = await _dbContext.Hradministrators
                        .FirstOrDefaultAsync(h => h.EmployeeId == EmployeeId);
                    if (hrAdmin != null)
                        _dbContext.Hradministrators.Remove(hrAdmin);
                }
                else if (currentRoleName == "LINE_MANAGER")
                {
                    var lineManager = await _dbContext.LineManagers
                        .FirstOrDefaultAsync(l => l.EmployeeId == EmployeeId);
                    if (lineManager != null)
                        _dbContext.LineManagers.Remove(lineManager);
                }

                _dbContext.EmployeeRoles.Add(new EmployeeRole
                {
                    EmployeeId = EmployeeId,
                    RoleId = newRoleEntity.RoleId,
                    AssignedDate = DateTime.Now
                });

                if (dbRoleName == "SYS_ADMIN")
                {
                    _dbContext.SystemAdministrators.Add(new SystemAdministrator { EmployeeId = EmployeeId });
                }
                else if (dbRoleName == "HR_ADMIN")
                {
                    _dbContext.Hradministrators.Add(new Hradministrator { EmployeeId = EmployeeId });
                }
                else if (dbRoleName == "LINE_MANAGER")
                {
                    _dbContext.LineManagers.Add(new LineManager { EmployeeId = EmployeeId });
                }

                await _dbContext.SaveChangesAsync();
                TempData["Success"] = $"Role successfully assigned to {employee.FullName}.";
            }
            else
            {
                TempData["Info"] = $"{employee.FullName} already has this role.";
            }

            return RedirectToAction("AllEmployees", "Home");
        }

        // ======================================================
        // EDIT PROFILE
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? sessionId = HttpContext.Session.GetInt32("EmployeeId");
            bool isHrAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";

            if (sessionId == null)
                return Unauthorized();

            if (!isHrAdmin && !isSystemAdmin && sessionId != id)
                return Unauthorized();

            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee model, IFormFile? ProfileImageFile)
        {
            int? sessionId = HttpContext.Session.GetInt32("EmployeeId");
            bool isHrAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";

            if (sessionId == null)
                return Unauthorized();

            if (!isHrAdmin && !isSystemAdmin && sessionId != model.EmployeeId)
                return Unauthorized();

            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId);

            if (employee == null)
                return NotFound();

            if (ProfileImageFile != null && ProfileImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(ProfileImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.";
                    return View(employee);
                }

                if (ProfileImageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File size too large. Maximum size is 5MB.";
                    return View(employee);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (!string.IsNullOrEmpty(employee.ProfileImage))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", employee.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var uniqueFileName = $"{employee.EmployeeId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImageFile.CopyToAsync(fileStream);
                }

                employee.ProfileImage = $"/uploads/profiles/{uniqueFileName}";
            }

            if (isHrAdmin || isSystemAdmin)
            {
                employee.FirstName = model.FirstName;
                employee.LastName = model.LastName;
                employee.FullName = $"{model.FirstName} {model.LastName}";
                employee.Email = model.Email;
                employee.Phone = model.Phone;
                employee.Address = model.Address;
                employee.CountryOfBirth = model.CountryOfBirth;
                employee.EmploymentStatus = model.EmploymentStatus;
                employee.AccountStatus = model.AccountStatus;
                employee.IsActive = model.IsActive;
            }
            else
            {
                employee.Phone = model.Phone;
                employee.Address = model.Address;
                employee.Biography = model.Biography;
                employee.EmergencyContactName = model.EmergencyContactName;
                employee.EmergencyContactPhone = model.EmergencyContactPhone;
                employee.Relationship = model.Relationship;
            }

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";

            if (!isHrAdmin && !isSystemAdmin && sessionId == model.EmployeeId)
            {
                return RedirectToAction("Details", "Employees", new { id = model.EmployeeId });
            }

            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // REMOVE PROFILE PICTURE
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture(int id)
        {
            int? sessionId = HttpContext.Session.GetInt32("EmployeeId");
            bool isHrAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";

            if (sessionId == null)
                return Unauthorized();

            if (!isHrAdmin && !isSystemAdmin && sessionId != id)
                return Unauthorized();

            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            if (!string.IsNullOrEmpty(employee.ProfileImage))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", employee.ProfileImage.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                employee.ProfileImage = null;
                await _dbContext.SaveChangesAsync();

                TempData["Success"] = "Profile picture removed successfully.";
            }

            return RedirectToAction("Edit", new { id = id });
        }

        // ======================================================
        // CHANGE PASSWORD
        // ======================================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ViewBag.Error = "Please fill in all fields.";
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View();
            }

            if (NewPassword.Length < 6)
            {
                ViewBag.Error = "New password must be at least 6 characters long.";
                return View();
            }

            var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee == null)
                return NotFound();

            if (employee.NationalId != CurrentPassword)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            employee.NationalId = NewPassword;
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Index", "Home");
        }

        // ======================================================
        // LOGOUT
        // ======================================================
        public IActionResult Logout()
        {
            // Clear Remember Me cookies
            Response.Cookies.Delete("RememberMe_EmployeeId");
            Response.Cookies.Delete("RememberMe_Token");

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ======================================================
        // HELPER METHODS
        // ======================================================
        private async Task LogLoginActivity(int? employeeId, string email, string status, string? reason, string ipAddress, string userAgent)
        {
            try
            {
                // Truncate user agent if too long
                if (userAgent?.Length > 200)
                    userAgent = userAgent.Substring(0, 200);

                var notification = new Notification
                {
                    MessageContent = $"Login {status}: {email} | IP: {ipAddress} | {reason ?? "OK"}",
                    Timestamp = DateTime.Now,
                    Urgency = status == "Success" ? "Low" : "High",
                    ReadStatus = false,
                    NotificationType = "LoginActivity"
                };

                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                if (employeeId.HasValue)
                {
                    _dbContext.EmployeeNotifications.Add(new EmployeeNotification
                    {
                        EmployeeId = employeeId.Value,
                        NotificationId = notification.NotificationId,
                        DeliveryStatus = "Logged",
                        DeliveredAt = DateTime.Now
                    });
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error logging login activity: {ex.Message}");
            }
        }

        private string GenerateResetToken(int employeeId)
        {
            // Simple token based on employee ID and date (valid for current day)
            string data = $"{employeeId}-{DateTime.Now:yyyyMMdd}-HRMS_RESET_SECRET";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(data);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Substring(0, 32);
            }
        }

        private string GenerateRememberMeToken(Employee employee)
        {
            string data = $"{employee.EmployeeId}-{employee.Email}-{employee.NationalId}-HRMS_REMEMBER";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(data);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Substring(0, 32);
            }
        }

        // Using EmploymentProgress field to store failed attempts (temporary solution)
        private int GetFailedAttempts(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.EmploymentProgress))
                return 0;

            if (employee.EmploymentProgress.StartsWith("FAILED:"))
            {
                if (int.TryParse(employee.EmploymentProgress.Substring(7), out int attempts))
                    return attempts;
            }
            return 0;
        }

        private void SetFailedAttempts(Employee employee, int attempts)
        {
            if (attempts == 0)
            {
                if (employee.EmploymentProgress?.StartsWith("FAILED:") == true)
                    employee.EmploymentProgress = "onboarded";
            }
            else
            {
                employee.EmploymentProgress = $"FAILED:{attempts}";
            }
        }
    }
}