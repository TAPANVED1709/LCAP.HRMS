using System.ComponentModel.DataAnnotations;

namespace LCAP.HRMS.Application.Departments.DTOs;

public abstract class DepartmentWriteRequest : IValidatableObject
{
    [Required]
    public Guid CompanyId { get; set; } = Guid.Empty;

    [Required]
    [StringLength(50)]
    public string DepartmentCode { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string DepartmentName { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; } = null;

    public Guid? ParentDepartmentId { get; set; } = null;

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompanyId == Guid.Empty)
            yield return new ValidationResult("CompanyId must be a non-empty company ID.", [nameof(CompanyId)]);
        if (ParentDepartmentId == Guid.Empty)
            yield return new ValidationResult("ParentDepartmentId must be null or a non-empty department ID.", [nameof(ParentDepartmentId)]);
    }
}
