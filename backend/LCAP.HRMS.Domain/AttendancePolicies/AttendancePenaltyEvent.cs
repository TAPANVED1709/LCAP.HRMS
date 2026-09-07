using LCAP.HRMS.Domain.Common;
namespace LCAP.HRMS.Domain.AttendancePolicies;

public sealed class AttendancePenaltyEvent : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid AttendanceEvaluationId { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public Guid AttendancePolicyId { get; set; }
    public DateOnly PenaltyDate { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public decimal? PenaltyValue { get; set; }
    public string ReasonCode { get; set; } = "";
    public PenaltyEventStatus Status { get; set; } = PenaltyEventStatus.Pending;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? PayrollConsumedAt { get; set; }
    public Guid? PayrollRunId { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public AttendanceEvaluation AttendanceEvaluation { get; set; } = null!;
}

