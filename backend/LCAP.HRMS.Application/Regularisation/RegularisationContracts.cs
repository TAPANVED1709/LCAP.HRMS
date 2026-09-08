using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using LCAP.HRMS.Domain.Regularisation;
using LCAP.HRMS.Domain.AttendancePolicies;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Application.AttendancePolicies;

namespace LCAP.HRMS.Application.Regularisation;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegularisationCreateRequest
{
    [Required]
    public DateOnly? AttendanceDate { get; set; }

    [EnumDataType(typeof(RegularisationType))]
    public RegularisationType RequestType { get; set; }
    public DateTimeOffset? RequestedCheckInTime { get; set; }
    public DateTimeOffset? RequestedCheckOutTime { get; set; }

    [Required, MaxLength(2000)]
    public string EmployeeReason { get; set; } = "";

    [MaxLength(2000)]
    public string? SupportingNote { get; set; }

    [MaxLength(2048), Url]
    public string? AttachmentUrl { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReviewRequest
{
    [MaxLength(2000)]
    public string? ReviewerRemarks { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CancelRequest
{
    [Required, MaxLength(2000)]
    public string CancellationReason { get; set; } = "";
}

public sealed record RegularisationQuery(RegularisationStatus? Status = null, Guid? EmployeeId = null, Guid? ManagerId = null, Guid? BranchId = null, DateOnly? Date = null, DateOnly? FromDate = null, DateOnly? ToDate = null, int Skip = 0, int Take = 50, Guid? CompanyId = null);
public sealed class RegularisationResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public Guid ReportingManagerId { get; set; }
    public string ManagerName { get; set; } = "";
    public DateOnly AttendanceDate { get; set; }
    public string TimeZoneId { get; set; } = "";
    public RegularisationType RequestType { get; set; }
    public RegularisationStatus Status { get; set; }
    public DateTimeOffset? OriginalCheckInTime { get; set; }
    public DateTimeOffset? OriginalCheckOutTime { get; set; }
    public DateTimeOffset? RequestedCheckInTime { get; set; }
    public DateTimeOffset? RequestedCheckOutTime { get; set; }
    public DateTimeOffset? BaseEffectiveCheckInTime { get; set; }
    public DateTimeOffset? BaseEffectiveCheckOutTime { get; set; }
    public string EmployeeReason { get; set; } = "";
    public string? SupportingNote { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public string? ReviewerRemarks { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool PolicyImpactReviewRequired { get; set; }
}

public sealed record RegularisationEmployee(Guid Id, Guid CompanyId, Guid BranchId, Guid ShiftId, Guid? ReportingManagerId, bool Eligible, TimeOnly Start, TimeOnly End, string ShiftName);
public sealed record EffectiveAttendance(Guid EmployeeId, DateOnly AttendanceDate, string TimeZoneId, Guid? AttendanceRecordId, DateTimeOffset? OriginalCheckInTime, DateTimeOffset? OriginalCheckOutTime, DateTimeOffset? EffectiveCheckInTime, DateTimeOffset? EffectiveCheckOutTime, AttendanceSource Source, Guid? RegularisationRequestId, Guid? CorrectionId, EvaluationResponse? Evaluation, bool PolicyImpactReviewRequired);
public sealed record ReevaluationDay(DateOnly Date, Guid? RawId, DateTimeOffset? CheckIn, DateTimeOffset? CheckOut, TimeOnly ShiftStart, string ShiftName, string TimeZone, AttendanceEvaluation? Original, AttendanceEvaluationRevision? Latest, Guid? PenaltyId);
public interface IRegularisationRepository
{
    Task<RegularisationEmployee?> EmployeeAsync(Guid id, CancellationToken ct);
    Task<AttendanceRecord?> RawAsync(Guid employee, DateOnly date, CancellationToken ct);
    Task<AttendanceCorrection?> CorrectionAsync(Guid employee, DateOnly date, CancellationToken ct);
    Task<AttendanceRegularisationRequest?> GetAsync(Guid id, CancellationToken ct);
    Task<RegularisationResponse?> ResponseAsync(Guid id, CancellationToken ct);
    Task<bool> ActiveAsync(Guid employee, DateOnly date, RegularisationType type, CancellationToken ct);
    Task<IReadOnlyList<RegularisationResponse>> ListAsync(RegularisationQuery query, CancellationToken ct);
    Task<IReadOnlyList<RegularisationAuditEntry>> AuditAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ReevaluationDay>> TimelineAsync(Guid employee, CancellationToken ct);
    Task<AttendanceEvaluationRevision?> RevisionAsync(Guid employee, DateOnly date, CancellationToken ct);
    Task AddAsync(object entity, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task<T> WriteAsync<T>(Guid employee, Func<Task<T>> action, CancellationToken ct);
}

public interface IRegularisationService
{
    Task<RegularisationResponse> SubmitAsync(RegularisationCreateRequest request, CancellationToken ct);
    Task<IReadOnlyList<RegularisationResponse>> ListAsync(string scope, RegularisationQuery query, CancellationToken ct);
    Task<RegularisationResponse> GetAsync(Guid id, bool ownOnly, CancellationToken ct);
    Task<RegularisationResponse> ReviewAsync(Guid id, bool approve, ReviewRequest request, CancellationToken ct);
    Task<RegularisationResponse> CancelAsync(Guid id, CancelRequest request, CancellationToken ct);
    Task<IReadOnlyList<RegularisationAuditEntry>> AuditAsync(Guid id, CancellationToken ct);
}

public interface IAttendanceEffectiveStateService
{
    Task<EffectiveAttendance> GetAsync(Guid employee, DateOnly date, CancellationToken ct);
}

public static class EvaluationSnapshots
{
    public static AttendanceEvaluation Read(AttendanceEvaluationRevision revision) => JsonSerializer.Deserialize<AttendanceEvaluation>(revision.ResultJson)!;
    public static EvaluationResponse Response(AttendanceEvaluation e) => new()
    {
        Id = e.Id,
        EmployeeId = e.EmployeeId,
        AttendanceRecordId = e.AttendanceRecordId,
        AttendanceDate = e.AttendanceDate,
        TimeZoneId = e.TimeZoneId,
        ShiftName = e.ShiftName,
        CheckInTime = e.ActualCheckInTime,
        AttendancePolicyId = e.AttendancePolicyId,
        PolicyCode = e.PolicyCode,
        PolicyRevision = e.PolicyRevision,
        EvaluationVersion = e.EvaluationVersion,
        IsEvaluated = e.IsEvaluated,
        IsLate = e.IsLate,
        LateMinutes = e.LateMinutes,
        GracePeriodMinutes = e.GracePeriodMinutes,
        ConsecutiveLateCount = e.ConsecutiveLateCount,
        SequenceAfterEvaluation = e.SequenceAfterEvaluation,
        Threshold = e.ConsecutiveLateThreshold,
        ThresholdReached = e.ThresholdReached,
        PenaltyTriggered = e.PenaltyTriggered,
        NextLateTriggersPenalty = LateRuleEngine.NextTriggers(e),
        ReasonCode = e.ReasonCode,
        EvaluatedAt = e.EvaluatedAt
    };
}
