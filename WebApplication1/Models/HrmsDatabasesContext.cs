using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models;

public partial class HrmsDatabasesContext : DbContext
{
    public HrmsDatabasesContext()
    {
    }

    public HrmsDatabasesContext(DbContextOptions<HrmsDatabasesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AllowanceDeduction> AllowanceDeductions { get; set; }

    public virtual DbSet<ApprovalWorkflow> ApprovalWorkflows { get; set; }

    public virtual DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests { get; set; }

    public virtual DbSet<AttendanceLog> AttendanceLogs { get; set; }

    public virtual DbSet<AttendanceSource> AttendanceSources { get; set; }

    public virtual DbSet<BonusPolicy> BonusPolicies { get; set; }

    public virtual DbSet<ConsultantContract> ConsultantContracts { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<ContractSalaryType> ContractSalaryTypes { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<DeductionPolicy> DeductionPolicies { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeHierarchy> EmployeeHierarchies { get; set; }

    public virtual DbSet<EmployeeNotification> EmployeeNotifications { get; set; }

    public virtual DbSet<EmployeeRole> EmployeeRoles { get; set; }

    public virtual DbSet<EmployeeSkill> EmployeeSkills { get; set; }

    public virtual DbSet<Exception> Exceptions { get; set; }

    public virtual DbSet<FullTimeContract> FullTimeContracts { get; set; }

    public virtual DbSet<HolidayLeave> HolidayLeaves { get; set; }

    public virtual DbSet<HourlySalaryType> HourlySalaryTypes { get; set; }

    public virtual DbSet<Hradministrator> Hradministrators { get; set; }

    public virtual DbSet<Insurance> Insurances { get; set; }

    public virtual DbSet<InternshipContract> InternshipContracts { get; set; }

    public virtual DbSet<LatenessPolicy> LatenessPolicies { get; set; }

    public virtual DbSet<Leave> Leaves { get; set; }

    public virtual DbSet<LeaveDocument> LeaveDocuments { get; set; }

    public virtual DbSet<LeaveEntitlement> LeaveEntitlements { get; set; }

    public virtual DbSet<LeavePolicy> LeavePolicies { get; set; }

    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }

    public virtual DbSet<LineManager> LineManagers { get; set; }

    public virtual DbSet<ManagerNote> ManagerNotes { get; set; }

    public virtual DbSet<Mission> Missions { get; set; }

    public virtual DbSet<MonthlySalaryType> MonthlySalaryTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OvertimePolicy> OvertimePolicies { get; set; }

    public virtual DbSet<PartTimeContract> PartTimeContracts { get; set; }

    public virtual DbSet<PayGrade> PayGrades { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<PayrollLog> PayrollLogs { get; set; }

    public virtual DbSet<PayrollPeriod> PayrollPeriods { get; set; }

    public virtual DbSet<PayrollPolicy> PayrollPolicies { get; set; }

    public virtual DbSet<PayrollSpecialist> PayrollSpecialists { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<ProbationLeave> ProbationLeaves { get; set; }

    public virtual DbSet<Reimbursement> Reimbursements { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<SalaryType> SalaryTypes { get; set; }

    public virtual DbSet<ShiftAssignment> ShiftAssignments { get; set; }

    public virtual DbSet<ShiftCycle> ShiftCycles { get; set; }

    public virtual DbSet<ShiftCycleAssignment> ShiftCycleAssignments { get; set; }

    public virtual DbSet<ShiftSchedule> ShiftSchedules { get; set; }

    public virtual DbSet<SickLeave> SickLeaves { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SystemAdministrator> SystemAdministrators { get; set; }

    public virtual DbSet<TaxForm> TaxForms { get; set; }

    public virtual DbSet<Termination> Terminations { get; set; }

    public virtual DbSet<VacationLeave> VacationLeaves { get; set; }

    public virtual DbSet<Verification> Verifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=HRMS__Databases;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllowanceDeduction>(entity =>
        {
            entity.HasKey(e => e.AdId).HasName("PK__Allowanc__CAA4A6276DAF3D53");

            entity.ToTable("AllowanceDeduction");

            entity.Property(e => e.AdId).HasColumnName("ad_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("currency");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.Timezone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("timezone");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.CurrencyNavigation).WithMany(p => p.AllowanceDeductions)
                .HasForeignKey(d => d.Currency)
                .HasConstraintName("FK__Allowance__curre__4F47C5E3");

            entity.HasOne(d => d.Employee).WithMany(p => p.AllowanceDeductions)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Allowance__emplo__4E53A1AA");

            entity.HasOne(d => d.Payroll).WithMany(p => p.AllowanceDeductions)
                .HasForeignKey(d => d.PayrollId)
                .HasConstraintName("FK__Allowance__payro__4D5F7D71");
        });

        modelBuilder.Entity<ApprovalWorkflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).HasName("PK__Approval__64A76B70A567C3AB");

            entity.ToTable("ApprovalWorkflow");

            entity.Property(e => e.WorkflowId).HasColumnName("workflow_id");
            entity.Property(e => e.ApproverRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("approver_role");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.ThresholdAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("threshold_amount");
            entity.Property(e => e.WorkflowType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("workflow_type");
        });

        modelBuilder.Entity<ApprovalWorkflowStep>(entity =>
        {
            entity.HasKey(e => new { e.WorkflowId, e.StepNumber }).HasName("PK__Approval__D7E8C765DEE8C7DE");

            entity.ToTable("ApprovalWorkflowStep");

            entity.Property(e => e.WorkflowId).HasColumnName("workflow_id");
            entity.Property(e => e.StepNumber).HasColumnName("step_number");
            entity.Property(e => e.ActionRequired)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action_required");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.ApprovalWorkflowSteps)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__ApprovalW__role___756D6ECB");

            entity.HasOne(d => d.Workflow).WithMany(p => p.ApprovalWorkflowSteps)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ApprovalW__workf__74794A92");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__20D6A96841C00197");

            entity.ToTable("Attendance");

            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EntryTime)
                .HasColumnType("datetime")
                .HasColumnName("entry_time");
            entity.Property(e => e.ExceptionId).HasColumnName("exception_id");
            entity.Property(e => e.ExitTime)
                .HasColumnType("datetime")
                .HasColumnName("exit_time");
            entity.Property(e => e.LoginMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("login_method");
            entity.Property(e => e.LogoutMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("logout_method");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Attendanc__emplo__2645B050");

            entity.HasOne(d => d.Exception).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.ExceptionId)
                .HasConstraintName("FK__Attendanc__excep__282DF8C2");

            entity.HasOne(d => d.Shift).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("FK__Attendanc__shift__2739D489");
        });

        modelBuilder.Entity<AttendanceCorrectionRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Attendan__18D3B90F5FD16246");

            entity.ToTable("AttendanceCorrectionRequest");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.CorrectionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("correction_type");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("reason");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.AttendanceCorrectionRequestEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Attendanc__emplo__2DE6D218");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.AttendanceCorrectionRequestRecordedByNavigations)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK__Attendanc__recor__2EDAF651");
        });

        modelBuilder.Entity<AttendanceLog>(entity =>
        {
            entity.HasKey(e => e.AttendanceLogId).HasName("PK__Attendan__DB38FB0986A5704C");

            entity.ToTable("AttendanceLog");

            entity.Property(e => e.AttendanceLogId).HasColumnName("attendance_log_id");
            entity.Property(e => e.Actor).HasColumnName("actor");
            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("reason");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.Attendance).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.AttendanceId)
                .HasConstraintName("FK__Attendanc__atten__2B0A656D");
        });

