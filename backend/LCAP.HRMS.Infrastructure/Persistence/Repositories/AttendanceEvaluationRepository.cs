using System.Linq.Expressions;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.EntityFrameworkCore;
namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class AttendanceEvaluationRepository(ApplicationDbContext context, IAttendanceRepository attendance) : IAttendanceEvaluationRepository
{
    public async Task<EvaluationInput?> InputAsync(Guid id, CancellationToken ct) { var r = await context.AttendanceRecords.IgnoreQueryFilters().AsNoTracking().Include(r => r.Shift).SingleOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct); return r is null ? null : new(r, r.Shift.StartTime, r.Shift.ShiftName); }
    public Task<AttendanceEvaluation?> ExistingAsync(Guid id, CancellationToken ct) => context.AttendanceEvaluations.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(e => e.AttendanceRecordId == id, ct);
    public Task<AttendanceEvaluation?> PreviousAsync(Guid employee, DateOnly date, CancellationToken ct) => context.AttendanceEvaluations.IgnoreQueryFilters().AsNoTracking().Where(e => e.EmployeeId == employee && e.AttendanceDate < date && e.ReasonCode != "HistoricalOrderReviewRequired").OrderByDescending(e => e.AttendanceDate).FirstOrDefaultAsync(ct);
    public Task<bool> LaterAsync(Guid employee, DateOnly date, CancellationToken ct) => context.AttendanceEvaluations.IgnoreQueryFilters().AnyAsync(e => e.EmployeeId == employee && e.AttendanceDate > date, ct);
    public async Task AddAsync(AttendanceEvaluation evaluation, AttendancePenaltyEvent? penalty, CancellationToken ct) { await context.AddAsync(evaluation, ct); if (penalty is not null) await context.AddAsync(penalty, ct); }
    public Task<T> WriteAsync<T>(Guid employee, Func<Task<T>> action, CancellationToken ct) => context.Database.CurrentTransaction is not null ? action() : attendance.WriteAsync(employee, action, ct);
    private IQueryable<AttendanceEvaluation> History => context.AttendanceEvaluations.IgnoreQueryFilters().AsNoTracking().Where(e => !e.IsDeleted);
    internal static readonly Expression<Func<AttendanceEvaluation, EvaluationResponse>> Projection = e => new()
    {
        Id = e.Id,
        AttendanceRecordId = e.AttendanceRecordId,
        EmployeeId = e.EmployeeId,
        ReportingManagerId = e.Employee.ReportingManagerId,
        EmployeeCode = e.Employee.EmployeeCode,
        EmployeeName = e.Employee.FirstName + (e.Employee.LastName == null ? "" : " " + e.Employee.LastName),
        Branch = e.AttendanceRecord.WorkLocation.Branch.BranchName,
        ShiftName = e.ShiftName,
        AttendanceDate = e.AttendanceDate,
        TimeZoneId = e.TimeZoneId,
        CheckInTime = e.ActualCheckInTime,
        CheckOutTime = e.AttendanceRecord.CheckOutTime,
        AttendancePolicyId = e.AttendancePolicyId,
        PolicyCode = e.PolicyCode,
        PolicyRevision = e.PolicyRevision,
        EvaluationVersion = e.EvaluationVersion,
        IsEvaluated = e.IsEvaluated,
        IsLate = e.IsLate,
        LateMinutes = e.LateMinutes,
        GracePeriodMinutes = e.GracePeriodMinutes,
        ConsecutiveLateCount = e.ConsecutiveLateCount,
        SequenceAfterEvaluation = e.SequenceAfterEvaluation,
        Threshold = e.ConsecutiveLateThreshold,
        ThresholdReached = e.ThresholdReached,
        PenaltyTriggered = e.PenaltyTriggered,
        ReasonCode = e.ReasonCode,
        EvaluatedAt = e.EvaluatedAt,
        NextLateTriggersPenalty = e.LateRuleEnabled && ((e.PenaltyTriggerMode == PenaltyTriggerMode.NextQualifyingLate && e.SequenceAfterEvaluation == e.ConsecutiveLateThreshold) || (e.PenaltyTriggerMode == PenaltyTriggerMode.AfterThresholdReached && e.SequenceAfterEvaluation == e.ConsecutiveLateThreshold - 1))
    };
    public Task<EvaluationResponse?> ResponseAsync(Guid id, CancellationToken ct) => History.Where(e => e.AttendanceRecordId == id).Select(Projection).SingleOrDefaultAsync(ct);
    private IQueryable<AttendanceEvaluation> Filter(EvaluationQuery q) { var v = History; if (q.CompanyId is { } c) v = v.Where(e => e.CompanyId == c); if (q.EmployeeId is { } id) v = v.Where(e => e.EmployeeId == id); if (q.BranchId is { } b) v = v.Where(e => e.AttendanceRecord.WorkLocation.BranchId == b); if (q.FromDate is { } from) v = v.Where(e => e.AttendanceDate >= from); if (q.ToDate is { } to) v = v.Where(e => e.AttendanceDate <= to); if (q.LateOnly) v = v.Where(e => e.IsLate); if (q.PenaltyOnly) v = v.Where(e => e.PenaltyTriggered); return v; }
    public async Task<IReadOnlyList<EvaluationResponse>> ListAsync(EvaluationQuery q, CancellationToken ct) => await Filter(q).OrderByDescending(e => e.AttendanceDate).ThenBy(e => e.Id).Skip(q.Skip).Take(q.Take).Select(Projection).ToListAsync(ct);
    private static readonly Expression<Func<AttendancePenaltyEvent, PenaltyResponse>> PenaltyProjection = p => new() { CompanyId = p.CompanyId, Id = p.Id, EmployeeId = p.EmployeeId, AttendanceRecordId = p.AttendanceRecordId, AttendanceEvaluationId = p.AttendanceEvaluationId, AttendancePolicyId = p.AttendancePolicyId, EmployeeCode = p.AttendanceEvaluation.Employee.EmployeeCode, EmployeeName = p.AttendanceEvaluation.Employee.FirstName + (p.AttendanceEvaluation.Employee.LastName == null ? "" : " " + p.AttendanceEvaluation.Employee.LastName), PenaltyDate = p.PenaltyDate, LateMinutes = p.AttendanceEvaluation.LateMinutes, ConsecutiveLateCount = p.AttendanceEvaluation.ConsecutiveLateCount, PenaltyType = p.PenaltyType, ReasonCode = p.ReasonCode, Status = p.Status, GeneratedAt = p.GeneratedAt };
    public async Task<IReadOnlyList<PenaltyResponse>> PenaltiesAsync(EvaluationQuery q, CancellationToken ct) { var ids = Filter(q).Select(e => e.Id); return await context.AttendancePenaltyEvents.IgnoreQueryFilters().AsNoTracking().Where(p => !p.IsDeleted && ids.Contains(p.AttendanceEvaluationId)).OrderByDescending(p => p.PenaltyDate).ThenBy(p => p.Id).Skip(q.Skip).Take(q.Take).Select(PenaltyProjection).ToListAsync(ct); }
    public Task<PenaltyResponse?> PenaltyAsync(Guid id, CancellationToken ct) => context.AttendancePenaltyEvents.IgnoreQueryFilters().AsNoTracking().Where(p => p.Id == id && !p.IsDeleted).Select(PenaltyProjection).SingleOrDefaultAsync(ct);
    public Task<Guid?> EmployeeCompanyAsync(Guid id, CancellationToken ct) => context.Employees.IgnoreQueryFilters().Where(e => e.Id == id).Select(e => (Guid?)e.CompanyId).SingleOrDefaultAsync(ct);
}


