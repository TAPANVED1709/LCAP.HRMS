using LCAP.HRMS.Domain.Common;
namespace LCAP.HRMS.Domain.AttendancePolicies;

public enum PenaltyTriggerMode { NextQualifyingLate = 1, AfterThresholdReached = 2, ManualReview = 3 }
public enum PenaltyType { HalfDay = 1, FullDay = 2, FixedAmount = 3, Hourly = 4, Custom = 5 }
public enum ResetMode { AfterPenalty = 1, OnNonLateAttendance = 2, Monthly = 3, Never = 4 }
public enum PenaltyEventStatus { Pending = 1, Consumed = 2, Cancelled = 3 }
public sealed class AttendancePolicy : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string PolicyCode { get; set; } = "";
    public string PolicyName { get; set; } = "";
    public string? Description { get; set; }
    public int GracePeriodMinutes { get; set; }
    public bool LateRuleEnabled { get; set; } = true;
    public int ConsecutiveLateThreshold { get; set; } = 3;
    public PenaltyTriggerMode PenaltyTriggerMode { get; set; } = PenaltyTriggerMode.NextQualifyingLate;
    public PenaltyType PenaltyType { get; set; } = PenaltyType.HalfDay;
    public decimal? PenaltyValue { get; set; }
    public ResetMode ResetMode { get; set; } = ResetMode.AfterPenalty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; } = 1;
}