        modelBuilder.Entity<AttendanceSource>(entity =>
        {
            entity.HasKey(e => new { e.AttendanceId, e.DeviceId }).HasName("PK__Attendan__83662CB061FAA028");

            entity.ToTable("AttendanceSource");

            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("longitude");
            entity.Property(e => e.RecordedAt)
                .HasColumnType("datetime")
                .HasColumnName("recorded_at");
            entity.Property(e => e.SourceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("source_type");

            entity.HasOne(d => d.Attendance).WithMany(p => p.AttendanceSources)
                .HasForeignKey(d => d.AttendanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__atten__3864608B");

            entity.HasOne(d => d.Device).WithMany(p => p.AttendanceSources)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__devic__395884C4");
        });

        modelBuilder.Entity<BonusPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__BonusPol__47DA3F03D0D30076");

            entity.ToTable("BonusPolicy");

            entity.Property(e => e.PolicyId)
                .ValueGeneratedNever()
                .HasColumnName("policy_id");
            entity.Property(e => e.BonusType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bonus_type");
            entity.Property(e => e.EligibilityCriteria)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("eligibility_criteria");

            entity.HasOne(d => d.Policy).WithOne(p => p.BonusPolicy)
                .HasForeignKey<BonusPolicy>(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BonusPoli__polic__59C55456");
        });

        modelBuilder.Entity<ConsultantContract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Consulta__F8D66423536600E9");

            entity.ToTable("ConsultantContract");

            entity.Property(e => e.ContractId)
                .ValueGeneratedNever()
                .HasColumnName("contract_id");
            entity.Property(e => e.Fees)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("fees");
            entity.Property(e => e.PaymentSchedule)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payment_schedule");
            entity.Property(e => e.ProjectScope)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("project_scope");

            entity.HasOne(d => d.Contract).WithOne(p => p.ConsultantContract)
                .HasForeignKey<ConsultantContract>(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Consultan__contr__73BA3083");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__F8D66423C46B939F");

            entity.ToTable("Contract");

            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CurrentState)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("current_state");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<ContractSalaryType>(entity =>
        {
            entity.HasKey(e => e.SalaryTypeId).HasName("PK__Contract__4D647062E174B9AF");

            entity.ToTable("ContractSalaryType");

            entity.Property(e => e.SalaryTypeId)
                .ValueGeneratedNever()
                .HasColumnName("salary_type_id");
            entity.Property(e => e.ContractValue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("contract_value");
            entity.Property(e => e.InstallmentDetails)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("installment_details");

            entity.HasOne(d => d.SalaryType).WithOne(p => p.ContractSalaryType)
                .HasForeignKey<ContractSalaryType>(d => d.SalaryTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContractS__salar__4A8310C6");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.CurrencyCode).HasName("PK__Currency__408426BEF375CD97");

            entity.ToTable("Currency");

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.CurrencyName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExchangeRate).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
        });

        modelBuilder.Entity<DeductionPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Deductio__47DA3F03774A722B");

            entity.ToTable("DeductionPolicy");

            entity.Property(e => e.PolicyId)
                .ValueGeneratedNever()
                .HasColumnName("policy_id");
            entity.Property(e => e.CalculationMode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("calculation_mode");
            entity.Property(e => e.DeductionReason)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("deduction_reason");

            entity.HasOne(d => d.Policy).WithOne(p => p.DeductionPolicy)
                .HasForeignKey<DeductionPolicy>(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Deduction__polic__5CA1C101");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__C2232422D5284636");

            entity.ToTable("Department");

            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.DepartmentHeadId).HasColumnName("department_head_id");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("department_name");
            entity.Property(e => e.Purpose)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("purpose");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId).HasName("PK__Device__3B085D8B21519FAE");

            entity.ToTable("Device");

            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.DeviceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("device_type");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("longitude");
            entity.Property(e => e.TerminalId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("terminal_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.Devices)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Device__employee__3587F3E0");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__C52E0BA88F9B4EAA");

            entity.ToTable("Employee");

            entity.HasIndex(e => e.Email, "UQ__Employee__AB6E6164AEC4A4A4").IsUnique();

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("account_status");
            entity.Property(e => e.Address)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.Biography)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("biography");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CountryOfBirth)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("country_of_birth");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.EmergencyContactName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("emergency_contact_name");
            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("emergency_contact_phone");
            entity.Property(e => e.EmploymentProgress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("employment_progress");
            entity.Property(e => e.EmploymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("employment_status");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.NationalId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("national_id");
            entity.Property(e => e.PayGrade).HasColumnName("pay_grade");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.ProfileCompletion).HasColumnName("profile_completion");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("profile_image");
            entity.Property(e => e.Relationship)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("relationship");
            entity.Property(e => e.SalaryTypeId).HasColumnName("salary_type_id");
            entity.Property(e => e.TaxFormId).HasColumnName("tax_form_id");

            entity.HasOne(d => d.Contract).WithMany(p => p.Employees)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("FK__Employee__contra__48CFD27E");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Employee__depart__45F365D3");

            entity.HasOne(d => d.Manager).WithMany(p => p.InverseManager)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK__Employee__manage__47DBAE45");

            entity.HasOne(d => d.PayGradeNavigation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PayGrade)
                .HasConstraintName("FK__Employee__pay_gr__4BAC3F29");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PositionId)
                .HasConstraintName("FK__Employee__positi__46E78A0C");

