using LCAP.HRMS.Application.Regularisation;
using LCAP.HRMS.Domain.Regularisation;
using System.Data;
using System.Linq.Expressions;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Domain.Attendance;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;
public sealed class AttendanceRepository(ApplicationDbContext context) : IAttendanceRepository
{
    public Task<AttendanceEmployee?> EmployeeAsync(Guid id, CancellationToken ct) => context.Employees.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == id).Select(e => new AttendanceEmployee { Id = e.Id, CompanyId = e.CompanyId, BranchId = e.BranchId, WorkLocationId = e.WorkLocationId, ShiftId = e.ShiftId, IsDeleted = e.IsDeleted, IsActive = e.IsActive, Status = e.EmployeeStatus, OrganisationActive = !e.Company.IsDeleted && e.Company.IsActive && !e.Branch.IsDeleted && e.Branch.IsActive && !e.Shift.IsDeleted && e.Shift.IsActive, AssignmentsValid = e.Branch.CompanyId == e.CompanyId && e.Shift.CompanyId == e.CompanyId && e.WorkLocation.CompanyId == e.CompanyId && e.WorkLocation.BranchId == e.BranchId, StartTime = e.Shift.StartTime, EndTime = e.Shift.EndTime, LocationActive = !e.WorkLocation.IsDeleted && e.WorkLocation.IsActive, GeofenceEnabled = e.WorkLocation.IsGeoFenceEnabled, OfficeLatitude = e.WorkLocation.Latitude, OfficeLongitude = e.WorkLocation.Longitude, RadiusMeters = e.WorkLocation.AllowedRadiusMeters }).SingleOrDefaultAsync(ct);
    public Task<AttendanceRecord?> OpenAsync(Guid employeeId, CancellationToken ct) => context.AttendanceRecords.SingleOrDefaultAsync(r => r.EmployeeId == employeeId && r.CheckOutTime == null && !context.AttendanceCorrections.Any(c => c.AttendanceRecordId == r.Id && c.CorrectedCheckOutTime != null), ct);
    public async Task<bool> DateExistsAsync(Guid employeeId, DateOnly date, CancellationToken ct) => await context.AttendanceRecords.IgnoreQueryFilters().AnyAsync(r => r.EmployeeId == employeeId && r.AttendanceDate == date, ct) || await context.AttendanceCorrections.AnyAsync(c => c.EmployeeId == employeeId && c.AttendanceDate == date, ct);
    public async Task AddAsync(AttendanceRecord record, CancellationToken ct) => await context.AttendanceRecords.AddAsync(record, ct);
    private IQueryable<AttendanceRecord> History => context.AttendanceRecords.IgnoreQueryFilters().AsNoTracking().Where(r => !r.IsDeleted);

    private static readonly Expression<Func<AttendanceRecord, AttendanceResponse>> Projection = r => new AttendanceResponse
    {
        AttendanceId = r.Id,
        ShiftName = r.Shift.ShiftName,
        EmployeeId = r.EmployeeId,
        EmployeeCode = r.Employee.EmployeeCode,
        EmployeeName = r.Employee.FirstName + (r.Employee.MiddleName == null ? "" : " " + r.Employee.MiddleName) + (r.Employee.LastName == null ? "" : " " + r.Employee.LastName),
        Branch = r.WorkLocation.Branch.BranchName,
        AttendanceDate = r.AttendanceDate,
        TimeZoneId = r.TimeZoneId,
        CheckInTime = r.CheckInTime,
        CheckOutTime = r.CheckOutTime,
        WorkLocationName = r.WorkLocation.LocationName,
        DistanceMeters = r.CheckOutTime == null ? r.CheckInDistanceMeters : r.CheckOutDistanceMeters,
        WithinGeofence = r.CheckOutTime == null ? r.CheckInWithinGeofence : r.CheckOutWithinGeofence,
        Status = r.Status
    };
    public async Task<AttendanceResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var row = await History.Where(r => r.Id == id).Select(Projection).SingleOrDefaultAsync(ct);
        if (row is not null)
            await Enrich([row], ct);
        return row;
    }

    private async Task Enrich(IReadOnlyList<AttendanceResponse> rows, CancellationToken ct)
    {
        var ids = rows.Select(r => r.AttendanceId).ToArray();
        var evaluations = await context.AttendanceEvaluations.IgnoreQueryFilters().AsNoTracking().Where(e => ids.Contains(e.AttendanceRecordId)).Select(AttendanceEvaluationRepository.Projection).ToDictionaryAsync(e => e.AttendanceRecordId, ct);
        var corrections = (await context.AttendanceCorrections.IgnoreQueryFilters().AsNoTracking().Where(c => c.AttendanceRecordId != null && ids.Contains(c.AttendanceRecordId.Value) && c.IsActive).ToListAsync(ct)).GroupBy(c => c.AttendanceRecordId!.Value).ToDictionary(g => g.Key, g => g.MaxBy(c => c.Version)!);
        var revisions = (await context.AttendanceEvaluationRevisions.IgnoreQueryFilters().AsNoTracking().Where(v => v.AttendanceRecordId != null && ids.Contains(v.AttendanceRecordId.Value)).ToListAsync(ct)).GroupBy(v => v.AttendanceRecordId!.Value).ToDictionary(g => g.Key, g => g.MaxBy(v => v.Version)!);
        foreach (var row in rows)
        {
            row.Evaluation = evaluations.GetValueOrDefault(row.AttendanceId);
            var c = corrections.GetValueOrDefault(row.AttendanceId);
            var v = revisions.GetValueOrDefault(row.AttendanceId);
            row.Effective = new(row.EmployeeId, row.AttendanceDate, row.TimeZoneId, row.AttendanceId, row.CheckInTime, row.CheckOutTime, c is null ? row.CheckInTime : c.CorrectedCheckInTime, c is null ? row.CheckOutTime : c.CorrectedCheckOutTime, c is null ? AttendanceSource.Raw : AttendanceSource.Regularised, c?.RegularisationRequestId, c?.Id, v is null ? row.Evaluation : EvaluationSnapshots.Response(EvaluationSnapshots.Read(v)), v?.PolicyImpactReviewRequired ?? false);
        }
    }

    public Task<Guid?> CompanyAsync(Guid id, CancellationToken ct) => History.Where(r => r.Id == id).Select(r => (Guid? )r.CompanyId).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<AttendanceResponse>> ListAsync(AttendanceQuery q, CancellationToken ct)
    {
        var query = History;
        if (q.LateOnly)
            query = query.Where(r => context.AttendanceEvaluations.Any(e => e.AttendanceRecordId == r.Id && e.IsLate));
        if (q.PenaltyOnly)
            query = query.Where(r => context.AttendanceEvaluations.Any(e => e.AttendanceRecordId == r.Id && e.PenaltyTriggered));
        if (q.CompanyId is { } company)
            query = query.Where(r => r.CompanyId == company);
        if (q.EmployeeId is { } employee)
            query = query.Where(r => r.EmployeeId == employee);
        if (q.BranchId is { } branch)
            query = query.Where(r => r.WorkLocation.BranchId == branch);
        if (q.Date is { } date)
            query = query.Where(r => r.AttendanceDate == date);
        if (q.FromDate is { } from)
            query = query.Where(r => r.AttendanceDate >= from);
        if (q.ToDate is { } to)
            query = query.Where(r => r.AttendanceDate <= to);
        if (q.Status is { } status)
            query = query.Where(r => r.Status == status);
        var rows = await query.OrderByDescending(r => r.AttendanceDate).ThenBy(r => r.Id).Skip(q.Skip).Take(q.Take).Select(Projection).ToListAsync(ct);
        await Enrich(rows, ct);
        return rows;
    }

    public async Task<T> WriteAsync<T>(Guid employeeId, Func<Task<T>> action, CancellationToken ct) => await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (context.Database.IsSqlServer())
        {
            var resource = "LCAP.Attendance." + employeeId;
            await context.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51000,'Attendance is busy. Retry the request.',1;", ct);
        }

        var result = await action();
        await transaction.CommitAsync(ct);
        return result;
    });
}
