using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Domain.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LCAP.HRMS.Api.Controllers;

/// <summary>Self-service geo-fenced attendance and scoped, read-only HR attendance lists.</summary>
/// <remarks>Identity is derived from signed employee_id/company_id claims. Requests cannot supply employee or company IDs. Server UTC time is authoritative. Geolocation evidence is never returned by these endpoints.</remarks>
[ApiController, Route("api/attendance"), Authorize(Roles = "Employee,Manager,HRUser,PayrollAdmin,HRAdmin,SuperAdmin")]
[Produces("application/json"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ApiResponse<object>), 400)]
[ProducesResponseType(typeof(ApiResponse<object>), 401)]
[ProducesResponseType(typeof(ApiResponse<object>), 403)]
[ProducesResponseType(typeof(ApiResponse<object>), 409)]
public sealed class AttendanceController(IAttendanceService service) : ControllerBase
{
    /// <summary>Checks in the authenticated employee. Rejects poor GPS, outside geofence and duplicates.</summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), 201)]
    public async Task<IActionResult> CheckIn(AttendanceCheckInRequest request, CancellationToken cancellationToken)
    {
        var row = await service.CheckInAsync(request, Request.Headers.UserAgent.ToString(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = row.AttendanceId }, ApiResponse<AttendanceResponse>.Ok(row, HttpContext.TraceIdentifier, "Check-in recorded."));
    }
    /// <summary>Completes the employee's open record, including a check-in from the previous night.</summary>
    [HttpPost("check-out")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), 200)]
    public async Task<IActionResult> CheckOut(AttendanceCheckOutRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AttendanceResponse>.Ok(await service.CheckOutAsync(request, cancellationToken), HttpContext.TraceIdentifier, "Check-out recorded."));
    /// <summary>Gets the resolved attendance day and its record, or an outstanding open record.</summary>
    [HttpGet("me/today")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceTodayResponse>), 200)]
    public async Task<IActionResult> Today(CancellationToken cancellationToken) => Ok(ApiResponse<AttendanceTodayResponse>.Ok(await service.TodayAsync(cancellationToken), HttpContext.TraceIdentifier));
    /// <summary>Gets a bounded page of the authenticated employee's attendance.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AttendanceResponse>>), 200)]
    public async Task<IActionResult> Mine(DateOnly? date, DateOnly? fromDate, DateOnly? toDate, AttendanceStatus? status, int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<AttendanceResponse>>.Ok(await service.MineAsync(new(Date: date, FromDate: fromDate, ToDate: toDate, Status: status, Skip: skip, Take: take), cancellationToken), HttpContext.TraceIdentifier));
    /// <summary>Reads own attendance or an HR-authorized attendance record. Does not return GPS coordinates.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<AttendanceResponse>.Ok(await service.GetAsync(id, cancellationToken), HttpContext.TraceIdentifier));
    /// <summary>HRAdmin/SuperAdmin read-only attendance search; HRAdmin is scoped to company_id.</summary>
    [HttpGet, Authorize(Roles = "HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AttendanceResponse>>), 200)]
    public async Task<IActionResult> List(DateOnly? date, DateOnly? fromDate, DateOnly? toDate, Guid? employeeId, Guid? branchId, AttendanceStatus? status, int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<AttendanceResponse>>.Ok(await service.ListAsync(new(date, fromDate, toDate, employeeId, branchId, status, skip, take), cancellationToken), HttpContext.TraceIdentifier));
}
