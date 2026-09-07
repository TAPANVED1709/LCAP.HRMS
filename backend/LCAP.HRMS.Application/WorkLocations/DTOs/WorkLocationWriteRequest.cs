using System.ComponentModel.DataAnnotations;

namespace LCAP.HRMS.Application.WorkLocations.DTOs;

public abstract class WorkLocationWriteRequest : IValidatableObject
{
    [Required]
    public Guid CompanyId { get; set; } = Guid.Empty;

    [Required]
    public Guid BranchId { get; set; } = Guid.Empty;

    [Required]
    [StringLength(50)]
    public string LocationCode { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string LocationName { get; set; } = "";

    [StringLength(1000)]
    public string? Address { get; set; } = null;

    [StringLength(100)]
    public string? City { get; set; } = null;

    [StringLength(100)]
    public string? State { get; set; } = null;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = "India";

    [StringLength(20)]
    public string? PinCode { get; set; } = null;

    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; } = null;

    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; } = null;

    [Range(1, int.MaxValue)]
    public int AllowedRadiusMeters { get; set; } = 100;

    public bool IsGeoFenceEnabled { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompanyId == Guid.Empty)
            yield return new ValidationResult("CompanyId must be a non-empty company ID.", [nameof(CompanyId)]);
        if (BranchId == Guid.Empty)
            yield return new ValidationResult("BranchId must be a non-empty branch ID.", [nameof(BranchId)]);
        if (Latitude.HasValue != Longitude.HasValue)
            yield return new ValidationResult("Provide both coordinates or leave both null.", [nameof(Latitude), nameof(Longitude)]);
        if (Latitude is decimal lat && decimal.Round(lat, 7) != lat)
            yield return new ValidationResult("Latitude supports at most seven decimal places.", [nameof(Latitude)]);
        if (Longitude is decimal lon && decimal.Round(lon, 7) != lon)
            yield return new ValidationResult("Longitude supports at most seven decimal places.", [nameof(Longitude)]);
    }
}
