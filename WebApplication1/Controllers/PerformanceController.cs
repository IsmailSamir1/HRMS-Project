using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class PerformanceController : Controller
    {
        private readonly HrmsDatabasesContext _context;

        public PerformanceController(HrmsDatabasesContext context)
        {
            _context = context;
        }

        // ======================================================
        // PERFORMANCE DASHBOARD - INDEX
        // ======================================================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            // Get performance data
            var performanceData = await CalculatePerformanceScore(employeeId);

            ViewBag.Employee = employee;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.PerformanceData = performanceData;

            // Get recent manager notes
            var recentNotes = await _context.ManagerNotes
                .Include(n => n.Manager)
                .Where(n => n.EmployeeId == employeeId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentNotes = recentNotes;

            // Get employee skills
            var skills = await _context.EmployeeSkills
                .Include(es => es.Skill)
                .Where(es => es.EmployeeId == employeeId)
                .ToListAsync();

            ViewBag.Skills = skills;

            // Get missions
            var missions = await _context.Missions
                .Where(m => m.EmployeeId == employeeId)
                .OrderByDescending(m => m.StartDate)
                .Take(10)
                .ToListAsync();

            ViewBag.Missions = missions;

            return View();
        }

        // ======================================================
        // EMPLOYEE PERFORMANCE DETAIL (For Managers/HR)
        // ======================================================
        public async Task<IActionResult> EmployeePerformance(int id)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int currentUserId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            // Check access
            if (!isSystemAdmin && !isHRAdmin)
            {
                if (isLineManager)
                {
                    var isTeamMember = await _context.Employees
                        .AnyAsync(e => e.EmployeeId == id && e.ManagerId == currentUserId);

                    if (!isTeamMember && id != currentUserId)
                    {
                        TempData["Error"] = "Access denied. This employee is not in your team.";
                        return RedirectToAction("Index");
                    }
                }
                else if (id != currentUserId)
                {
                    TempData["Error"] = "Access denied.";
                    return RedirectToAction("Index");
                }
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            var performanceData = await CalculatePerformanceScore(id);

            ViewBag.Employee = employee;
            ViewBag.PerformanceData = performanceData;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.IsLineManager = isLineManager;
            ViewBag.CanAddNote = isSystemAdmin || isHRAdmin || (isLineManager && employee.ManagerId == currentUserId);

            // Get all manager notes
            var notes = await _context.ManagerNotes
                .Include(n => n.Manager)
                .Where(n => n.EmployeeId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.Notes = notes;

            // Get employee skills
            var skills = await _context.EmployeeSkills
                .Include(es => es.Skill)
                .Where(es => es.EmployeeId == id)
                .ToListAsync();

            ViewBag.Skills = skills;

            // Get all missions
            var missions = await _context.Missions
                .Where(m => m.EmployeeId == id)
                .OrderByDescending(m => m.StartDate)
                .ToListAsync();

            ViewBag.Missions = missions;

            // Get attendance summary
            var currentYear = DateTime.Now.Year;
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == id && a.EntryTime.HasValue && a.EntryTime.Value.Year == currentYear)
                .ToListAsync();

            ViewBag.Attendances = attendances;

            // Get leave summary
            var leaves = await _context.LeaveRequests
                .Include(l => l.Leave)
                .Where(l => l.EmployeeId == id && l.ApprovalTiming.HasValue && l.ApprovalTiming.Value.Year == currentYear)
                .ToListAsync();

            ViewBag.Leaves = leaves;

            return View();
        }

        // ======================================================
        // TEAM PERFORMANCE (For Managers)
        // ======================================================
        public async Task<IActionResult> TeamPerformance()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied. Only managers can view team performance.";
                return RedirectToAction("Index");
            }

            List<Employee> teamMembers;

            if (isSystemAdmin || isHRAdmin)
            {
                teamMembers = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Position)
                    .Where(e => e.IsActive == true)
                    .OrderBy(e => e.Department.DepartmentName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();
            }
            else
            {
                teamMembers = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Position)
                    .Where(e => e.ManagerId == managerId && e.IsActive == true)
                    .OrderBy(e => e.FirstName)
                    .ToListAsync();
            }

            // Calculate performance for each team member
            var teamPerformance = new List<TeamMemberPerformance>();

            foreach (var member in teamMembers)
            {
                var perfData = await CalculatePerformanceScore(member.EmployeeId);
                teamPerformance.Add(new TeamMemberPerformance
                {
                    Employee = member,
                    PerformanceData = perfData
                });
            }

            // Sort by overall score descending
            teamPerformance = teamPerformance
                .OrderByDescending(t => t.PerformanceData.OverallScore)
                .ToList();

            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;
            ViewBag.TeamCount = teamMembers.Count;
            ViewBag.AverageScore = teamPerformance.Any() ? teamPerformance.Average(t => t.PerformanceData.OverallScore) : 0;
            ViewBag.TopPerformers = teamPerformance.Take(3).ToList();
            ViewBag.NeedImprovement = teamPerformance.Where(t => t.PerformanceData.OverallScore < 60).ToList();

            return View(teamPerformance);
        }

        // ======================================================
        // SKILLS MANAGEMENT
        // ======================================================
        public async Task<IActionResult> Skills()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            // Get all skills
            var allSkills = await _context.Skills.ToListAsync();

            // Get employee's current skills
            var employeeSkills = await _context.EmployeeSkills
                .Include(es => es.Skill)
                .Where(es => es.EmployeeId == employeeId)
                .ToListAsync();

            ViewBag.AllSkills = allSkills;
            ViewBag.EmployeeSkills = employeeSkills;
            ViewBag.IsSystemAdmin = isSystemAdmin;
            ViewBag.IsHRAdmin = isHRAdmin;

            return View();
        }

        // ======================================================
        // ADD/UPDATE SKILL - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSkill(int skillId, string proficiencyLevel)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var existingSkill = await _context.EmployeeSkills
                .FirstOrDefaultAsync(es => es.EmployeeId == employeeId && es.SkillId == skillId);

            if (existingSkill != null)
            {
                existingSkill.ProficiencyLevel = proficiencyLevel;
            }
            else
            {
                var newSkill = new EmployeeSkill
                {
                    EmployeeId = employeeId,
                    SkillId = skillId,
                    ProficiencyLevel = proficiencyLevel
                };
                _context.EmployeeSkills.Add(newSkill);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Skill updated successfully!";

            return RedirectToAction("Skills");
        }

        // ======================================================
        // REMOVE SKILL - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSkill(int skillId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

            var skill = await _context.EmployeeSkills
                .FirstOrDefaultAsync(es => es.EmployeeId == employeeId && es.SkillId == skillId);

            if (skill != null)
            {
                _context.EmployeeSkills.Remove(skill);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Skill removed successfully!";
            }

            return RedirectToAction("Skills");
        }

        // ======================================================
        // ADD MANAGER NOTE - GET
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> AddNote(int employeeId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return NotFound();

            // Check if manager has access
            if (!isSystemAdmin && !isHRAdmin && employee.ManagerId != managerId)
            {
                TempData["Error"] = "Access denied. This employee is not in your team.";
                return RedirectToAction("Index");
            }

            ViewBag.Employee = employee;

            return View();
        }

        // ======================================================
        // ADD MANAGER NOTE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int employeeId, string noteContent)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";
            bool isLineManager = HttpContext.Session.GetString("IsLineManager") == "True";

            if (!isSystemAdmin && !isHRAdmin && !isLineManager)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(noteContent))
            {
                TempData["Error"] = "Note content cannot be empty.";
                return RedirectToAction("AddNote", new { employeeId });
            }

            var note = new ManagerNote
            {
                EmployeeId = employeeId,
                ManagerId = managerId,
                NoteContent = noteContent,
                CreatedAt = DateTime.Now
            };

            _context.ManagerNotes.Add(note);
            await _context.SaveChangesAsync();

            // Send notification to employee
            await SendNotification(employeeId, "You have received new feedback from your manager.", "Performance", "Normal");

            TempData["Success"] = "Note added successfully!";
            return RedirectToAction("EmployeePerformance", new { id = employeeId });
        }

        // ======================================================
        // DELETE MANAGER NOTE - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(int noteId, int employeeId)
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login", "Account");

            int managerId = HttpContext.Session.GetInt32("EmployeeId").Value;
            bool isSystemAdmin = HttpContext.Session.GetString("IsSystemAdmin") == "True";
            bool isHRAdmin = HttpContext.Session.GetString("IsHRAdmin") == "True";

            var note = await _context.ManagerNotes.FirstOrDefaultAsync(n => n.NoteId == noteId);

            if (note == null)
            {
                TempData["Error"] = "Note not found.";
                return RedirectToAction("EmployeePerformance", new { id = employeeId });
            }

            // Only the note creator or admin can delete
            if (!isSystemAdmin && !isHRAdmin && note.ManagerId != managerId)
            {
                TempData["Error"] = "You can only delete your own notes.";
                return RedirectToAction("EmployeePerformance", new { id = employeeId });
            }

            _context.ManagerNotes.Remove(note);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Note deleted successfully!";
            return RedirectToAction("EmployeePerformance", new { id = employeeId });
        }

        // ======================================================
        // SKILLS DIRECTORY (HR/Admin)
        // ======================================================
        public async Task<IActionResult> SkillsDirectory()
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

            var skills = await _context.Skills
                .Include(s => s.EmployeeSkills)
                    .ThenInclude(es => es.Employee)
                .ToListAsync();

            var skillStats = skills.Select(s => new SkillStats
            {
                Skill = s,
                TotalEmployees = s.EmployeeSkills.Count,
                BeginnerCount = s.EmployeeSkills.Count(es => es.ProficiencyLevel == "Beginner"),
                IntermediateCount = s.EmployeeSkills.Count(es => es.ProficiencyLevel == "Intermediate"),
                AdvancedCount = s.EmployeeSkills.Count(es => es.ProficiencyLevel == "Advanced"),
                ExpertCount = s.EmployeeSkills.Count(es => es.ProficiencyLevel == "Expert")
            }).ToList();

            return View(skillStats);
        }

        // ======================================================
        // CREATE SKILL (HR/Admin) - GET
        // ======================================================
        [HttpGet]
        public IActionResult CreateSkill()
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
        // CREATE SKILL (HR/Admin) - POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSkill(string skillName, string description)
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

            if (string.IsNullOrWhiteSpace(skillName))
            {
                TempData["Error"] = "Skill name is required.";
                return View();
            }

            var exists = await _context.Skills.AnyAsync(s => s.SkillName == skillName);
            if (exists)
            {
                TempData["Error"] = "A skill with this name already exists.";
                return View();
            }

            var skill = new Skill
            {
                SkillName = skillName,
                Description = description
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Skill '{skillName}' created successfully!";
            return RedirectToAction("SkillsDirectory");
        }

        // ======================================================
        // HELPER: Calculate Performance Score
        // ======================================================
        private async Task<PerformanceData> CalculatePerformanceScore(int employeeId)
        {
            var data = new PerformanceData();
            var currentYear = DateTime.Now.Year;

            // 1. ATTENDANCE SCORE (30%)
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId &&
                           a.EntryTime.HasValue &&
                           a.EntryTime.Value.Year == currentYear)
                .ToListAsync();

            var totalWorkDays = GetWorkingDaysInYear(currentYear);
            var presentDays = attendances.Count;
            var lateArrivals = attendances.Count(a => a.EntryTime.HasValue && a.EntryTime.Value.Hour >= 9); // Assuming 9 AM start

            data.AttendanceRate = totalWorkDays > 0 ? Math.Round((double)presentDays / totalWorkDays * 100, 1) : 0;
            data.LateArrivals = lateArrivals;
            data.PresentDays = presentDays;

            // Attendance score: 100 if perfect, minus points for absences and late arrivals
            double attendanceScore = 100;
            if (totalWorkDays > 0)
            {
                var absenceRate = (double)(totalWorkDays - presentDays) / totalWorkDays * 100;
                attendanceScore -= absenceRate;
                attendanceScore -= (lateArrivals * 0.5); // -0.5 for each late arrival
            }
            data.AttendanceScore = Math.Max(0, Math.Min(100, attendanceScore));

            // 2. LEAVE SCORE (20%)
            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId &&
                           l.Status == "Approved" &&
                           l.ApprovalTiming.HasValue &&
                           l.ApprovalTiming.Value.Year == currentYear)
                .ToListAsync();

            data.TotalLeaveDays = leaves.Sum(l => l.Duration ?? 0);
            data.SickLeaveDays = leaves.Where(l => l.LeaveId == 2).Sum(l => l.Duration ?? 0); // Assuming LeaveId 2 is Sick Leave

            // Leave score: Lower leave usage = higher score
            double leaveScore = 100;
            leaveScore -= (data.TotalLeaveDays * 2); // -2 for each leave day
            leaveScore -= (data.SickLeaveDays * 1); // Additional -1 for sick leaves
            data.LeaveScore = Math.Max(0, Math.Min(100, leaveScore));

            // 3. MISSION SCORE (25%)
            var missions = await _context.Missions
                .Where(m => m.EmployeeId == employeeId)
                .ToListAsync();

            data.TotalMissions = missions.Count;
            data.CompletedMissions = missions.Count(m => m.Status == "Completed");
            data.PendingMissions = missions.Count(m => m.Status == "Planned" || m.Status == "In Progress");

            // Mission score: Completion rate
            data.MissionScore = data.TotalMissions > 0
                ? Math.Round((double)data.CompletedMissions / data.TotalMissions * 100, 1)
                : 100; // If no missions, assume 100

            // 4. SKILLS SCORE (25%)
            var skills = await _context.EmployeeSkills
                .Where(es => es.EmployeeId == employeeId)
                .ToListAsync();

            data.TotalSkills = skills.Count;
            data.ExpertSkills = skills.Count(s => s.ProficiencyLevel == "Expert");
            data.AdvancedSkills = skills.Count(s => s.ProficiencyLevel == "Advanced");

            // Skills score: Based on proficiency levels
            double skillPoints = 0;
            skillPoints += skills.Count(s => s.ProficiencyLevel == "Beginner") * 25;
            skillPoints += skills.Count(s => s.ProficiencyLevel == "Intermediate") * 50;
            skillPoints += skills.Count(s => s.ProficiencyLevel == "Advanced") * 75;
            skillPoints += skills.Count(s => s.ProficiencyLevel == "Expert") * 100;

            data.SkillsScore = skills.Count > 0 ? Math.Round(skillPoints / skills.Count, 1) : 50;

            // 5. CALCULATE OVERALL SCORE
            data.OverallScore = Math.Round(
                (data.AttendanceScore * 0.30) +
                (data.LeaveScore * 0.20) +
                (data.MissionScore * 0.25) +
                (data.SkillsScore * 0.25),
                1
            );

            // 6. DETERMINE PERFORMANCE LEVEL
            data.PerformanceLevel = data.OverallScore switch
            {
                >= 90 => "Outstanding",
                >= 80 => "Excellent",
                >= 70 => "Good",
                >= 60 => "Satisfactory",
                >= 50 => "Needs Improvement",
                _ => "Poor"
            };

            // 7. DETERMINE BADGE COLOR
            data.BadgeColor = data.OverallScore switch
            {
                >= 90 => "success",
                >= 80 => "primary",
                >= 70 => "info",
                >= 60 => "warning",
                _ => "danger"
            };

            return data;
        }

        // ======================================================
        // HELPER: Get Working Days in Year
        // ======================================================
        private int GetWorkingDaysInYear(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = DateTime.Now < new DateTime(year, 12, 31) ? DateTime.Now : new DateTime(year, 12, 31);

            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        // ======================================================
        // HELPER: Send Notification
        // ======================================================
        private async Task SendNotification(int employeeId, string message, string type, string urgency)
        {
            try
            {
                var notification = new Notification
                {
                    MessageContent = message,
                    Timestamp = DateTime.Now,
                    Urgency = urgency,
                    ReadStatus = false,
                    NotificationType = type
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

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
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }

    // ======================================================
    // VIEW MODELS
    // ======================================================
    public class PerformanceData
    {
        // Attendance
        public double AttendanceRate { get; set; }
        public int PresentDays { get; set; }
        public int LateArrivals { get; set; }
        public double AttendanceScore { get; set; }

        // Leave
        public int TotalLeaveDays { get; set; }
        public int SickLeaveDays { get; set; }
        public double LeaveScore { get; set; }

        // Missions
        public int TotalMissions { get; set; }
        public int CompletedMissions { get; set; }
        public int PendingMissions { get; set; }
        public double MissionScore { get; set; }

        // Skills
        public int TotalSkills { get; set; }
        public int ExpertSkills { get; set; }
        public int AdvancedSkills { get; set; }
        public double SkillsScore { get; set; }

        // Overall
        public double OverallScore { get; set; }
        public string PerformanceLevel { get; set; } = "";
        public string BadgeColor { get; set; } = "";
    }

    public class TeamMemberPerformance
    {
        public Employee Employee { get; set; } = null!;
        public PerformanceData PerformanceData { get; set; } = null!;
    }

    public class SkillStats
    {
        public Skill Skill { get; set; } = null!;
        public int TotalEmployees { get; set; }
        public int BeginnerCount { get; set; }
        public int IntermediateCount { get; set; }
        public int AdvancedCount { get; set; }
        public int ExpertCount { get; set; }
    }
}