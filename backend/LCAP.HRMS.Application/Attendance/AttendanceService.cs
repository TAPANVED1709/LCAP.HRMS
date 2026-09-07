using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Domain.Enums;
using Microsoft.Extensions.Logging;
namespace LCAP.HRMS.Application.Attendance;

public sealed class AttendanceService(IAttendanceRepository repository, IUnitOfWork unitOfWork, IEmployeeAccess access,
    IGeoDistanceService distance, AttendanceOptions options, AttendanceDateResolver dates, TimeProvider time,
    ILogger<AttendanceService> logger) : IAttendanceService
{
    private Guid SelfId() => access.EmployeeId ?? throw new ForbiddenException();
    private async Task<AttendanceEmployee> Self(CancellationToken ct, bool writing = false)
    {
        var e = await repository.EmployeeAsync(SelfId(), ct);
        if (e is null || e.IsDeleted) throw new ForbiddenException();
        if (access.CompanyId is null || access.CompanyId != e.CompanyId) throw new ForbiddenException();
        if (writing)
        {
            if (!e.IsActive || e.Status is not (EmployeeStatus.Active or EmployeeStatus.OnNotice)) throw new ValidationException("Employee is not eligible to mark attendance.");
            if (!e.OrganisationActive || !e.AssignmentsValid || e.WorkLocationId == Guid.Empty || e.ShiftId == Guid.Empty)
                throw new ConflictException("Attendance assignments are unavailable. Please contact HR.");
            if (!e.LocationActive) throw new ConflictException("Your work location is not active. Please contact HR.");
        }
        return e;
    }
    private TimeZoneInfo Zone(AttendanceEmployee e)
    {
        try { return dates.Zone(e.CompanyId, e.BranchId); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        { logger.LogWarning("Attendance configuration unavailable for company {CompanyId}. Reason: Timezone", e.CompanyId); throw new ConflictException("Attendance configuration is unavailable. Please contact HR."); }
    }
    private (decimal? Distance, bool? Within) ValidatePosition(AttendanceEmployee e, AttendancePositionRequest r, DateTimeOffset now)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(r, new ValidationContext(r), errors, true)) throw new ValidationException("Valid latitude, longitude and positive GPS accuracy are required.");
        if (options.MaximumGpsAccuracyMeters <= 0 || options.MaximumGpsAccuracyMeters > 999999999 || options.MaximumLocationAgeSeconds <= 0 || options.MaximumFutureClockSkewSeconds < 0)
        { logger.LogWarning("Attendance configuration unavailable. Reason: GPS thresholds"); throw new ConflictException("Attendance configuration is unavailable. Please contact HR."); }
        if (r.AccuracyMeters > options.MaximumGpsAccuracyMeters) throw new ValidationException("Location accuracy is insufficient. Please enable precise location and try again.");
        if (r.ClientTimestamp is { } timestamp && ((now - timestamp).TotalSeconds > options.MaximumLocationAgeSeconds || (timestamp - now).TotalSeconds > options.MaximumFutureClockSkewSeconds))
            throw new ValidationException("Location timestamp is stale or invalid. Please obtain a fresh location and try again.");
        if (!e.GeofenceEnabled) return (null, null); // GPS/accuracy still required; no distance enforcement.
        if (e.OfficeLatitude is null || e.OfficeLongitude is null || Math.Abs(e.OfficeLatitude.Value) > 90 || Math.Abs(e.OfficeLongitude.Value) > 180 || e.RadiusMeters <= 0)
        { logger.LogWarning("Attendance configuration unavailable for work location {WorkLocationId}. Reason: Geofence", e.WorkLocationId); throw new ConflictException("Attendance is not configured for your work location. Please contact HR."); }
        var metres = distance.DistanceMeters((double)e.OfficeLatitude, (double)e.OfficeLongitude, (double)r.Latitude!.Value, (double)r.Longitude!.Value);
        if (metres > e.RadiusMeters) throw new ValidationException("You are outside the permitted attendance area.");
        return ((decimal)metres, true); // Compare full precision before evidence rounding by SQL.
    }
    private async Task<AttendanceResponse> Write(string action, Func<Task<Guid>> operation, CancellationToken ct)
    {
        var employeeId = SelfId();
        try
        {
            var id = await repository.WriteAsync(employeeId, operation, ct);
            logger.LogInformation("Attendance {Action} accepted. Employee {EmployeeId}; record {AttendanceId}", action, employeeId, id);
            return await GetAsync(id, ct);
        }
        catch (Exception ex) when (ex is ValidationException or ConflictException or ForbiddenException)
        { logger.LogInformation("Attendance {Action} rejected. Employee {EmployeeId}; category {Category}", action, employeeId, ex.GetType().Name); throw; }
    }
    public Task<AttendanceResponse> CheckInAsync(AttendanceCheckInRequest r, string? userAgent, CancellationToken ct) => Write("CheckIn", async () =>
    {
        var e = await Self(ct, true); var now = time.GetUtcNow(); var zone = Zone(e); var evidence = ValidatePosition(e, r, now);
        var day = AttendanceDateResolver.Resolve(now, zone, e.StartTime, e.EndTime);
        if (await repository.OpenAsync(e.Id, ct) is not null) throw new ConflictException("You are already checked in. Complete check-out before checking in again.");
        if (await repository.DateExistsAsync(e.Id, day, ct)) throw new ConflictException("Attendance already exists for this attendance date.");
        var record = new AttendanceRecord
        {
            CompanyId = e.CompanyId,
            EmployeeId = e.Id,
            WorkLocationId = e.WorkLocationId,
            ShiftId = e.ShiftId,
            AttendanceDate = day,
            TimeZoneId = AttendanceDateResolver.DisplayZone(zone),
            CheckInTime = now,
            CheckInLatitude = r.Latitude,
            CheckInLongitude = r.Longitude,
            CheckInAccuracyMeters = r.AccuracyMeters,
            CheckInDistanceMeters = evidence.Distance,
            CheckInWithinGeofence = evidence.Within,
            Status = AttendanceStatus.CheckedIn,
            CheckInSource = "Browser",
            DeviceIdentifier = r.DeviceIdentifier?.Trim(),
            UserAgent = userAgent is null ? null : userAgent[..Math.Min(512, userAgent.Length)]
        };
        await repository.AddAsync(record, ct); await unitOfWork.SaveChangesAsync(ct); return record.Id;
    }, ct);
    public Task<AttendanceResponse> CheckOutAsync(AttendanceCheckOutRequest r, CancellationToken ct) => Write("CheckOut", async () =>
    {
        var e = await Self(ct, true);
        var record = await repository.OpenAsync(e.Id, ct) ?? throw new ConflictException("No open check-in exists. You may already have checked out.");
        if (record.CompanyId != e.CompanyId || record.WorkLocationId != e.WorkLocationId || record.ShiftId != e.ShiftId) throw new ConflictException("Attendance assignments have changed. Please contact HR.");
        var now = time.GetUtcNow(); var evidence = ValidatePosition(e, r, now);
        if (now < record.CheckInTime) throw new ConflictException("Server time is inconsistent. Please try again later.");
        record.CheckOutTime = now; record.CheckOutLatitude = r.Latitude; record.CheckOutLongitude = r.Longitude; record.CheckOutAccuracyMeters = r.AccuracyMeters;
        record.CheckOutDistanceMeters = evidence.Distance; record.CheckOutWithinGeofence = evidence.Within; record.CheckOutSource = "Browser"; record.Status = AttendanceStatus.Completed;
        await unitOfWork.SaveChangesAsync(ct); return record.Id;
    }, ct);
    public async Task<AttendanceTodayResponse> TodayAsync(CancellationToken ct)
    {
        var e = await Self(ct); var zone = Zone(e); var day = AttendanceDateResolver.Resolve(time.GetUtcNow(), zone, e.StartTime, e.EndTime);
        var open = await repository.OpenAsync(e.Id, ct);
        var record = open is null ? (await repository.ListAsync(new(Date: day, EmployeeId: e.Id, CompanyId: e.CompanyId, Take: 1), ct)).FirstOrDefault() : await repository.GetAsync(open.Id, ct);
        return new(record?.AttendanceDate ?? day, record?.TimeZoneId ?? AttendanceDateResolver.DisplayZone(zone), record);
    }
    private static void ValidateQuery(AttendanceQuery q)
    { if (q.Skip < 0 || q.Take is < 1 or > 1000 || q.FromDate > q.ToDate || (q.Status is { } s && !Enum.IsDefined(s))) throw new ValidationException("Invalid attendance filters or page."); }
    public async Task<IReadOnlyList<AttendanceResponse>> MineAsync(AttendanceQuery q, CancellationToken ct)
    { var e = await Self(ct); ValidateQuery(q); return await repository.ListAsync(q with { EmployeeId = e.Id, CompanyId = e.CompanyId }, ct); }
    public Task<IReadOnlyList<AttendanceResponse>> ListAsync(AttendanceQuery q, CancellationToken ct)
    {
        if (!access.CanWrite) throw new ForbiddenException(); ValidateQuery(q);
        if (!access.IsSuperAdmin) { if (access.CompanyId is not { } company) throw new ForbiddenException(); q = q with { CompanyId = company }; }
        return repository.ListAsync(q, ct);
    }
    public async Task<AttendanceResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var company = await repository.CompanyAsync(id, ct) ?? throw new NotFoundException("Attendance record was not found.");
        if (!access.IsSuperAdmin && (access.CompanyId is null || access.CompanyId != company)) throw new ForbiddenException();
        var row = await repository.GetAsync(id, ct) ?? throw new NotFoundException("Attendance record was not found.");
        if (!access.CanWrite && row.EmployeeId != SelfId()) throw new ForbiddenException();
        return row;
    }
}