            entity.HasOne(d => d.SalaryType).WithMany(p => p.Employees)
                .HasForeignKey(d => d.SalaryTypeId)
                .HasConstraintName("FK__Employee__salary__4AB81AF0");

            entity.HasOne(d => d.TaxForm).WithMany(p => p.Employees)
                .HasForeignKey(d => d.TaxFormId)
                .HasConstraintName("FK__Employee__tax_fo__49C3F6B7");

            entity.HasMany(d => d.Exceptions).WithMany(p => p.Employees)
                .UsingEntity<Dictionary<string, object>>(
                    "EmployeeException",
                    r => r.HasOne<Exception>().WithMany()
                        .HasForeignKey("ExceptionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Employee___excep__32AB8735"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Employee___emplo__31B762FC"),
                    j =>
                    {
                        j.HasKey("EmployeeId", "ExceptionId").HasName("PK__Employee__F96CD5647564563D");
                        j.ToTable("Employee_Exception");
                        j.IndexerProperty<int>("EmployeeId").HasColumnName("employee_id");
                        j.IndexerProperty<int>("ExceptionId").HasColumnName("exception_id");
                    });

            entity.HasMany(d => d.Verifications).WithMany(p => p.Employees)
                .UsingEntity<Dictionary<string, object>>(
                    "EmployeeVerification",
                    r => r.HasOne<Verification>().WithMany()
                        .HasForeignKey("VerificationId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Employee___verif__6B24EA82"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Employee___emplo__6A30C649"),
                    j =>
                    {
                        j.HasKey("EmployeeId", "VerificationId").HasName("PK__Employee__47611C3E77280A18");
                        j.ToTable("Employee_Verification");
                        j.IndexerProperty<int>("EmployeeId").HasColumnName("employee_id");
                        j.IndexerProperty<int>("VerificationId").HasColumnName("verification_id");
                    });
        });

