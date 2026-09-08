using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.Extensions.Logging;

namespace LCAP.HRMS.Application.AttendancePolicies;
public sealed class AttendanceEvaluationService(IAttendanceEvaluationRepository repository, IAttendancePolicyResolver resolver, IUnitOfWork work, IEmployeeAccess access, IAttendanceRepository attendance, AttendanceDateResolver dates, TimeProvider time, ILogger<AttendanceEvaluationService> logger) : IAttendanceEvaluationService
{
    private void Scope(Guid company, Guid employee)
    {
        if (!access.IsSuperAdmin && (access.CompanyId is null || access.CompanyId != company))
            throw new ForbiddenException();
        if (!access.CanWrite && access.EmployeeId != employee)
            throw new ForbiddenException();
    }

    public async Task EvaluateAsync(Guid attendanceId, CancellationToken ct)
    {
        var initial = await repository.InputAsync(attendanceId, ct) ?? throw new NotFoundException("Attendance was not found.");
        await repository.WriteAsync(initial.Record.EmployeeId, async () =>
        {
            if (await repository.ExistingAsync(attendanceId, ct)is not null)
                return true;
            var input = await repository.InputAsync(attendanceId, ct) ?? throw new NotFoundException("Attendance was not found.");
            var r = input.Record;
            var p = await resolver.ResolveAsync(r.EmployeeId, r.CompanyId, r.AttendanceDate, ct);
            var e = new AttendanceEvaluation
            {
                CompanyId = r.CompanyId,
                EmployeeId = r.EmployeeId,
                AttendanceRecordId = r.Id,
                AttendanceDate = r.AttendanceDate,
                ShiftStartTime = input.ShiftStart,
                ShiftName = input.ShiftName,
                TimeZoneId = r.TimeZoneId,
                ActualCheckInTime = r.CheckInTime,
                EvaluatedAt = time.GetUtcNow(),
                ReasonCode = "PolicyNotConfigured"
            };
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
                if (await repository.LaterAsync(r.EmployeeId, r.AttendanceDate, ct))
                    e.ReasonCode = "HistoricalOrderReviewRequired";
                else
                    try
                    {
                        LateRuleEngine.Apply(e, p, await repository.PreviousAsync(r.EmployeeId, r.AttendanceDate, ct));
                    }
                    catch (Exception ex)when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
                    {
                        e.ReasonCode = "TimezoneNotConfigured";
                        logger.LogWarning("Evaluation timezone unavailable for attendance {AttendanceId}", r.Id);
                    }
            }

            AttendancePenaltyEvent? penalty = e.PenaltyTriggered ? new()
            {
                CompanyId = r.CompanyId,
                EmployeeId = r.EmployeeId,
                AttendanceEvaluationId = e.Id,
                AttendanceRecordId = r.Id,
                AttendancePolicyId = p!.Id,
                PenaltyDate = r.AttendanceDate,
                PenaltyType = p.PenaltyType,
                PenaltyValue = p.PenaltyValue,
                ReasonCode = "QualifyingLatePenalty",
                GeneratedAt = time.GetUtcNow()
            }

            : null;
            await repository.AddAsync(e, penalty, ct);
            await work.SaveChangesAsync(ct);
            logger.LogInformation("Attendance evaluated. Record {AttendanceId}; evaluation {EvaluationId}; policy {PolicyId}; version {Version}; reason {Reason}", r.Id, e.Id, p?.Id, e.EvaluationVersion, e.ReasonCode);
            if (penalty is not null)
                logger.LogInformation("Pending attendance penalty event generated. Evaluation {EvaluationId}; event {EventId}", e.Id, penalty.Id);
            return true;
        }, ct);
    }

    public async Task<EvaluationResponse?> GetAsync(Guid attendanceId, CancellationToken ct)
    {
        var input = await repository.InputAsync(attendanceId, ct) ?? throw new NotFoundException("Attendance was not found.");
        Scope(input.Record.CompanyId, input.Record.EmployeeId);
        return await repository.ResponseAsync(attendanceId, ct);
    }

    private EvaluationQuery Query(EvaluationQuery q)
    {
        if (q.Skip < 0 || q.Take is < 1 or > 1000 || q.FromDate > q.ToDate)
            throw new ValidationException("Invalid filters or page.");
        if (!access.CanWrite)
            throw new ForbiddenException();
        if (!access.IsSuperAdmin)
        {
            if (access.CompanyId is not { } company)
                throw new ForbiddenException();
            if (q.CompanyId is { } requested && requested != company)
                throw new ForbiddenException();
            q = q with
            {
                CompanyId = company
            };
        }

        return q;
    }

    public Task<IReadOnlyList<EvaluationResponse>> ListAsync(EvaluationQuery q, CancellationToken ct) => repository.ListAsync(Query(q), ct);
    public async Task<IReadOnlyList<PenaltyResponse>> PenaltiesAsync(EvaluationQuery q, CancellationToken ct)
    {
        if (!access.CanWrite && q.EmployeeId is { } employee)
        {
            var company = await repository.EmployeeCompanyAsync(employee, ct) ?? throw new NotFoundException("Employee was not found.");
            Scope(company, employee);
            if (q.Skip < 0 || q.Take is < 1 or > 1000 || q.FromDate > q.ToDate)
                throw new ValidationException("Invalid page.");
            q = q with
            {
                CompanyId = company
            };
        }
        else
            q = Query(q);
        return await repository.PenaltiesAsync(q, ct);
    }

    public async Task<PenaltyResponse> PenaltyAsync(Guid id, CancellationToken ct)
    {
        var p = await repository.PenaltyAsync(id, ct) ?? throw new NotFoundException("Penalty event was not found.");
        Scope(p.CompanyId, p.EmployeeId);
        return p;
    }

    public async Task<LateSummary> SummaryAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await attendance.EmployeeAsync(employeeId, ct) ?? throw new NotFoundException("Employee was not found.");
        Scope(employee.CompanyId, employeeId);
        var day = AttendanceDateResolver.Resolve(time.GetUtcNow(), dates.Zone(employee.CompanyId, employee.BranchId), employee.StartTime, employee.EndTime);
        var p = await resolver.ResolveAsync(employeeId, employee.CompanyId, day, ct);
        var previous = await repository.PreviousAsync(employeeId, day.AddDays(1), ct);
        var latest = previous is null ? null : LCAP.HRMS.Application.Regularisation.EvaluationSnapshots.Response(previous);
        var count = latest is not null && p is not null && latest.AttendancePolicyId == p.Id && latest.PolicyRevision == p.Revision ? latest.SequenceAfterEvaluation : 0;
        if (p?.ResetMode == ResetMode.Monthly && latest is not null && (latest.AttendanceDate.Year != day.Year || latest.AttendanceDate.Month != day.Month))
            count = 0;
        var threshold = p?.ConsecutiveLateThreshold ?? 0;
        var enabled = p?.LateRuleEnabled == true;
        var next = enabled && p!.PenaltyTriggerMode switch
        {
            PenaltyTriggerMode.NextQualifyingLate => count == threshold,
            PenaltyTriggerMode.AfterThresholdReached => count == threshold - 1,
            _ => false
        };
        var penalty = (await repository.PenaltiesAsync(new(EmployeeId: employeeId, CompanyId: employee.CompanyId, Take: 1), ct)).FirstOrDefault();
        return new(employeeId, count, threshold, enabled && count >= threshold, next, penalty);
    }
}
