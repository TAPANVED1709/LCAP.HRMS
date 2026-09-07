using System.ComponentModel.DataAnnotations;

namespace LCAP.HRMS.Application.Branches.DTOs;

public abstract class BranchWriteRequest : IValidatableObject
{
    [Required]
    public Guid CompanyId { get; set; } = Guid.Empty;

    [Required]
    [StringLength(50)]
    public string BranchCode { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string BranchName { get; set; } = "";

    [StringLength(500)]
    public string? AddressLine1 { get; set; } = null;

    [StringLength(500)]
    public string? AddressLine2 { get; set; } = null;

    [StringLength(100)]
    public string? City { get; set; } = null;

    [StringLength(100)]
    public string? State { get; set; } = null;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = "India";

    [StringLength(20)]
    public string? PinCode { get; set; } = null;

    [StringLength(254)]
    [EmailAddress]
    public string? Email { get; set; } = null;

    [StringLength(30)]
    [Phone]
    public string? Phone { get; set; } = null;

    public bool IsHeadOffice { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompanyId == Guid.Empty)
            yield return new ValidationResult("CompanyId must be a non-empty company ID.", [nameof(CompanyId)]);
    }
}
