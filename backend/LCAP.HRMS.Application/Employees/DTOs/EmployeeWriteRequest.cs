using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Domain.Enums;
namespace LCAP.HRMS.Application.Employees.DTOs;
public abstract class EmployeeWriteRequest : IValidatableObject
{
    
    public Guid CompanyId { get; set; }
    
    public Guid BranchId { get; set; }
    
    public Guid DepartmentId { get; set; }
    
    public Guid DesignationId { get; set; }
    
    public Guid ShiftId { get; set; }
    
    public Guid WorkLocationId { get; set; }
    
    public Guid? ReportingManagerId { get; set; }
    [Required] [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;
    [Required] [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? MiddleName { get; set; }
    [MaxLength(100)]
    public string? LastName { get; set; }
    [Required] [MaxLength(25)] [RegularExpression(@"[+]?[0-9][0-9 ()-]{6,23}[0-9]", ErrorMessage = "Invalid MobileNumber format.")]
    public string MobileNumber { get; set; } = string.Empty;
    [MaxLength(25)] [RegularExpression(@"[+]?[0-9][0-9 ()-]{6,23}[0-9]", ErrorMessage = "Invalid AlternateMobileNumber format.")]
    public string? AlternateMobileNumber { get; set; }
    [MaxLength(254)] [EmailAddress]
    public string? PersonalEmail { get; set; }
    [MaxLength(254)] [EmailAddress]
    public string? OfficialEmail { get; set; }
    
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(50)]
    public string? Gender { get; set; }
    [MaxLength(10)]
    public string? BloodGroup { get; set; }
    [Required]
    public DateOnly? DateOfJoining { get; set; }
    
    public DateOnly? DateOfConfirmation { get; set; }
    [Required]
    public EmploymentType? EmploymentType { get; set; }
    [Required]
    public EmployeeStatus? EmployeeStatus { get; set; }
    
    public DateOnly? ProbationEndDate { get; set; }
    
    public DateOnly? DateOfResignation { get; set; }
    
    public DateOnly? LastWorkingDate { get; set; }
    [MaxLength(1000)]
    public string? ExitReason { get; set; }
    [MaxLength(10)] [RegularExpression(@"[A-Z]{5}[0-9]{4}[A-Z]", ErrorMessage = "Invalid PAN format.")]
    public string? PAN { get; set; }
    [MaxLength(12)] [RegularExpression(@"[0-9]{12}", ErrorMessage = "Invalid AadhaarNumber format.")]
    public string? AadhaarNumber { get; set; }
    [MaxLength(20)] [RegularExpression(@"[0-9]{12}", ErrorMessage = "Invalid UAN format.")]
    public string? UAN { get; set; }
    [MaxLength(20)] [RegularExpression(@"[0-9]{10,17}", ErrorMessage = "Invalid ESICNumber format.")]
    public string? ESICNumber { get; set; }
    [MaxLength(200)]
    public string? BankName { get; set; }
    [MaxLength(200)]
    public string? AccountHolderName { get; set; }
    [MaxLength(34)] [RegularExpression(@"[A-Za-z0-9]{6,34}", ErrorMessage = "Invalid BankAccountNumber format.")]
    public string? BankAccountNumber { get; set; }
    [MaxLength(11)] [RegularExpression(@"[A-Z]{4}0[A-Z0-9]{6}", ErrorMessage = "Invalid IFSCCode format.")]
    public string? IFSCCode { get; set; }
    [MaxLength(2048)] [Url]
    public string? ProfilePhotoUrl { get; set; }
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (new[] {CompanyId,BranchId,DepartmentId,DesignationId,ShiftId,WorkLocationId}.Any(id => id == Guid.Empty)) yield return new("Organisation assignments are required.");
        if (ReportingManagerId == Guid.Empty) yield return new("Reporting manager ID is invalid.");
        if (EmployeeStatus is {} status && (!Enum.IsDefined(status) || status == Domain.Enums.EmployeeStatus.Unknown)) yield return new("Invalid employee status.");
        if (EmploymentType is {} type && (!Enum.IsDefined(type) || type == Domain.Enums.EmploymentType.Unknown)) yield return new("Invalid employment type.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (DateOfBirth > today) yield return new("Date of birth cannot be in the future.");
        if (DateOfJoining is {} joining)
        {
            if (joining < new DateOnly(1900,1,1) || joining > today.AddYears(1)) yield return new("Joining date must be between 1900 and one year from today.");
            if (DateOfBirth >= joining) yield return new("Date of birth must precede joining date.");
            if (DateOfConfirmation < joining || LastWorkingDate < joining || ProbationEndDate < joining || DateOfResignation < joining) yield return new("Employment dates cannot precede joining date.");
            if (LastWorkingDate < DateOfResignation) yield return new("Last working date cannot precede resignation.");
        }
    }
}
public sealed class EmployeeCreateRequest : EmployeeWriteRequest;
public sealed class EmployeeUpdateRequest : EmployeeWriteRequest;
