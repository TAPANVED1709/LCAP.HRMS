using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Domain.AttendancePolicies;
using Xunit;
namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class LateRuleEngineTests
{
    private static AttendancePolicy Policy() => new() { GracePeriodMinutes = 15, ConsecutiveLateThreshold = 3, LateRuleEnabled = true, Revision = 1, PenaltyTriggerMode = PenaltyTriggerMode.NextQualifyingLate, ResetMode = ResetMode.AfterPenalty };
    private static AttendanceEvaluation Row(AttendancePolicy p, DateOnly date, int hour, int minute, int second = 0, string zone = "Asia/Kolkata", int shift = 9, int shiftMinute = 30) { var tz = TimeZoneInfo.FindSystemTimeZoneById(zone); var local = date.ToDateTime(new TimeOnly(hour, minute, second), DateTimeKind.Unspecified); return new() { AttendancePolicyId = p.Id, PolicyRevision = p.Revision, AttendanceDate = date, ShiftStartTime = new(shift, shiftMinute), TimeZoneId = zone, ActualCheckInTime = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, tz), TimeSpan.Zero) }; }
    [Theory]
    [InlineData(9, 20, 0, false, 0)]
    [InlineData(9, 45, 0, false, 0)]
    [InlineData(9, 45, 1, true, 1)]
    [InlineData(9, 48, 0, true, 3)]
    [InlineData(9, 49, 59, true, 5)]
    public void Grace_boundary_and_minutes(int h, int m, int s, bool late, int minutes) { var p = Policy(); var e = Row(p, new(2026, 9, 8), h, m, s); LateRuleEngine.Apply(e, p, null); Assert.Equal(late, e.IsLate); Assert.Equal(minutes, e.LateMinutes); }
    [Fact] public void Configured_grace_overrides_shift_grace() { var p = Policy(); p.GracePeriodMinutes = 20; var e = Row(p, new(2026, 9, 8), 9, 48); LateRuleEngine.Apply(e, p, null); Assert.False(e.IsLate); }
    [Fact] public void Four_lates_trigger_once_and_next_starts_at_one() { var p = Policy(); AttendanceEvaluation? prior = null; int[] mins = [48, 49, 46, 50, 48]; for (int i = 0; i < 5; i++) { var e = Row(p, new(2026, 9, 8 + i), 9, mins[i]); LateRuleEngine.Apply(e, p, prior); Assert.Equal(i == 4 ? 1 : i + 1, e.ConsecutiveLateCount); Assert.Equal(i == 3, e.PenaltyTriggered); Assert.Equal(i is 2 or 3, e.ThresholdReached); if (i == 3) Assert.Equal(0, e.SequenceAfterEvaluation); prior = e; } }
    [Theory]
    [InlineData(ResetMode.AfterPenalty, 2)]
    [InlineData(ResetMode.OnNonLateAttendance, 1)]
    [InlineData(ResetMode.Monthly, 2)]
    [InlineData(ResetMode.Never, 2)]
    public void Punctual_day_semantics(ResetMode mode, int count) { var p = Policy(); p.ResetMode = mode; var a = Row(p, new(2026, 9, 8), 9, 48); LateRuleEngine.Apply(a, p, null); var b = Row(p, new(2026, 9, 9), 9, 30); LateRuleEngine.Apply(b, p, a); var c = Row(p, new(2026, 9, 10), 9, 48); LateRuleEngine.Apply(c, p, b); Assert.Equal(count, c.ConsecutiveLateCount); }
    [Fact] public void Monthly_resets_at_calendar_boundary() { var p = Policy(); p.ResetMode = ResetMode.Monthly; var a = Row(p, new(2026, 9, 30), 9, 48); LateRuleEngine.Apply(a, p, null); var b = Row(p, new(2026, 10, 1), 9, 48); LateRuleEngine.Apply(b, p, a); Assert.Equal(1, b.ConsecutiveLateCount); }
    [Theory]
    [InlineData(PenaltyTriggerMode.AfterThresholdReached, 3)]
    [InlineData(PenaltyTriggerMode.NextQualifyingLate, 4)]
    [InlineData(PenaltyTriggerMode.ManualReview, 0)]
    public void Trigger_modes(PenaltyTriggerMode mode, int trigger) { var p = Policy(); p.PenaltyTriggerMode = mode; p.ResetMode = ResetMode.Never; AttendanceEvaluation? prior = null; for (int i = 1; i <= 6; i++) { var e = Row(p, new(2026, 9, i), 9, 48); LateRuleEngine.Apply(e, p, prior); Assert.Equal(i == trigger, e.PenaltyTriggered); prior = e; } }
    [Fact] public void Missing_checkin_is_not_late() { var p = Policy(); var e = Row(p, new(2026, 9, 8), 9, 48); e.ActualCheckInTime = null; LateRuleEngine.Apply(e, p, null); Assert.False(e.IsEvaluated); Assert.False(e.IsLate); Assert.False(e.PenaltyTriggered); }
    [Fact] public void Disabled_rule_clears_counter() { var p = Policy(); p.LateRuleEnabled = false; var e = Row(p, new(2026, 9, 8), 9, 48); LateRuleEngine.Apply(e, p, null); Assert.False(e.IsLate); Assert.Equal(0, e.ConsecutiveLateCount); }
    [Theory]
    [InlineData("Asia/Kolkata", 22, 15, 0, false)]
    [InlineData("Asia/Kolkata", 22, 15, 1, true)]
    [InlineData("UTC", 22, 16, 0, true)]
    public void Overnight_start_is_on_attendance_date(string zone, int h, int m, int sec, bool late) { var p = Policy(); var e = Row(p, new(2026, 9, 8), h, m, sec, zone, 22, 0); LateRuleEngine.Apply(e, p, null); Assert.Equal(late, e.IsLate); }
    [Fact] public void Overnight_checkin_after_midnight_is_compared_with_prior_evening() { var p = Policy(); var e = Row(p, new(2026, 9, 9), 0, 5, 0, "Asia/Kolkata", 22, 0); e.AttendanceDate = new(2026, 9, 8); LateRuleEngine.Apply(e, p, null); Assert.True(e.IsLate); Assert.Equal(110, e.LateMinutes); }
    [Fact] public void Policy_revision_starts_new_sequence() { var p = Policy(); var a = Row(p, new(2026, 9, 8), 9, 48); LateRuleEngine.Apply(a, p, null); p.Revision++; var b = Row(p, new(2026, 9, 9), 9, 48); LateRuleEngine.Apply(b, p, a); Assert.Equal(1, b.ConsecutiveLateCount); }
    [Fact] public void Missing_calendar_day_does_not_create_absence_or_reset() { var p = Policy(); var a = Row(p, new(2026, 9, 8), 9, 48); LateRuleEngine.Apply(a, p, null); var b = Row(p, new(2026, 9, 11), 9, 48); LateRuleEngine.Apply(b, p, a); Assert.Equal(2, b.ConsecutiveLateCount); }
}
