using LCAP.HRMS.Domain.Common;

namespace LCAP.HRMS.Domain.Regularisation;
public enum RegularisationType
{
    MissedCheckIn = 1,
    MissedCheckOut = 2,
    IncorrectCheckIn = 3,
    IncorrectCheckOut = 4,
    IncorrectBoth = 5,
    AttendanceNotRecorded = 6,
    Other = 7
}

public enum RegularisationStatus
{
    Draft = 1,
    Submitted = 2,
    PendingManager = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6,
    Applied = 7
}

public enum AttendanceSource
{
    Raw = 1,
    Regularised = 2
}

public sealed class AttendanceRegularisationRequest : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? AttendanceRecordId { get; set; }
    public Guid ReportingManagerId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public TimeOnly ShiftStartTime { get; set; }
    public TimeOnly ShiftEndTime { get; set; }
    public string ShiftName { get; set; } = "";
    public RegularisationType RequestType { get; set; }
    public DateTimeOffset? RequestedCheckInTime { get; set; }
    public DateTimeOffset? RequestedCheckOutTime { get; set; }
    public DateTimeOffset? OriginalCheckInTime { get; set; }
    public DateTimeOffset? OriginalCheckOutTime { get; set; }
    public Guid? BaseCorrectionId { get; set; }
    public DateTimeOffset? BaseEffectiveCheckInTime { get; set; }
    public DateTimeOffset? BaseEffectiveCheckOutTime { get; set; }
    public string EmployeeReason { get; set; } = "";
    public string? SupportingNote { get; set; }
    public string? AttachmentUrl { get; set; }
    public RegularisationStatus Status { get; set; } = RegularisationStatus.PendingManager;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public string? ReviewerRemarks { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public string? AppliedBy { get; set; }
    public string? CancellationReason { get; set; }
    public LCAP.HRMS.Domain.Employees.Employee Employee { get; set; } = null!;
    public LCAP.HRMS.Domain.Employees.Employee ReportingManager { get; set; } = null!;
}

public sealed class AttendanceCorrection : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? AttendanceRecordId { get; set; }
    public Guid? OriginalAttendanceRecordId { get; set; }
    public Guid RegularisationRequestId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public int Version { get; set; }
    public DateTimeOffset? CorrectedCheckInTime { get; set; }
    public DateTimeOffset? CorrectedCheckOutTime { get; set; }
    public RegularisationType CorrectionType { get; set; }
    public string Reason { get; set; } = "";
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public string AppliedBy { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public AttendanceRegularisationRequest Request { get; set; } = null!;
}

// Append-only snapshot; existing Day 4 evaluations and penalty events remain untouched.
public sealed class AttendanceEvaluationRevision : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? AttendanceRecordId { get; set; }
    public Guid CorrectionId { get; set; }
    public Guid? OriginalEvaluationId { get; set; }
    public Guid? RelatedPenaltyEventId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public int Version { get; set; }
    public bool PolicyImpactReviewRequired { get; set; }
    public string ResultJson { get; set; } = "";
}

public sealed class RegularisationAuditEntry : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RegularisationRequestId { get; set; }
    public string Actor { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public string Action { get; set; } = "";
    public string? Reason { get; set; }
}
