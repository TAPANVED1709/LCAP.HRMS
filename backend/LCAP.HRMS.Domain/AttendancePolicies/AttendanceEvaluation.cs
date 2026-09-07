using LCAP.HRMS.Domain.Common;
namespace LCAP.HRMS.Domain.AttendancePolicies;

public sealed class AttendanceEvaluation : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public Guid? AttendancePolicyId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public TimeOnly ShiftStartTime { get; set; }
    public string ShiftName { get; set; } = "";
    public string TimeZoneId { get; set; } = "UTC";
    public int GracePeriodMinutes { get; set; }
    public DateTimeOffset? ActualCheckInTime { get; set; }
    public bool IsEvaluated { get; set; }
    public bool IsLate { get; set; }
    public int LateMinutes { get; set; }
    public int ConsecutiveLateCount { get; set; }
    public int SequenceAfterEvaluation { get; set; }
    public bool ThresholdReached { get; set; }
    public bool PenaltyTriggered { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
    public int EvaluationVersion { get; set; } = 1;
    public string? ReasonCode { get; set; }
    public string? Notes { get; set; }
    // Immutable policy snapshot: an edit never changes historical meaning.
    public string? PolicyCode { get; set; }
    public int PolicyRevision { get; set; }
    public bool LateRuleEnabled { get; set; }
    public int ConsecutiveLateThreshold { get; set; }
    public PenaltyTriggerMode PenaltyTriggerMode { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public decimal? PenaltyValue { get; set; }
    public ResetMode ResetMode { get; set; }
    public LCAP.HRMS.Domain.Employees.Employee Employee { get; set; } = null!;
    public LCAP.HRMS.Domain.Attendance.AttendanceRecord AttendanceRecord { get; set; } = null!;
}

