using LCAP.HRMS.Application.Regularisation;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Domain.Regularisation;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;
public sealed class RegularisationRepository(ApplicationDbContext db, IAttendanceRepository attendance) : IRegularisationRepository
{
    public Task<RegularisationEmployee?> EmployeeAsync(Guid id, CancellationToken ct) => db.Employees.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == id).Select(e => new RegularisationEmployee(e.Id, e.CompanyId, e.BranchId, e.ShiftId, e.ReportingManagerId, !e.IsDeleted && e.IsActive && !e.Company.IsDeleted && e.Company.IsActive && (e.EmployeeStatus == EmployeeStatus.Active || e.EmployeeStatus == EmployeeStatus.OnNotice), e.Shift.StartTime, e.Shift.EndTime, e.Shift.ShiftName)).SingleOrDefaultAsync(ct);
    public Task<AttendanceRecord?> RawAsync(Guid employee, DateOnly date, CancellationToken ct) => db.AttendanceRecords.IgnoreQueryFilters().AsNoTracking().Include(r => r.Shift).SingleOrDefaultAsync(r => r.EmployeeId == employee && r.AttendanceDate == date, ct);
    public Task<AttendanceCorrection?> CorrectionAsync(Guid employee, DateOnly date, CancellationToken ct) => db.AttendanceCorrections.IgnoreQueryFilters().AsNoTracking().Where(c => c.EmployeeId == employee && c.AttendanceDate == date && c.IsActive).OrderByDescending(c => c.Version).FirstOrDefaultAsync(ct);
    public Task<AttendanceRegularisationRequest?> GetAsync(Guid id, CancellationToken ct) => db.AttendanceRegularisationRequests.IgnoreQueryFilters().SingleOrDefaultAsync(r => r.Id == id, ct);
    public async Task<bool> ActiveAsync(Guid employee, DateOnly date, RegularisationType type, CancellationToken ct)
    {
        var types = await db.AttendanceRegularisationRequests.IgnoreQueryFilters().Where(r => r.EmployeeId == employee && r.AttendanceDate == date && (r.Status == RegularisationStatus.PendingManager || r.Status == RegularisationStatus.Submitted || r.Status == RegularisationStatus.Approved)).Select(r => r.RequestType).ToListAsync(ct);
        static int Parts(RegularisationType t) => t is RegularisationType.MissedCheckIn or RegularisationType.IncorrectCheckIn ? 1 : t is RegularisationType.MissedCheckOut or RegularisationType.IncorrectCheckOut ? 2 : 3;
        return types.Any(t => (Parts(t) & Parts(type)) != 0);
    }

    private IQueryable<AttendanceRegularisationRequest> Query(RegularisationQuery q)
    {
        var v = db.AttendanceRegularisationRequests.IgnoreQueryFilters().AsNoTracking();
        if (q.CompanyId is { } c)
            v = v.Where(r => r.CompanyId == c);
        if (q.EmployeeId is { } e)
            v = v.Where(r => r.EmployeeId == e);
        if (q.ManagerId is { } m)
            v = v.Where(r => r.ReportingManagerId == m);
        if (q.BranchId is { } b)
            v = v.Where(r => r.BranchId == b);
        if (q.Status is { } s)
            v = v.Where(r => r.Status == s);
        if (q.Date is { } d)
            v = v.Where(r => r.AttendanceDate == d);
        if (q.FromDate is { } f)
            v = v.Where(r => r.AttendanceDate >= f);
        if (q.ToDate is { } t)
            v = v.Where(r => r.AttendanceDate <= t);
        return v;
    }

    private IQueryable<RegularisationResponse> Project(IQueryable<AttendanceRegularisationRequest> query) => query.Select(r => new RegularisationResponse { Id = r.Id, EmployeeId = r.EmployeeId, EmployeeCode = r.Employee.EmployeeCode, EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName, ReportingManagerId = r.ReportingManagerId, ManagerName = r.ReportingManager.FirstName + " " + r.ReportingManager.LastName, AttendanceDate = r.AttendanceDate, TimeZoneId = r.TimeZoneId, RequestType = r.RequestType, Status = r.Status, OriginalCheckInTime = r.OriginalCheckInTime, OriginalCheckOutTime = r.OriginalCheckOutTime, BaseEffectiveCheckInTime = r.BaseEffectiveCheckInTime, BaseEffectiveCheckOutTime = r.BaseEffectiveCheckOutTime, RequestedCheckInTime = r.RequestedCheckInTime, RequestedCheckOutTime = r.RequestedCheckOutTime, EmployeeReason = r.EmployeeReason, SupportingNote = r.SupportingNote, AttachmentUrl = r.AttachmentUrl, SubmittedAt = r.SubmittedAt, ReviewedAt = r.ReviewedAt, ReviewedByEmployeeId = r.ReviewedByEmployeeId, ReviewerRemarks = r.ReviewerRemarks, AppliedAt = r.AppliedAt, CancellationReason = r.CancellationReason, PolicyImpactReviewRequired = db.AttendanceEvaluationRevisions.Any(v => v.PolicyImpactReviewRequired && db.AttendanceCorrections.Any(c => c.Id == v.CorrectionId && c.RegularisationRequestId == r.Id)) });
    public async Task<IReadOnlyList<RegularisationResponse>> ListAsync(RegularisationQuery q, CancellationToken ct) => await Project(Query(q).OrderByDescending(r => r.AttendanceDate).ThenByDescending(r => r.Id).Skip(q.Skip).Take(q.Take)).ToListAsync(ct);
    public Task<RegularisationResponse?> ResponseAsync(Guid id, CancellationToken ct) => Project(db.AttendanceRegularisationRequests.IgnoreQueryFilters().AsNoTracking().Where(r => r.Id == id)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<RegularisationAuditEntry>> AuditAsync(Guid id, CancellationToken ct)
    {
        var rows = await db.RegularisationAuditEntries.IgnoreQueryFilters().AsNoTracking().Where(a => a.RegularisationRequestId == id).ToListAsync(ct);
        string[] order = ["Submitted", "Approved", "Rejected", "Cancelled", "CorrectionApplied", "EffectiveStateChanged", "ReevaluationTriggered", "PolicyImpactReviewRequired"];
        return rows.OrderBy(a => a.OccurredAt).ThenBy(a => Array.IndexOf(order, a.Action)).ToList();
    }

    public Task<AttendanceEvaluationRevision?> RevisionAsync(Guid employee, DateOnly date, CancellationToken ct) => db.AttendanceEvaluationRevisions.IgnoreQueryFilters().AsNoTracking().Where(v => v.EmployeeId == employee && v.AttendanceDate == date).OrderByDescending(v => v.Version).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<ReevaluationDay>> TimelineAsync(Guid employee, CancellationToken ct)
    {
        var raw = await db.AttendanceRecords.IgnoreQueryFilters().AsNoTracking().Include(r => r.Shift).Where(r => r.EmployeeId == employee).ToListAsync(ct);
        var corrections = (await db.AttendanceCorrections.IgnoreQueryFilters().AsNoTracking().Include(c => c.Request).Where(c => c.EmployeeId == employee && c.IsActive).ToListAsync(ct)).GroupBy(c => c.AttendanceDate).ToDictionary(g => g.Key, g => g.MaxBy(c => c.Version)!);
        var original = await db.AttendanceEvaluations.IgnoreQueryFilters().AsNoTracking().Where(v => v.EmployeeId == employee).ToDictionaryAsync(v => v.AttendanceDate, ct);
        var latest = (await db.AttendanceEvaluationRevisions.IgnoreQueryFilters().AsNoTracking().Where(v => v.EmployeeId == employee).ToListAsync(ct)).GroupBy(v => v.AttendanceDate).ToDictionary(g => g.Key, g => g.MaxBy(v => v.Version)!);
        var events = await db.AttendancePenaltyEvents.IgnoreQueryFilters().AsNoTracking().Where(p => p.EmployeeId == employee).ToDictionaryAsync(p => p.PenaltyDate, p => p.Id, ct);
        var records = raw.ToDictionary(r => r.AttendanceDate);
        var result = new List<ReevaluationDay>();
        foreach (var day in records.Keys.Union(corrections.Keys).Order())
        {
            var r = records.GetValueOrDefault(day);
            var c = corrections.GetValueOrDefault(day);
            result.Add(new(day, r?.Id, c is null ? r?.CheckInTime : c.CorrectedCheckInTime, c is null ? r?.CheckOutTime : c.CorrectedCheckOutTime, r?.Shift.StartTime ?? c!.Request.ShiftStartTime, r?.Shift.ShiftName ?? c!.Request.ShiftName, r?.TimeZoneId ?? c!.Request.TimeZoneId, original.GetValueOrDefault(day), latest.GetValueOrDefault(day), events.TryGetValue(day, out var id) ? id : null));
        }

        return result;
    }

    public async Task AddAsync(object entity, CancellationToken ct) => await db.AddAsync(entity, ct);
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public Task<T> WriteAsync<T>(Guid employee, Func<Task<T>> action, CancellationToken ct) => attendance.WriteAsync(employee, action, ct);
}
