using LCAP.HRMS.Domain.Attendance;
namespace LCAP.HRMS.Application.Attendance;

public interface IAttendanceRepository
{
    Task<AttendanceEmployee?> EmployeeAsync(Guid employeeId, CancellationToken ct);
    Task<AttendanceRecord?> OpenAsync(Guid employeeId, CancellationToken ct);
    Task<bool> DateExistsAsync(Guid employeeId, DateOnly date, CancellationToken ct);
    Task AddAsync(AttendanceRecord record, CancellationToken ct);
    Task<AttendanceResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid?> CompanyAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AttendanceResponse>> ListAsync(AttendanceQuery query, CancellationToken ct);
    Task<T> WriteAsync<T>(Guid employeeId, Func<Task<T>> action, CancellationToken ct);
}
public interface IAttendanceService
{
    Task<AttendanceResponse> CheckInAsync(AttendanceCheckInRequest request, string? userAgent, CancellationToken ct);
    Task<AttendanceResponse> CheckOutAsync(AttendanceCheckOutRequest request, CancellationToken ct);
    Task<AttendanceTodayResponse> TodayAsync(CancellationToken ct);
    Task<IReadOnlyList<AttendanceResponse>> MineAsync(AttendanceQuery query, CancellationToken ct);
    Task<IReadOnlyList<AttendanceResponse>> ListAsync(AttendanceQuery query, CancellationToken ct);
    Task<AttendanceResponse> GetAsync(Guid id, CancellationToken ct);
}
