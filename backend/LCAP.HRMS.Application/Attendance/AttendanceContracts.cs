using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Domain.Enums;

namespace LCAP.HRMS.Application.Attendance;
public abstract class AttendancePositionRequest
{
    [Required, Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    [Required, Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }

    [Required, Range(typeof(decimal), "0.001", "999999999")]
    public decimal? AccuracyMeters { get; set; }

    [MaxLength(200)]
    public string? DeviceIdentifier { get; set; }
    public DateTimeOffset? ClientTimestamp { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AttendanceCheckInRequest : AttendancePositionRequest;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AttendanceCheckOutRequest : AttendancePositionRequest;
public sealed class AttendanceResponse
{
    public LCAP.HRMS.Application.Regularisation.EffectiveAttendance? Effective { get; set; }
    public LCAP.HRMS.Application.AttendancePolicies.EvaluationResponse? Evaluation { get; set; }
    public string ShiftName { get; set; } = "";
    public Guid AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string Branch { get; set; } = "";
    public DateOnly AttendanceDate { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public string WorkLocationName { get; set; } = "";
    public decimal? DistanceMeters { get; set; }
    public bool? WithinGeofence { get; set; }
    public AttendanceStatus Status { get; set; }
}

public sealed record AttendanceTodayResponse(DateOnly AttendanceDate, string TimeZoneId, AttendanceResponse? Record);
public sealed record AttendanceQuery(DateOnly? Date = null, DateOnly? FromDate = null, DateOnly? ToDate = null, Guid? EmployeeId = null, Guid? BranchId = null, AttendanceStatus? Status = null, int Skip = 0, int Take = 100, Guid? CompanyId = null, bool LateOnly = false, bool PenaltyOnly = false);
// Attendance queries deliberately avoid loading statutory, bank and contact details.
public sealed class AttendanceEmployee
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WorkLocationId { get; set; }
    public Guid ShiftId { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
    public EmployeeStatus Status { get; set; }
    public bool OrganisationActive { get; set; }
    public bool AssignmentsValid { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool LocationActive { get; set; }
    public bool GeofenceEnabled { get; set; }
    public decimal? OfficeLatitude { get; set; }
    public decimal? OfficeLongitude { get; set; }
    public int RadiusMeters { get; set; }
}
