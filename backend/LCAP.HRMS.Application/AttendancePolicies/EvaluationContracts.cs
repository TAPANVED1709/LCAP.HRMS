using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Domain.AttendancePolicies;

namespace LCAP.HRMS.Application.AttendancePolicies;
public sealed record EvaluationInput(AttendanceRecord Record, TimeOnly ShiftStart, string ShiftName);
public sealed record EvaluationQuery(Guid? CompanyId = null, Guid? EmployeeId = null, Guid? BranchId = null, DateOnly? FromDate = null, DateOnly? ToDate = null, bool LateOnly = false, bool PenaltyOnly = false, int Skip = 0, int Take = 100);
public sealed class EvaluationResponse
{
    public Guid Id { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string Branch { get; set; } = "";
    public string ShiftName { get; set; } = "";
    public DateOnly AttendanceDate { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public Guid? AttendancePolicyId { get; set; }
    public string? PolicyCode { get; set; }
    public int PolicyRevision { get; set; }
    public int EvaluationVersion { get; set; }
    public bool IsEvaluated { get; set; }
    public bool IsLate { get; set; }
    public int LateMinutes { get; set; }
    public int GracePeriodMinutes { get; set; }
    public int ConsecutiveLateCount { get; set; }
    public int SequenceAfterEvaluation { get; set; }
    public int Threshold { get; set; }
    public bool ThresholdReached { get; set; }
    public bool PenaltyTriggered { get; set; }
    public bool NextLateTriggersPenalty { get; set; }
    public string? ReasonCode { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
}

public sealed class PenaltyResponse
{
    public bool PolicyImpactReviewRequired { get; set; }
    public Guid CompanyId { get; set; }
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public Guid AttendanceEvaluationId { get; set; }
    public Guid AttendancePolicyId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateOnly PenaltyDate { get; set; }
    public int LateMinutes { get; set; }
    public int ConsecutiveLateCount { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public string ReasonCode { get; set; } = "";
    public PenaltyEventStatus Status { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed record LateSummary(Guid EmployeeId, int CurrentLateSequence, int Threshold, bool ThresholdReached, bool NextLateTriggersPenalty, PenaltyResponse? LatestPenaltyEvent);
public interface IAttendanceEvaluationService
{
    // Internal idempotent operation; not a manual editing endpoint.
    Task EvaluateAsync(Guid attendanceId, CancellationToken ct);
    Task<EvaluationResponse?> GetAsync(Guid attendanceId, CancellationToken ct);
    Task<IReadOnlyList<EvaluationResponse>> ListAsync(EvaluationQuery query, CancellationToken ct);
    Task<LateSummary> SummaryAsync(Guid employeeId, CancellationToken ct);
    Task<IReadOnlyList<PenaltyResponse>> PenaltiesAsync(EvaluationQuery query, CancellationToken ct);
    Task<PenaltyResponse> PenaltyAsync(Guid id, CancellationToken ct);
}

public interface IAttendanceEvaluationRepository
{
    Task<EvaluationInput?> InputAsync(Guid attendanceId, CancellationToken ct);
    Task<AttendanceEvaluation?> ExistingAsync(Guid attendanceId, CancellationToken ct);
    Task<AttendanceEvaluation?> PreviousAsync(Guid employeeId, DateOnly date, CancellationToken ct);
    Task<bool> LaterAsync(Guid employeeId, DateOnly date, CancellationToken ct);
    Task AddAsync(AttendanceEvaluation evaluation, AttendancePenaltyEvent? penalty, CancellationToken ct);
    Task<T> WriteAsync<T>(Guid employeeId, Func<Task<T>> action, CancellationToken ct);
    Task<EvaluationResponse?> ResponseAsync(Guid attendanceId, CancellationToken ct);
    Task<IReadOnlyList<EvaluationResponse>> ListAsync(EvaluationQuery query, CancellationToken ct);
    Task<IReadOnlyList<PenaltyResponse>> PenaltiesAsync(EvaluationQuery query, CancellationToken ct);
    Task<PenaltyResponse?> PenaltyAsync(Guid id, CancellationToken ct);
    Task<Guid?> EmployeeCompanyAsync(Guid employeeId, CancellationToken ct);
}
