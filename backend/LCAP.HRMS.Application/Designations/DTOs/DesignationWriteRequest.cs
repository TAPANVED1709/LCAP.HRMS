using System.ComponentModel.DataAnnotations;

namespace LCAP.HRMS.Application.Designations.DTOs;

public abstract class DesignationWriteRequest : IValidatableObject
{
    [Required]
    public Guid CompanyId { get; set; } = Guid.Empty;

    [Required]
    [StringLength(50)]
    public string DesignationCode { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string DesignationName { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; } = null;

    [StringLength(50)]
    public string? Grade { get; set; } = null;

    public int? Level { get; set; } = null;

    public bool IsManagerial { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompanyId == Guid.Empty)
            yield return new ValidationResult("CompanyId must be a non-empty company ID.", [nameof(CompanyId)]);
    }
}
