using LCAP.HRMS.Domain.Common;
namespace LCAP.HRMS.Domain.Attendance;

public enum AttendanceStatus { CheckedIn = 1, Completed = 2 }
public sealed class AttendanceRecord : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid WorkLocationId { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string TimeZoneId { get; set; } = "";
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public decimal? CheckInAccuracyMeters { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public decimal? CheckOutAccuracyMeters { get; set; }
    public decimal? CheckInDistanceMeters { get; set; }
    public decimal? CheckOutDistanceMeters { get; set; }
    public bool? CheckInWithinGeofence { get; set; }
    public bool? CheckOutWithinGeofence { get; set; }
    public AttendanceStatus Status { get; set; }
    public string CheckInSource { get; set; } = "";
    public string? CheckOutSource { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? UserAgent { get; set; }
    public string? Notes { get; set; }
    public Companies.Company Company { get; set; } = null!;
    public Employees.Employee Employee { get; set; } = null!;
    public WorkLocations.WorkLocation WorkLocation { get; set; } = null!;
    public Shifts.Shift Shift { get; set; } = null!;
}
