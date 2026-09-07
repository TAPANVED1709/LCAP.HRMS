using System.ComponentModel.DataAnnotations;

namespace LCAP.HRMS.Application.Shifts.DTOs;

public abstract class ShiftWriteRequest : IValidatableObject
{
    [Required]
    public Guid CompanyId { get; set; }
    [Required, StringLength(50)]
    public string ShiftCode { get; set; } = "";
    [Required, StringLength(200)]
    public string ShiftName { get; set; } = "";
    /// <summary>Local time in HH:mm:ss format; midnight is 00:00:00.</summary>
    [Required]
    public TimeOnly? StartTime { get; set; }
    /// <summary>Earlier than StartTime means the following day. Equal times are invalid.</summary>
    [Required]
    public TimeOnly? EndTime { get; set; }
    [Range(0, int.MaxValue)]
    public int GracePeriodMinutes { get; set; }
    [Range(0, int.MaxValue)]
    public int? MinimumHalfDayMinutes { get; set; }
    [Range(0, int.MaxValue)]
    public int? MinimumFullDayMinutes { get; set; }
    /// <summary>Overnight shifts automatically set this flag to true.</summary>
    public bool IsNightShift { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompanyId == Guid.Empty)
            yield return new ValidationResult("CompanyId must be a non-empty company ID.", [nameof(CompanyId)]);
        if (StartTime.HasValue && EndTime.HasValue && StartTime == EndTime)
            yield return new ValidationResult("StartTime and EndTime must differ.", [nameof(EndTime)]);
        if (MinimumHalfDayMinutes.HasValue && MinimumFullDayMinutes.HasValue
            && MinimumHalfDayMinutes > MinimumFullDayMinutes)
            yield return new ValidationResult("MinimumHalfDayMinutes cannot exceed MinimumFullDayMinutes.", [nameof(MinimumHalfDayMinutes)]);
    }
}
