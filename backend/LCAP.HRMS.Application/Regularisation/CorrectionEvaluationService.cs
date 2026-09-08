using System.Text.Json;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Domain.AttendancePolicies;
using LCAP.HRMS.Domain.Regularisation;

namespace LCAP.HRMS.Application.Regularisation;
public sealed class CorrectionEvaluationService(IRegularisationRepository store, IAttendancePolicyResolver resolver, TimeProvider clock)
{
    public async Task<bool> ApplyAsync(AttendanceCorrection correction, CancellationToken ct)
    {
        var timeline = await store.TimelineAsync(correction.EmployeeId, ct);
        if (timeline.Count > 5000)
            throw new ConflictException("This history requires a supervised recalculation. No changes were applied.");
        AttendanceEvaluation? previous = null;
        bool review = false;
        foreach (var day in timeline.OrderBy(d => d.Date))
        {
            if (day.Date < correction.AttendanceDate)
            {
                previous = day.Latest is null ? day.Original : EvaluationSnapshots.Read(day.Latest);
                continue;
            }

            var original = day.Original;
            var e = original is null ? new AttendanceEvaluation
            {
                CompanyId = correction.CompanyId,
                EmployeeId = correction.EmployeeId,
                AttendanceRecordId = day.RawId ?? Guid.Empty,
                AttendanceDate = day.Date,
                ShiftStartTime = day.ShiftStart,
                ShiftName = day.ShiftName,
                TimeZoneId = day.TimeZone
            }

            : JsonSerializer.Deserialize<AttendanceEvaluation>(JsonSerializer.Serialize(original))!;
            // Preserve the original policy snapshot whenever one exists, including no-policy outcomes.
            AttendancePolicy? p = original is null ? await resolver.ResolveAsync(correction.EmployeeId, correction.CompanyId, day.Date, ct) : original.AttendancePolicyId is null ? null : new AttendancePolicy
            {
                Id = original.AttendancePolicyId.Value,
                CompanyId = original.CompanyId,
                PolicyCode = original.PolicyCode ?? "",
                Revision = original.PolicyRevision,
                GracePeriodMinutes = original.GracePeriodMinutes,
                LateRuleEnabled = original.LateRuleEnabled,
                ConsecutiveLateThreshold = original.ConsecutiveLateThreshold,
                PenaltyTriggerMode = original.PenaltyTriggerMode,
                PenaltyType = original.PenaltyType,
                PenaltyValue = original.PenaltyValue,
                ResetMode = original.ResetMode
            };
            if (original is null && day.Latest is not null)
            {
                var snapshot = EvaluationSnapshots.Read(day.Latest);
                e = snapshot;
                p = snapshot.AttendancePolicyId is null ? null : new AttendancePolicy
                {
                    Id = snapshot.AttendancePolicyId.Value,
                    PolicyCode = snapshot.PolicyCode ?? "",
                    Revision = snapshot.PolicyRevision,
                    GracePeriodMinutes = snapshot.GracePeriodMinutes,
                    LateRuleEnabled = snapshot.LateRuleEnabled,
                    ConsecutiveLateThreshold = snapshot.ConsecutiveLateThreshold,
                    PenaltyTriggerMode = snapshot.PenaltyTriggerMode,
                    PenaltyType = snapshot.PenaltyType,
                    PenaltyValue = snapshot.PenaltyValue,
                    ResetMode = snapshot.ResetMode
                };
            }

            e.Id = Guid.NewGuid();
            e.EvaluationVersion = (day.Latest?.Version ?? original?.EvaluationVersion ?? 0) + 1;
            e.ActualCheckInTime = day.CheckIn;
            e.EvaluatedAt = clock.GetUtcNow();
            e.IsEvaluated = false;
            e.IsLate = false;
            e.LateMinutes = 0;
            e.ConsecutiveLateCount = 0;
            e.SequenceAfterEvaluation = 0;
            e.ThresholdReached = false;
            e.PenaltyTriggered = false;
            e.ReasonCode = "PolicyNotConfigured";
            if (p is not null)
            {
                e.AttendancePolicyId = p.Id;
                e.PolicyCode = p.PolicyCode;
                e.PolicyRevision = p.Revision;
                e.GracePeriodMinutes = p.GracePeriodMinutes;
                e.LateRuleEnabled = p.LateRuleEnabled;
                e.ConsecutiveLateThreshold = p.ConsecutiveLateThreshold;
                e.PenaltyTriggerMode = p.PenaltyTriggerMode;
                e.PenaltyType = p.PenaltyType;
                e.PenaltyValue = p.PenaltyValue;
                e.ResetMode = p.ResetMode;
                try
                {
                    LateRuleEngine.Apply(e, p, previous);
                }
                catch (Exception ex)when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
                {
                    e.ReasonCode = "TimezoneNotConfigured";
                }
            }

            // A reason-only exception is not attendance and must not erase a qualifying sequence.
            if (day.CheckIn is null && previous is not null && previous.AttendancePolicyId == e.AttendancePolicyId && previous.PolicyRevision == e.PolicyRevision)
            {
                var carry = previous.SequenceAfterEvaluation;
                if (e.ResetMode == ResetMode.Monthly && (previous.AttendanceDate.Year != e.AttendanceDate.Year || previous.AttendanceDate.Month != e.AttendanceDate.Month))
                    carry = 0;
                e.ConsecutiveLateCount = carry;
                e.SequenceAfterEvaluation = carry;
                e.ThresholdReached = e.LateRuleEnabled && carry >= e.ConsecutiveLateThreshold;
            }

            var impact = day.PenaltyId is not null || e.PenaltyTriggered || day.Latest?.PolicyImpactReviewRequired == true;
            await store.AddAsync(new AttendanceEvaluationRevision { CompanyId = correction.CompanyId, EmployeeId = correction.EmployeeId, AttendanceRecordId = day.RawId, CorrectionId = correction.Id, OriginalEvaluationId = original?.Id, RelatedPenaltyEventId = day.PenaltyId, AttendanceDate = day.Date, Version = e.EvaluationVersion, PolicyImpactReviewRequired = impact, ResultJson = JsonSerializer.Serialize(e) }, ct);
            previous = e;
            review |= impact;
        }

        await store.SaveAsync(ct);
        return review;
    }
}

