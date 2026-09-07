using LCAP.HRMS.Domain.AttendancePolicies;
namespace LCAP.HRMS.Application.AttendancePolicies;

public static class LateRuleEngine
{
    public static void Apply(AttendanceEvaluation row, AttendancePolicy policy, AttendanceEvaluation? previous)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(row.TimeZoneId);
        var local = row.AttendanceDate.ToDateTime(row.ShiftStartTime, DateTimeKind.Unspecified);
        if (row.ActualCheckInTime is null) { row.ReasonCode = "MissingCheckIn"; return; }
        if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) { row.ReasonCode = "ShiftTimezoneReviewRequired"; return; }
        var boundary = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero).AddMinutes(policy.GracePeriodMinutes);
        var delta = (row.ActualCheckInTime.Value - boundary).TotalMinutes;
        row.IsEvaluated = true; row.IsLate = policy.LateRuleEnabled && delta > 0; row.LateMinutes = row.IsLate ? (int)Math.Ceiling(delta) : 0;
        var same = previous is not null && previous.AttendancePolicyId == policy.Id && previous.PolicyRevision == policy.Revision;
        var count = same ? previous!.SequenceAfterEvaluation : 0;
        if (policy.ResetMode == ResetMode.Monthly && previous is not null && (previous.AttendanceDate.Year != row.AttendanceDate.Year || previous.AttendanceDate.Month != row.AttendanceDate.Month)) count = 0;
        if (!policy.LateRuleEnabled) count = 0;
        else if (row.IsLate) count++;
        else if (policy.ResetMode == ResetMode.OnNonLateAttendance) count = 0;
        row.ConsecutiveLateCount = count; row.ThresholdReached = policy.LateRuleEnabled && count >= policy.ConsecutiveLateThreshold;
        row.PenaltyTriggered = row.IsLate && policy.PenaltyTriggerMode switch
        {
            PenaltyTriggerMode.NextQualifyingLate => count == policy.ConsecutiveLateThreshold + 1,
            PenaltyTriggerMode.AfterThresholdReached => count == policy.ConsecutiveLateThreshold,
            _ => false
        };
        row.SequenceAfterEvaluation = row.PenaltyTriggered && policy.ResetMode == ResetMode.AfterPenalty ? 0 : count;
        row.ReasonCode = !policy.LateRuleEnabled ? "LateRuleDisabled" : row.PenaltyTriggered ? "QualifyingLatePenalty" : row.IsLate ? "LateAfterGrace" : "WithinGrace";
    }
    public static bool NextTriggers(AttendanceEvaluation row) => row.LateRuleEnabled && row.PenaltyTriggerMode switch
    {
        PenaltyTriggerMode.NextQualifyingLate => row.SequenceAfterEvaluation == row.ConsecutiveLateThreshold,
        PenaltyTriggerMode.AfterThresholdReached => row.SequenceAfterEvaluation == row.ConsecutiveLateThreshold - 1,
        _ => false
    };
}

