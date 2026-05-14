using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Employee
{
    private string? profileImage;

    public int EmployeeId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? FullName { get; set; }

    public string? NationalId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? CountryOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? Relationship { get; set; }

    public string? Biography { get; set; }

    public string? ProfileImage { get; set; }

    public string? EmploymentProgress { get; set; }

    public string? AccountStatus { get; set; }

    public string? EmploymentStatus { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool? IsActive { get; set; }

    public int? ProfileCompletion { get; set; }

    public int? DepartmentId { get; set; }

    public int? PositionId { get; set; }

    public int? ManagerId { get; set; }

    public int? ContractId { get; set; }

    public int? TaxFormId { get; set; }

    public int? SalaryTypeId { get; set; }

    public int? PayGrade { get; set; }


    public virtual ICollection<AllowanceDeduction> AllowanceDeductions { get; set; } = new List<AllowanceDeduction>();

    public virtual ICollection<AttendanceCorrectionRequest> AttendanceCorrectionRequestEmployees { get; set; } = new List<AttendanceCorrectionRequest>();

    public virtual ICollection<AttendanceCorrectionRequest> AttendanceCorrectionRequestRecordedByNavigations { get; set; } = new List<AttendanceCorrectionRequest>();

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Contract? Contract { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual ICollection<EmployeeHierarchy> EmployeeHierarchyEmployees { get; set; } = new List<EmployeeHierarchy>();

    public virtual ICollection<EmployeeHierarchy> EmployeeHierarchyManagers { get; set; } = new List<EmployeeHierarchy>();

    public virtual ICollection<EmployeeNotification> EmployeeNotifications { get; set; } = new List<EmployeeNotification>();

    public virtual ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();

    public virtual ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();

    public virtual Hradministrator? Hradministrator { get; set; }

    public virtual ICollection<Employee> InverseManager { get; set; } = new List<Employee>();

    public virtual ICollection<LeaveEntitlement> LeaveEntitlements { get; set; } = new List<LeaveEntitlement>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public virtual LineManager? LineManager { get; set; }

    public virtual Employee? Manager { get; set; }

    public virtual ICollection<ManagerNote> ManagerNoteEmployees { get; set; } = new List<ManagerNote>();

    public virtual ICollection<ManagerNote> ManagerNoteManagers { get; set; } = new List<ManagerNote>();

    public virtual ICollection<Mission> MissionEmployees { get; set; } = new List<Mission>();

    public virtual ICollection<Mission> MissionManagers { get; set; } = new List<Mission>();

    public virtual PayGrade? PayGradeNavigation { get; set; }

    public virtual PayrollSpecialist? PayrollSpecialist { get; set; }

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual Position? Position { get; set; }

    public virtual ICollection<Reimbursement> Reimbursements { get; set; } = new List<Reimbursement>();

    public virtual SalaryType? SalaryType { get; set; }

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();

    public virtual SystemAdministrator? SystemAdministrator { get; set; }

    public virtual TaxForm? TaxForm { get; set; }

    public virtual ICollection<Exception> Exceptions { get; set; } = new List<Exception>();

    public virtual ICollection<Verification> Verifications { get; set; } = new List<Verification>();
}