public sealed class AttendanceEffectiveStateService(IRegularisationRepository store, IEmployeeAccess access, AttendanceDateResolver dates) : IAttendanceEffectiveStateService
{
    public async Task<EffectiveAttendance> GetAsync(Guid employee, DateOnly date, CancellationToken ct)
    {
        var e = await store.EmployeeAsync(employee, ct) ?? throw new NotFoundException("Employee was not found.");
        if (!access.IsSuperAdmin && access.CompanyId != e.CompanyId || !access.CanWrite && access.EmployeeId != employee)
            throw new ForbiddenException();
        var raw = await store.RawAsync(employee, date, ct);
        var correction = await store.CorrectionAsync(employee, date, ct);
        var revision = await store.RevisionAsync(employee, date, ct);
        EvaluationResponse? result = revision is null ? null : EvaluationSnapshots.Response(EvaluationSnapshots.Read(revision));
        if (result is null && raw is not null)
        {
            var entry = (await store.TimelineAsync(employee, ct)).FirstOrDefault(d => d.Date == date);
            if (entry?.Original is { } original)
                result = EvaluationSnapshots.Response(original);
        }

        var request = correction is null ? null : await store.GetAsync(correction.RegularisationRequestId, ct);
        string zone = raw?.TimeZoneId ?? request?.TimeZoneId ?? AttendanceDateResolver.DisplayZone(dates.Zone(e.CompanyId, e.BranchId));
        return new(employee, date, zone, raw?.Id, raw?.CheckInTime, raw?.CheckOutTime, correction is null ? raw?.CheckInTime : correction.CorrectedCheckInTime, correction is null ? raw?.CheckOutTime : correction.CorrectedCheckOutTime, correction is null ? AttendanceSource.Raw : AttendanceSource.Regularised, correction?.RegularisationRequestId, correction?.Id, result, revision?.PolicyImpactReviewRequired ?? false);
    }
}
