using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Domain.Regularisation;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.Extensions.Logging;

namespace LCAP.HRMS.Application.Regularisation;
public sealed class RegularisationService(IRegularisationRepository store, IEmployeeAccess access, ICurrentUser actor, AttendanceDateResolver dates, TimeProvider clock, CorrectionEvaluationService evaluation, ILogger<RegularisationService> logger) : IRegularisationService
{
    private Guid SelfId => access.EmployeeId ?? throw new ForbiddenException();
    private string Actor => actor.UserId ?? SelfId.ToString();

    private async Task<RegularisationEmployee> Self(CancellationToken ct)
    {
        var e = await store.EmployeeAsync(SelfId, ct);
        if (e is null || !e.Eligible || access.CompanyId != e.CompanyId)
            throw new ForbiddenException();
        return e;
    }

    private void Company(Guid company)
    {
        if (!access.IsSuperAdmin && access.CompanyId != company)
            throw new ForbiddenException();
    }

    private async Task Audit(AttendanceRegularisationRequest r, string action, string? reason, CancellationToken ct)
    {
        await store.AddAsync(new RegularisationAuditEntry { CompanyId = r.CompanyId, EmployeeId = r.EmployeeId, RegularisationRequestId = r.Id, Actor = Actor, OccurredAt = clock.GetUtcNow(), Action = action, Reason = reason }, ct);
        logger.LogInformation("Regularisation {Action}; request {RequestId}; employee {EmployeeId}; actor {Actor}", action, r.Id, r.EmployeeId, Actor);
    }

    private static void ValidateObject(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ValidationException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }

    private TimeZoneInfo Zone(RegularisationEmployee e)
    {
        try
        {
            return dates.Zone(e.CompanyId, e.BranchId);
        }
        catch (Exception ex)when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            throw new ConflictException("Attendance timezone is unavailable. Contact HR.");
        }
    }

    public Task<RegularisationResponse> SubmitAsync(RegularisationCreateRequest body, CancellationToken ct)
    {
        ValidateObject(body);
        return store.WriteAsync(SelfId, async () =>
        {
            var e = await Self(ct);
            var zone = Zone(e);
            var date = body.AttendanceDate!.Value;
            var now = clock.GetUtcNow();
            if (date.Year < 1900 || date > AttendanceDateResolver.Resolve(now, zone, e.Start, e.End))
                throw new ValidationException("Attendance date must be a valid current or past attendance date.");
            if (e.ReportingManagerId is not { } managerId || managerId == e.Id)
                throw new ConflictException("An active reporting manager is required. Contact HR.");
            var manager = await store.EmployeeAsync(managerId, ct);
            if (manager is null || !manager.Eligible || manager.CompanyId != e.CompanyId)
                throw new ConflictException("The assigned reporting manager is not eligible. Contact HR.");
            if (await store.ActiveAsync(e.Id, date, body.RequestType, ct))
                throw new ConflictException("An overlapping active regularisation request already exists for this date.");
            var raw = await store.RawAsync(e.Id, date, ct);
            if (raw is not null && (raw.CompanyId != e.CompanyId || raw.IsDeleted))
                throw new ConflictException("This attendance history requires HR review.");
            var correction = await store.CorrectionAsync(e.Id, date, ct);
            var r = new AttendanceRegularisationRequest
            {
                CompanyId = e.CompanyId,
                EmployeeId = e.Id,
                ReportingManagerId = managerId,
                BranchId = e.BranchId,
                ShiftId = raw?.ShiftId ?? e.ShiftId,
                AttendanceRecordId = raw?.Id,
                AttendanceDate = date,
                TimeZoneId = raw?.TimeZoneId ?? AttendanceDateResolver.DisplayZone(zone),
                ShiftStartTime = raw?.Shift.StartTime ?? e.Start,
                ShiftEndTime = raw?.Shift.EndTime ?? e.End,
                ShiftName = raw?.Shift.ShiftName ?? e.ShiftName,
                RequestType = body.RequestType,
                RequestedCheckInTime = body.RequestedCheckInTime,
                RequestedCheckOutTime = body.RequestedCheckOutTime,
                OriginalCheckInTime = raw?.CheckInTime,
                OriginalCheckOutTime = raw?.CheckOutTime,
                BaseCorrectionId = correction?.Id,
                BaseEffectiveCheckInTime = correction is null ? raw?.CheckInTime : correction.CorrectedCheckInTime,
                BaseEffectiveCheckOutTime = correction is null ? raw?.CheckOutTime : correction.CorrectedCheckOutTime,
                EmployeeReason = body.EmployeeReason.Trim(),
                SupportingNote = body.SupportingNote?.Trim(),
                AttachmentUrl = body.AttachmentUrl,
                SubmittedAt = now
            };
            RegularisationValidation.Validate(r, now);
            await store.AddAsync(r, ct);
            await Audit(r, "Submitted", r.EmployeeReason, ct);
            await store.SaveAsync(ct);
            return await GetAsync(r.Id, true, ct);
        }, ct);
    }

    public async Task<IReadOnlyList<RegularisationResponse>> ListAsync(string scope, RegularisationQuery q, CancellationToken ct)
    {
        if (q.Skip < 0 || q.Take is < 1 or > 200 || q.FromDate > q.ToDate || q.Status is { } status && !Enum.IsDefined(status))
            throw new ValidationException("Invalid regularisation filters or page.");
        if (scope == "me")
        {
            var e = await Self(ct);
            q = q with
            {
                EmployeeId = e.Id,
                CompanyId = e.CompanyId,
                ManagerId = null
            };
        }
        else if (scope == "team")
        {
            var e = await Self(ct);
            if (!access.IsManager)
                throw new ForbiddenException();
            q = q with
            {
                ManagerId = e.Id,
                CompanyId = e.CompanyId
            };
        }
        else
        {
            if (!access.CanWrite)
                throw new ForbiddenException();
            if (!access.IsSuperAdmin)
            {
                if (access.CompanyId is not { } c)
                    throw new ForbiddenException();
                q = q with
                {
                    CompanyId = c
                };
            }
        }

        return await store.ListAsync(q, ct);
    }

    public async Task<RegularisationResponse> GetAsync(Guid id, bool ownOnly, CancellationToken ct)
    {
        var r = await store.GetAsync(id, ct) ?? throw new NotFoundException("Regularisation request was not found.");
        Company(r.CompanyId);
        if (ownOnly ? access.EmployeeId != r.EmployeeId : !access.CanWrite && access.EmployeeId != r.EmployeeId && !(access.IsManager && access.EmployeeId == r.ReportingManagerId))
            throw new ForbiddenException();
        return (await store.ResponseAsync(id, ct))!;
    }

    public async Task<IReadOnlyList<RegularisationAuditEntry>> AuditAsync(Guid id, CancellationToken ct)
    {
        await GetAsync(id, false, ct);
        return await store.AuditAsync(id, ct);
    }

    public async Task<RegularisationResponse> ReviewAsync(Guid id, bool approve, ReviewRequest body, CancellationToken ct)
    {
        ValidateObject(body);
        if (!approve && string.IsNullOrWhiteSpace(body.ReviewerRemarks))
            throw new ValidationException("Rejection remarks are required.");
        var initial = await store.GetAsync(id, ct) ?? throw new NotFoundException("Regularisation request was not found.");
        return await store.WriteAsync(initial.EmployeeId, async () =>
        {
            var r = await store.GetAsync(id, ct) ?? throw new NotFoundException("Regularisation request was not found.");
            var manager = await Self(ct);
            if (!access.IsManager || manager.Id != r.ReportingManagerId || manager.CompanyId != r.CompanyId || manager.Id == r.EmployeeId)
                throw new ForbiddenException();
            if (r.Status != RegularisationStatus.PendingManager)
                throw new ConflictException("This request has already been reviewed or cancelled.");
            if (approve)
            {
                var raw = await store.RawAsync(r.EmployeeId, r.AttendanceDate, ct);
                var current = await store.CorrectionAsync(r.EmployeeId, r.AttendanceDate, ct);
                if (raw?.Id != r.AttendanceRecordId || raw?.CheckInTime != r.OriginalCheckInTime || raw?.CheckOutTime != r.OriginalCheckOutTime || current?.Id != r.BaseCorrectionId)
                    throw new ConflictException("Attendance changed after submission. Cancel and submit a new request.");
                RegularisationValidation.Validate(r, clock.GetUtcNow());
            }

            r.Status = approve ? RegularisationStatus.Approved : RegularisationStatus.Rejected;
            r.ReviewedAt = clock.GetUtcNow();
            r.ReviewedByEmployeeId = manager.Id;
            r.ReviewerRemarks = body.ReviewerRemarks?.Trim();
            await Audit(r, approve ? "Approved" : "Rejected", r.ReviewerRemarks, ct);
            await store.SaveAsync(ct);
            if (approve)
            {
                var prior = await store.CorrectionAsync(r.EmployeeId, r.AttendanceDate, ct);
                var c = new AttendanceCorrection
                {
                    CompanyId = r.CompanyId,
                    EmployeeId = r.EmployeeId,
                    AttendanceRecordId = r.AttendanceRecordId,
                    OriginalAttendanceRecordId = r.AttendanceRecordId,
                    RegularisationRequestId = r.Id,
                    AttendanceDate = r.AttendanceDate,
                    Version = (prior?.Version ?? 0) + 1,
                    CorrectedCheckInTime = r.RequestedCheckInTime ?? r.BaseEffectiveCheckInTime,
                    CorrectedCheckOutTime = r.RequestedCheckOutTime ?? r.BaseEffectiveCheckOutTime,
                    CorrectionType = r.RequestType,
                    Reason = r.EmployeeReason,
                    AppliedAt = clock.GetUtcNow(),
                    EffectiveFrom = clock.GetUtcNow(),
                    AppliedBy = Actor
                };
                await store.AddAsync(c, ct);
                await Audit(r, "CorrectionApplied", r.EmployeeReason, ct);
                await Audit(r, "EffectiveStateChanged", null, ct);
                await store.SaveAsync(ct);
                var impact = await evaluation.ApplyAsync(c, ct);
                await Audit(r, "ReevaluationTriggered", null, ct);
                if (impact)
                    await Audit(r, "PolicyImpactReviewRequired", "Historical pending events are preserved; controlled review is required.", ct);
                r.Status = RegularisationStatus.Applied;
                r.AppliedAt = c.AppliedAt;
                r.AppliedBy = Actor;
                await store.SaveAsync(ct);
            }

            return await GetAsync(id, false, ct);
        }, ct);
    }

    public async Task<RegularisationResponse> CancelAsync(Guid id, CancelRequest body, CancellationToken ct)
    {
        ValidateObject(body);
        var initial = await store.GetAsync(id, ct) ?? throw new NotFoundException("Regularisation request was not found.");
        return await store.WriteAsync(initial.EmployeeId, async () =>
        {
            var e = await Self(ct);
            var r = await store.GetAsync(id, ct) ?? throw new NotFoundException("Regularisation request was not found.");
            if (e.Id != r.EmployeeId || e.CompanyId != r.CompanyId)
                throw new ForbiddenException();
            if (r.Status != RegularisationStatus.PendingManager)
                throw new ConflictException("Only pending requests can be cancelled.");
            r.Status = RegularisationStatus.Cancelled;
            r.CancellationReason = body.CancellationReason.Trim();
            await Audit(r, "Cancelled", r.CancellationReason, ct);
            await store.SaveAsync(ct);
            return await GetAsync(id, true, ct);
        }, ct);
    }
}

