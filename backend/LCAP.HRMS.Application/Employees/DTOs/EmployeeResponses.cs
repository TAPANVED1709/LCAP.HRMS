using LCAP.HRMS.Domain.Enums;
namespace LCAP.HRMS.Application.Employees.DTOs;
public class EmployeeListResponse
{
 public Guid Id { get; set; }
 public Guid CompanyId { get; set; }
 public Guid BranchId { get; set; }
 public Guid DepartmentId { get; set; }
 public string EmployeeCode { get; set; } = "";
 public string FullName { get; set; } = "";
 public string? OfficialEmail { get; set; }
 public string MobileNumber { get; set; } = "";
 public string Department { get; set; } = "";
 public string Designation { get; set; } = "";
 public string Branch { get; set; } = "";
 public string? ReportingManagerName { get; set; }
 public EmployeeStatus EmployeeStatus { get; set; }
 public DateOnly DateOfJoining { get; set; }
 public bool IsActive { get; set; }
}
public sealed class EmployeeDetailResponse
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
 public Guid Id { get; set; }
 public string FullName { get; set; } = "";
 public string Company { get; set; } = "";
 public string Branch { get; set; } = "";
 public string Department { get; set; } = "";
 public string Designation { get; set; } = "";
 public string Shift { get; set; } = "";
 public string WorkLocation { get; set; } = "";
 public string? ReportingManagerName { get; set; }
 public bool CanViewSensitive { get; set; }
 public DateTimeOffset CreatedAt { get; set; }
 public string? CreatedBy { get; set; }
 public DateTimeOffset? UpdatedAt { get; set; }
 public string? UpdatedBy { get; set; }
}
public sealed record EmployeeLookupResponse(Guid Id, Guid CompanyId, string EmployeeCode, string FullName);
