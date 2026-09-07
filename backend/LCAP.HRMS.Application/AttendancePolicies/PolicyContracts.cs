using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Domain.AttendancePolicies;
namespace LCAP.HRMS.Application.AttendancePolicies;

public sealed class AttendancePolicyRequest : IValidatableObject
{
    public Guid CompanyId { get; set; }
    [Required, MaxLength(30)] public string PolicyCode { get; set; } = "";
    [Required, MaxLength(200)] public string PolicyName { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    [Range(0, 1440)] public int GracePeriodMinutes { get; set; }
    public bool LateRuleEnabled { get; set; } = true;
    [Range(0, 10000)] public int ConsecutiveLateThreshold { get; set; } = 3;
    [EnumDataType(typeof(PenaltyTriggerMode))] public PenaltyTriggerMode PenaltyTriggerMode { get; set; } = PenaltyTriggerMode.NextQualifyingLate;
    [EnumDataType(typeof(PenaltyType))] public PenaltyType PenaltyType { get; set; } = PenaltyType.HalfDay;
    [Range(typeof(decimal), "0.001", "999999999")] public decimal? PenaltyValue { get; set; }
    [EnumDataType(typeof(ResetMode))] public ResetMode ResetMode { get; set; } = ResetMode.AfterPenalty;
    [Required] public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        if (CompanyId == Guid.Empty) yield return new("Company is required.");
        if (LateRuleEnabled && ConsecutiveLateThreshold < 1) yield return new("Late threshold must be positive.");
        if (EffectiveFrom is { } start && start < new DateOnly(1900, 1, 1)) yield return new("Effective date must be on or after 1900-01-01.");
        if (EffectiveTo < EffectiveFrom) yield return new("EffectiveTo cannot precede EffectiveFrom.");
        if (PenaltyType is PenaltyType.HalfDay or PenaltyType.FullDay && PenaltyValue is not null) yield return new("Day-based events do not take a monetary value.");
        if (PenaltyType is PenaltyType.FixedAmount or PenaltyType.Hourly && PenaltyValue is null) yield return new("A configured event value is required for this type; no money is calculated.");
    }
}
public sealed record PolicyPage(Guid? CompanyId = null, int Skip = 0, int Take = 100);
public interface IAttendancePolicyService
{
    Task<IReadOnlyList<AttendancePolicy>> ListAsync(PolicyPage q, CancellationToken ct);
    Task<AttendancePolicy> GetAsync(Guid id, CancellationToken ct);
    Task<AttendancePolicy> SaveAsync(AttendancePolicyRequest request, Guid? id, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<AttendancePolicy?> CurrentAsync(Guid companyId, DateOnly date, CancellationToken ct);
}
public interface IAttendancePolicyResolver
{
    Task<AttendancePolicy?> ResolveAsync(Guid employeeId, Guid companyId, DateOnly attendanceDate, CancellationToken ct);
}
public interface IAttendancePolicyRepository
{
    Task<AttendancePolicy?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AttendancePolicy>> ListAsync(PolicyPage q, CancellationToken ct);
    Task<AttendancePolicy?> EffectiveAsync(Guid companyId, DateOnly date, CancellationToken ct);
    Task<bool> CompanyExistsAsync(Guid id, CancellationToken ct);
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? except, CancellationToken ct);
    Task<bool> OverlapAsync(AttendancePolicy policy, CancellationToken ct);
    Task AddAsync(AttendancePolicy policy, CancellationToken ct);
    Task<T> WriteAsync<T>(Guid companyId, Func<Task<T>> action, CancellationToken ct);
}