public static class RegularisationValidation
{
    public static void Validate(AttendanceRegularisationRequest r, DateTimeOffset now)
    {
        var i = r.RequestedCheckInTime;
        var o = r.RequestedCheckOutTime;
        var type = r.RequestType;
        if (string.IsNullOrWhiteSpace(r.EmployeeReason) || !Enum.IsDefined(type))
            throw new ValidationException("A valid request type and reason are required.");
        if (type is RegularisationType.MissedCheckIn or RegularisationType.IncorrectCheckIn && (i is null || o is not null))
            throw new ValidationException("This request requires only a proposed check-in.");
        if (type is RegularisationType.MissedCheckOut or RegularisationType.IncorrectCheckOut && (o is null || i is not null))
            throw new ValidationException("This request requires only a proposed check-out.");
        if (type is RegularisationType.IncorrectBoth or RegularisationType.AttendanceNotRecorded && (i is null || o is null))
            throw new ValidationException("Both proposed times are required.");
        if (type == RegularisationType.AttendanceNotRecorded && (r.AttendanceRecordId is not null || r.BaseCorrectionId is not null))
            throw new ValidationException("Attendance already exists. Select an incorrect-time request.");
        if (type == RegularisationType.MissedCheckIn && r.BaseEffectiveCheckInTime is not null || type == RegularisationType.MissedCheckOut && r.BaseEffectiveCheckOutTime is not null)
            throw new ValidationException("The selected punch is already present.");
        if (type is RegularisationType.IncorrectCheckIn or RegularisationType.IncorrectBoth && r.BaseEffectiveCheckInTime is null || type is RegularisationType.IncorrectCheckOut or RegularisationType.IncorrectBoth && r.BaseEffectiveCheckOutTime is null)
            throw new ValidationException("The selected original punch does not exist.");
        var effectiveIn = i ?? r.BaseEffectiveCheckInTime;
        var effectiveOut = o ?? r.BaseEffectiveCheckOutTime;
        if (effectiveOut is not null && effectiveIn is null)
            throw new ValidationException("A check-in is required before a check-out.");
        if (effectiveIn is { } start && effectiveOut is { } end && (end < start || end - start > TimeSpan.FromHours(36)))
            throw new ValidationException("Check-out must follow check-in within 36 hours. Overnight times must include the next date.");
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(r.TimeZoneId);
        }
        catch (Exception ex)when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ConflictException("Attendance timezone is unavailable.");
        }

        foreach (var timestamp in new[]
        {
            i,
            o
        }.OfType<DateTimeOffset>())
        {
            var local = TimeZoneInfo.ConvertTime(timestamp, zone);
            var day = DateOnly.FromDateTime(local.DateTime);
            if (timestamp > now || day < r.AttendanceDate || day > r.AttendanceDate.AddDays(1))
                throw new ValidationException("Proposed times must be past times on the attendance date or following overnight date.");
        }

        if (i is { } proposedIn && AttendanceDateResolver.Resolve(proposedIn, zone, r.ShiftStartTime, r.ShiftEndTime) != r.AttendanceDate)
            throw new ValidationException("Check-in does not belong to the selected shift attendance date.");
        if (r.AttachmentUrl is { } url && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"))
            throw new ValidationException("Attachment URL must use HTTPS.");
    }
}
