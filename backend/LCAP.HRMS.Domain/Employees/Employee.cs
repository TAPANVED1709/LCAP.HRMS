using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Enums;
namespace LCAP.HRMS.Domain.Employees;
public sealed class Employee : BaseEntity
{
    
    public Guid CompanyId { get; set; }
    
    public Guid BranchId { get; set; }
    
    public Guid DepartmentId { get; set; }
    
    public Guid DesignationId { get; set; }
    
    public Guid ShiftId { get; set; }
    
    public Guid WorkLocationId { get; set; }
    
    public Guid? ReportingManagerId { get; set; }
    
    public string EmployeeCode { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string? MiddleName { get; set; }
    
    public string? LastName { get; set; }
    
    public string MobileNumber { get; set; } = string.Empty;
    
    public string? AlternateMobileNumber { get; set; }
    
    public string? PersonalEmail { get; set; }
    
    public string? OfficialEmail { get; set; }
    
    public DateOnly? DateOfBirth { get; set; }
    
    public string? Gender { get; set; }
    
    public string? BloodGroup { get; set; }
    
    public DateOnly DateOfJoining { get; set; }
    
    public DateOnly? DateOfConfirmation { get; set; }
    
    public EmploymentType EmploymentType { get; set; }
    
    public EmployeeStatus EmployeeStatus { get; set; }
    
    public DateOnly? ProbationEndDate { get; set; }
    
    public DateOnly? DateOfResignation { get; set; }
    
    public DateOnly? LastWorkingDate { get; set; }
    
    public string? ExitReason { get; set; }
    
    public string? PAN { get; set; }
    
    public string? AadhaarNumber { get; set; }
    
    public string? UAN { get; set; }
    
    public string? ESICNumber { get; set; }
    
    public string? BankName { get; set; }
    
    public string? AccountHolderName { get; set; }
    
    public string? BankAccountNumber { get; set; }
    
    public string? IFSCCode { get; set; }
    
    public string? ProfilePhotoUrl { get; set; }
    
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public Companies.Company Company { get; set; } = null!;
    public Branches.Branch Branch { get; set; } = null!;
    public Departments.Department Department { get; set; } = null!;
    public Designations.Designation Designation { get; set; } = null!;
    public Shifts.Shift Shift { get; set; } = null!;
    public WorkLocations.WorkLocation WorkLocation { get; set; } = null!;
    public Employee? ReportingManager { get; set; }
}