        modelBuilder.Entity<EmployeeHierarchy>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.ManagerId }).HasName("PK__Employee__10880C9706DC70E7");

            entity.ToTable("EmployeeHierarchy");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.HierarchyLevel).HasColumnName("hierarchy_level");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeHierarchyEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployeeH__emplo__6EC0713C");

            entity.HasOne(d => d.Manager).WithMany(p => p.EmployeeHierarchyManagers)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployeeH__manag__6FB49575");
        });

        modelBuilder.Entity<EmployeeNotification>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.NotificationId }).HasName("PK__Employee__2B2B93EAA25923AC");

            entity.ToTable("Employee_Notification");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.DeliveredAt)
                .HasColumnType("datetime")
                .HasColumnName("delivered_at");
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("delivery_status");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeNotifications)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee___emplo__6AEFE058");

            entity.HasOne(d => d.Notification).WithMany(p => p.EmployeeNotifications)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee___notif__6BE40491");
        });

        modelBuilder.Entity<EmployeeRole>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.RoleId }).HasName("PK__Employee__124E9DF49E5B203D");

            entity.ToTable("Employee_Role");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.AssignedDate)
                .HasColumnType("datetime")
                .HasColumnName("assigned_date");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeRoles)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee___emplo__5BE2A6F2");

            entity.HasOne(d => d.Role).WithMany(p => p.EmployeeRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee___role___5CD6CB2B");
        });

        modelBuilder.Entity<EmployeeSkill>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.SkillId }).HasName("PK__Employee__4A95A39FA10D43EC");

            entity.ToTable("Employee_Skill");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.ProficiencyLevel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("proficiency_level");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeSkills)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee___emplo__6477ECF3");

            entity.HasOne(d => d.Skill).WithMany(p => p.EmployeeSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee___skill__656C112C");
        });

        modelBuilder.Entity<Exception>(entity =>
        {
            entity.HasKey(e => e.ExceptionId).HasName("PK__Exceptio__C42DECC2DEB691ED");

            entity.ToTable("Exception");

            entity.Property(e => e.ExceptionId).HasColumnName("exception_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("category");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
        });

        modelBuilder.Entity<FullTimeContract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__FullTime__F8D66423665BC9F5");

            entity.ToTable("FullTimeContract");

            entity.Property(e => e.ContractId)
                .ValueGeneratedNever()
                .HasColumnName("contract_id");
            entity.Property(e => e.InsuranceEligibility).HasColumnName("insurance_eligibility");
            entity.Property(e => e.LeaveEntitlement).HasColumnName("leave_entitlement");
            entity.Property(e => e.WeeklyWorkingHours).HasColumnName("weekly_working_hours");

            entity.HasOne(d => d.Contract).WithOne(p => p.FullTimeContract)
                .HasForeignKey<FullTimeContract>(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FullTimeC__contr__6E01572D");
        });

        modelBuilder.Entity<HolidayLeave>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__HolidayL__743350BC7829E6C6");

            entity.ToTable("HolidayLeave");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("leave_id");
            entity.Property(e => e.HolidayName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("holiday_name");
            entity.Property(e => e.OfficialRecognition).HasColumnName("official_recognition");
            entity.Property(e => e.RegionalScope)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("regional_scope");

            entity.HasOne(d => d.Leave).WithOne(p => p.HolidayLeave)
                .HasForeignKey<HolidayLeave>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HolidayLe__leave__0F624AF8");
        });

        modelBuilder.Entity<HourlySalaryType>(entity =>
        {
            entity.HasKey(e => e.SalaryTypeId).HasName("PK__HourlySa__4D64706248975332");

            entity.ToTable("HourlySalaryType");

            entity.Property(e => e.SalaryTypeId)
                .ValueGeneratedNever()
                .HasColumnName("salary_type_id");
            entity.Property(e => e.HourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("hourly_rate");
            entity.Property(e => e.MaxMonthlyHours).HasColumnName("max_monthly_hours");

            entity.HasOne(d => d.SalaryType).WithOne(p => p.HourlySalaryType)
                .HasForeignKey<HourlySalaryType>(d => d.SalaryTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HourlySal__salar__44CA3770");
        });

        modelBuilder.Entity<Hradministrator>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__HRAdmini__C52E0BA8816268CE");

            entity.ToTable("HRAdministrator");

            entity.Property(e => e.EmployeeId)
                .ValueGeneratedNever()
                .HasColumnName("employee_id");
            entity.Property(e => e.ApprovalLevel).HasColumnName("approval_level");
            entity.Property(e => e.DocumentValidationRights)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("document_validation_rights");
            entity.Property(e => e.RecordAccessScope)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("record_access_scope");

            entity.HasOne(d => d.Employee).WithOne(p => p.Hradministrator)
                .HasForeignKey<Hradministrator>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HRAdminis__emplo__4E88ABD4");
        });

        modelBuilder.Entity<Insurance>(entity =>
        {
            entity.HasKey(e => e.InsuranceId).HasName("PK__Insuranc__58B60F45B3ECAB4F");

            entity.ToTable("Insurance");

            entity.Property(e => e.InsuranceId).HasColumnName("insurance_id");
            entity.Property(e => e.ContributionRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("contribution_rate");
            entity.Property(e => e.Coverage)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("coverage");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<InternshipContract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Internsh__F8D66423EBC568BD");

            entity.ToTable("InternshipContract");

            entity.Property(e => e.ContractId)
                .ValueGeneratedNever()
                .HasColumnName("contract_id");
            entity.Property(e => e.Evaluation)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("evaluation");
            entity.Property(e => e.Mentoring)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("mentoring");
            entity.Property(e => e.StipendRelated)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("stipend_related");

            entity.HasOne(d => d.Contract).WithOne(p => p.InternshipContract)
                .HasForeignKey<InternshipContract>(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Internshi__contr__76969D2E");
        });

        modelBuilder.Entity<LatenessPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Lateness__47DA3F03EAA6CF3A");

            entity.ToTable("LatenessPolicy");

            entity.Property(e => e.PolicyId)
                .ValueGeneratedNever()
                .HasColumnName("policy_id");
            entity.Property(e => e.DeductionRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("deduction_rate");
            entity.Property(e => e.GracePeriodMins).HasColumnName("grace_period_mins");

            entity.HasOne(d => d.Policy).WithOne(p => p.LatenessPolicy)
                .HasForeignKey<LatenessPolicy>(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LatenessP__polic__56E8E7AB");
        });

        modelBuilder.Entity<Leave>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__Leave__743350BC3E2C7893");

            entity.ToTable("Leave");

            entity.Property(e => e.LeaveId).HasColumnName("leave_id");
            entity.Property(e => e.LeaveDescription)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("leave_description");
            entity.Property(e => e.LeaveType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("leave_type");
        });

        modelBuilder.Entity<LeaveDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__LeaveDoc__9666E8AC16058918");

            entity.ToTable("LeaveDocument");

            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.FilePath)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("file_path");
            entity.Property(e => e.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(e => e.UploadedAt)
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.LeaveRequest).WithMany(p => p.LeaveDocuments)
                .HasForeignKey(d => d.LeaveRequestId)
                .HasConstraintName("FK__LeaveDocu__leave__1BC821DD");
        });

        modelBuilder.Entity<LeaveEntitlement>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.LeaveTypeId }).HasName("PK__LeaveEnt__92097AE52198E440");

            entity.ToTable("LeaveEntitlement");

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(e => e.Entitlement)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("entitlement");

            entity.HasOne(d => d.Employee).WithMany(p => p.LeaveEntitlements)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LeaveEnti__emplo__17F790F9");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveEntitlements)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LeaveEnti__leave__18EBB532");
        });

        modelBuilder.Entity<LeavePolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__LeavePol__47DA3F0328D1A173");

            entity.ToTable("LeavePolicy");

            entity.Property(e => e.PolicyId).HasColumnName("policy_id");
            entity.Property(e => e.EligibilityRules)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("eligibility_rules");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.NoticePeriod).HasColumnName("notice_period");
            entity.Property(e => e.Purpose)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("purpose");
            entity.Property(e => e.ResetOnNewYear).HasColumnName("reset_on_new_year");
            entity.Property(e => e.SpecialLeaveType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("special_leave_type");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__LeaveReq__18D3B90FCDB4C3E6");

            entity.ToTable("LeaveRequest");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.ApprovalTiming)
                .HasColumnType("datetime")
                .HasColumnName("approval_timing");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Justification)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("justification");
            entity.Property(e => e.LeaveId).HasColumnName("leave_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__LeaveRequ__emplo__14270015");

            entity.HasOne(d => d.Leave).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.LeaveId)
                .HasConstraintName("FK__LeaveRequ__leave__151B244E");
        });

        modelBuilder.Entity<LineManager>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__LineMana__C52E0BA80D8328A8");

            entity.ToTable("LineManager");

            entity.Property(e => e.EmployeeId)
                .ValueGeneratedNever()
                .HasColumnName("employee_id");
            entity.Property(e => e.ApprovalLimit)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("approval_limit");
            entity.Property(e => e.SupervisedDepartments)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("supervised_departments");
            entity.Property(e => e.TeamSize).HasColumnName("team_size");

            entity.HasOne(d => d.Employee).WithOne(p => p.LineManager)
                .HasForeignKey<LineManager>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LineManag__emplo__571DF1D5");
        });

        modelBuilder.Entity<ManagerNote>(entity =>
        {
            entity.HasKey(e => e.NoteId).HasName("PK__ManagerN__CEDD0FA4EBBBFB2C");

            entity.Property(e => e.NoteId).HasColumnName("note_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.NoteContent)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("note_content");

            entity.HasOne(d => d.Employee).WithMany(p => p.ManagerNoteEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__ManagerNo__emplo__7849DB76");

            entity.HasOne(d => d.Manager).WithMany(p => p.ManagerNoteManagers)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK__ManagerNo__manag__793DFFAF");
        });

        modelBuilder.Entity<Mission>(entity =>
        {
            entity.HasKey(e => e.MissionId).HasName("PK__Mission__B5419AB2CA112001");

            entity.ToTable("Mission");

            entity.Property(e => e.MissionId).HasColumnName("mission_id");
            entity.Property(e => e.Destination)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("destination");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.MissionEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Mission__employe__7E37BEF6");

            entity.HasOne(d => d.Manager).WithMany(p => p.MissionManagers)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK__Mission__manager__7F2BE32F");
        });

        modelBuilder.Entity<MonthlySalaryType>(entity =>
        {
            entity.HasKey(e => e.SalaryTypeId).HasName("PK__MonthlyS__4D64706211A126D6");

            entity.ToTable("MonthlySalaryType");

            entity.Property(e => e.SalaryTypeId)
                .ValueGeneratedNever()
                .HasColumnName("salary_type_id");
            entity.Property(e => e.ContributionScheme)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("contribution_scheme");
            entity.Property(e => e.TaxRule)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("tax_rule");

            entity.HasOne(d => d.SalaryType).WithOne(p => p.MonthlySalaryType)
                .HasForeignKey<MonthlySalaryType>(d => d.SalaryTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MonthlySa__salar__47A6A41B");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842F49F230F6");

            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.MessageContent)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("message_content");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("notification_type");
            entity.Property(e => e.ReadStatus).HasColumnName("read_status");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.Urgency)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("urgency");
        });

        modelBuilder.Entity<OvertimePolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Overtime__47DA3F03E1A9357A");

            entity.ToTable("OvertimePolicy");

            entity.Property(e => e.PolicyId)
                .ValueGeneratedNever()
                .HasColumnName("policy_id");
            entity.Property(e => e.MaxHoursPerMonth).HasColumnName("max_hours_per_month");
            entity.Property(e => e.WeekdayRateMultiplier)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("weekday_rate_multiplier");
            entity.Property(e => e.WeekendRateMultiplier)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("weekend_rate_multiplier");

            entity.HasOne(d => d.Policy).WithOne(p => p.OvertimePolicy)
                .HasForeignKey<OvertimePolicy>(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OvertimeP__polic__540C7B00");
        });

        modelBuilder.Entity<PartTimeContract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__PartTime__F8D664239AF62C63");

            entity.ToTable("PartTimeContract");

            entity.Property(e => e.ContractId)
                .ValueGeneratedNever()
                .HasColumnName("contract_id");
            entity.Property(e => e.HourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("hourly_rate");
            entity.Property(e => e.WorkingHours).HasColumnName("working_hours");

            entity.HasOne(d => d.Contract).WithOne(p => p.PartTimeContract)
                .HasForeignKey<PartTimeContract>(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PartTimeC__contr__70DDC3D8");
        });

        modelBuilder.Entity<PayGrade>(entity =>
        {
            entity.HasKey(e => e.PayGradeId).HasName("PK__PayGrade__C8AD0DED644D7036");

            entity.ToTable("PayGrade");

            entity.Property(e => e.PayGradeId).HasColumnName("pay_grade_id");
            entity.Property(e => e.GradeName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("grade_name");
            entity.Property(e => e.MaxSalary)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_salary");
            entity.Property(e => e.MinSalary)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("min_salary");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.PayrollId).HasName("PK__Payroll__D99FC944C3F906B5");

            entity.ToTable("Payroll");

            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.ActualPay)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("actual_pay");
            entity.Property(e => e.Adjustments)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("adjustments");
            entity.Property(e => e.BaseAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("base_amount");
            entity.Property(e => e.Contributions)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("contributions");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.NetSalary)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("net_salary");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end");
            entity.Property(e => e.PeriodStart).HasColumnName("period_start");
            entity.Property(e => e.Taxes)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("taxes");

            entity.HasOne(d => d.Employee).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Payroll__employe__41EDCAC5");

            entity.HasMany(d => d.Policies).WithMany(p => p.Payrolls)
                .UsingEntity<Dictionary<string, object>>(
                    "PayrollPolicyId",
                    r => r.HasOne<PayrollPolicy>().WithMany()
                        .HasForeignKey("PolicyId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PayrollPo__polic__607251E5"),
                    l => l.HasOne<Payroll>().WithMany()
                        .HasForeignKey("PayrollId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PayrollPo__payro__5F7E2DAC"),
                    j =>
                    {
                        j.HasKey("PayrollId", "PolicyId").HasName("PK__PayrollP__FDE26AB4B7B0ABEF");
                        j.ToTable("PayrollPolicy_ID");
                        j.IndexerProperty<int>("PayrollId").HasColumnName("payroll_id");
                        j.IndexerProperty<int>("PolicyId").HasColumnName("policy_id");
                    });
        });

        modelBuilder.Entity<PayrollLog>(entity =>
        {
            entity.HasKey(e => e.PayrollLogId).HasName("PK__Payroll___7B69DA7A4AACAA33");

            entity.ToTable("Payroll_Log");

            entity.Property(e => e.PayrollLogId).HasColumnName("payroll_log_id");
            entity.Property(e => e.Actor).HasColumnName("actor");
            entity.Property(e => e.ChangeDate)
                .HasColumnType("datetime")
                .HasColumnName("change_date");
            entity.Property(e => e.ModificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("modification_type");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollLogs)
                .HasForeignKey(d => d.PayrollId)
                .HasConstraintName("FK__Payroll_L__payro__634EBE90");
        });

        modelBuilder.Entity<PayrollPeriod>(entity =>
        {
            entity.HasKey(e => e.PayrollPeriodId).HasName("PK__PayrollP__CD8483A2D0ADFFF1");

            entity.ToTable("PayrollPeriod");

            entity.Property(e => e.PayrollPeriodId).HasColumnName("payroll_period_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollPeriods)
                .HasForeignKey(d => d.PayrollId)
                .HasConstraintName("FK__PayrollPe__payro__662B2B3B");
        });

        modelBuilder.Entity<PayrollPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__PayrollP__47DA3F0356F8790E");

            entity.ToTable("PayrollPolicy");

            entity.Property(e => e.PolicyId).HasColumnName("policy_id");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.EffectiveDate).HasColumnName("effective_date");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<PayrollSpecialist>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__PayrollS__C52E0BA80F1AD0C5");

            entity.ToTable("PayrollSpecialist");

            entity.Property(e => e.EmployeeId)
                .ValueGeneratedNever()
                .HasColumnName("employee_id");
            entity.Property(e => e.AssignedRegion)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("assigned_region");
            entity.Property(e => e.LastProcessedPeriod).HasColumnName("last_processed_period");
            entity.Property(e => e.ProcessingFrequency)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("processing_frequency");

            entity.HasOne(d => d.Employee).WithOne(p => p.PayrollSpecialist)
                .HasForeignKey<PayrollSpecialist>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PayrollSp__emplo__5441852A");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__99A0E7A4D9B8F6C4");

            entity.ToTable("Position");

            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.PositionTitle)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("position_title");
            entity.Property(e => e.Responsibilities)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("responsibilities");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
        });

        modelBuilder.Entity<ProbationLeave>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__Probatio__743350BC635A092B");

            entity.ToTable("ProbationLeave");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("leave_id");
            entity.Property(e => e.EligibilityStartDate).HasColumnName("eligibility_start_date");
            entity.Property(e => e.ProbationPeriod).HasColumnName("probation_period");

            entity.HasOne(d => d.Leave).WithOne(p => p.ProbationLeave)
                .HasForeignKey<ProbationLeave>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Probation__leave__0C85DE4D");
        });

        modelBuilder.Entity<Reimbursement>(entity =>
        {
            entity.HasKey(e => e.ReimbursementId).HasName("PK__Reimburs__F6C26984A846D772");

            entity.ToTable("Reimbursement");

            entity.Property(e => e.ReimbursementId).HasColumnName("reimbursement_id");
            entity.Property(e => e.ApprovalDate).HasColumnName("approval_date");
            entity.Property(e => e.ClaimType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("claim_type");
            entity.Property(e => e.CurrentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("current_status");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.Employee).WithMany(p => p.Reimbursements)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Reimburse__emplo__02084FDA");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__760965CCD4717655");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Purpose)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("purpose");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionName }).HasName("PK__RolePerm__5E156A9685378612");

            entity.ToTable("RolePermission");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.PermissionName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("permission_name");
            entity.Property(e => e.AllowedAction)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("allowed_action");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RolePermi__role___5FB337D6");
        });

        modelBuilder.Entity<SalaryType>(entity =>
        {
            entity.HasKey(e => e.SalaryTypeId).HasName("PK__SalaryTy__4D64706225C34AE1");

            entity.ToTable("SalaryType");

            entity.Property(e => e.SalaryTypeId).HasColumnName("salary_type_id");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("currency");
            entity.Property(e => e.PaymentFrequency)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_frequency");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.CurrencyNavigation).WithMany(p => p.SalaryTypes)
                .HasForeignKey(d => d.Currency)
                .HasConstraintName("FK__SalaryTyp__curre__403A8C7D");
        });

        modelBuilder.Entity<ShiftAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK__ShiftAss__DA8918143BC4D543");

            entity.ToTable("ShiftAssignment");

            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.ShiftAssignments)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__ShiftAssi__emplo__208CD6FA");

            entity.HasOne(d => d.Shift).WithMany(p => p.ShiftAssignments)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("FK__ShiftAssi__shift__2180FB33");
        });

        modelBuilder.Entity<ShiftCycle>(entity =>
        {
            entity.HasKey(e => e.CycleId).HasName("PK__ShiftCyc__5D9558818E43F791");

            entity.ToTable("ShiftCycle");

            entity.Property(e => e.CycleId).HasColumnName("cycle_id");
            entity.Property(e => e.CycleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("cycle_name");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("description");
        });

        modelBuilder.Entity<ShiftCycleAssignment>(entity =>
        {
            entity.HasKey(e => new { e.CycleId, e.ShiftId }).HasName("PK__ShiftCyc__4A273FA3F665B448");

            entity.ToTable("ShiftCycleAssignment");

            entity.Property(e => e.CycleId).HasColumnName("cycle_id");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number");

            entity.HasOne(d => d.Cycle).WithMany(p => p.ShiftCycleAssignments)
                .HasForeignKey(d => d.CycleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShiftCycl__cycle__3E1D39E1");

            entity.HasOne(d => d.Shift).WithMany(p => p.ShiftCycleAssignments)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShiftCycl__shift__3F115E1A");
        });

        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__ShiftSch__7B26722024B811EC");

            entity.ToTable("ShiftSchedule");

            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.BreakDuration).HasColumnName("break_duration");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.ShiftDate).HasColumnName("shift_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<SickLeave>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__SickLeav__743350BCAAE236F8");

            entity.ToTable("SickLeave");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("leave_id");
            entity.Property(e => e.MedicalCertRequired).HasColumnName("medical_cert_required");
            entity.Property(e => e.PhysicianId).HasColumnName("physician_id");

            entity.HasOne(d => d.Leave).WithOne(p => p.SickLeave)
                .HasForeignKey<SickLeave>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SickLeave__leave__09A971A2");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("PK__Skill__FBBA8379274B8D59");

            entity.ToTable("Skill");

            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("skill_name");
        });

        modelBuilder.Entity<SystemAdministrator>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__SystemAd__C52E0BA8117326CD");

            entity.ToTable("SystemAdministrator");

            entity.Property(e => e.EmployeeId)
                .ValueGeneratedNever()
                .HasColumnName("employee_id");
            entity.Property(e => e.AuditVisibilityScope)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("audit_visibility_scope");
            entity.Property(e => e.ConfigurableFields)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("configurable_fields");
            entity.Property(e => e.SystemPrivilegeLevel).HasColumnName("system_privilege_level");

            entity.HasOne(d => d.Employee).WithOne(p => p.SystemAdministrator)
                .HasForeignKey<SystemAdministrator>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SystemAdm__emplo__5165187F");
        });

        modelBuilder.Entity<TaxForm>(entity =>
        {
            entity.HasKey(e => e.TaxFormId).HasName("PK__TaxForm__3184195AC65F019D");

            entity.ToTable("TaxForm");

            entity.Property(e => e.TaxFormId).HasColumnName("tax_form_id");
            entity.Property(e => e.FormContent)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("form_content");
            entity.Property(e => e.Jurisdiction)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("jurisdiction");
            entity.Property(e => e.ValidityPeriod).HasColumnName("validity_period");
        });

        modelBuilder.Entity<Termination>(entity =>
        {
            entity.HasKey(e => e.TerminationId).HasName("PK__Terminat__B66BAA11F9827CF8");

            entity.ToTable("Termination");

            entity.Property(e => e.TerminationId).HasColumnName("termination_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("reason");

            entity.HasOne(d => d.Contract).WithMany(p => p.Terminations)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("FK__Terminati__contr__7B5B524B");
        });

        modelBuilder.Entity<VacationLeave>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK__Vacation__743350BC3119E261");

            entity.ToTable("VacationLeave");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("leave_id");
            entity.Property(e => e.ApprovingManager).HasColumnName("approving_manager");
            entity.Property(e => e.CarryOverDays).HasColumnName("carry_over_days");

            entity.HasOne(d => d.Leave).WithOne(p => p.VacationLeave)
                .HasForeignKey<VacationLeave>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VacationL__leave__06CD04F7");
        });

        modelBuilder.Entity<Verification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__Verifica__24F179694447BABB");

            entity.ToTable("Verification");

            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.ExpiryPeriod).HasColumnName("expiry_period");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");
            entity.Property(e => e.Issuer)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("issuer");
            entity.Property(e => e.VerificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("verification_type");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}