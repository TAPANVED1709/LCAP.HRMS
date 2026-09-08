using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Regularisation;
using LCAP.HRMS.Application.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;
/// <summary>Audited regularisation requests. Raw attendance is never directly edited.</summary>
[ApiController, Route("api/attendance-regularisations"), Authorize(Roles = "Employee,Manager,HRUser,HRAdmin,SuperAdmin,PayrollAdmin"), Produces("application/json"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ApiResponse<object>), 400), ProducesResponseType(typeof(ApiResponse<object>), 401), ProducesResponseType(typeof(ApiResponse<object>), 403), ProducesResponseType(typeof(ApiResponse<object>), 404), ProducesResponseType(typeof(ApiResponse<object>), 409)]
public sealed class AttendanceRegularisationsController(IRegularisationService service) : ControllerBase
{
    private IActionResult EnvelopeResult<T>(T data) => Ok(ApiResponse<T>.Ok(data, HttpContext.TraceIdentifier));
    /// <summary>Submit own request using JWT employee/company identity. Assigned manager is snapshotted.</summary>
    [HttpPost, ProducesResponseType(typeof(ApiResponse<RegularisationResponse>), 201)]
    public async Task<IActionResult> Submit(RegularisationCreateRequest body, CancellationToken ct)
    {
        var row = await service.SubmitAsync(body, ct);
        return CreatedAtAction(nameof(MineById), new { id = row.Id }, ApiResponse<RegularisationResponse>.Ok(row, HttpContext.TraceIdentifier));
    }

    /// <summary>Own request history.</summary>
    [HttpGet("me"), ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RegularisationResponse>>), 200)]
    public async Task<IActionResult> Mine([FromQuery] RegularisationQuery q, CancellationToken ct) => EnvelopeResult(await service.ListAsync("me", q, ct));
    /// <summary>Read one own request.</summary>
    [HttpGet("me/{id:guid}"), ProducesResponseType(typeof(ApiResponse<RegularisationResponse>), 200)]
    public async Task<IActionResult> MineById(Guid id, CancellationToken ct) => EnvelopeResult(await service.GetAsync(id, true, ct));
    /// <summary>Manager inbox using the fixed reviewer assignment, not the current reporting hierarchy.</summary>
    [HttpGet("my-team"), Authorize(Roles = "Manager"), ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RegularisationResponse>>), 200)]
    public async Task<IActionResult> Team([FromQuery] RegularisationQuery q, CancellationToken ct) => EnvelopeResult(await service.ListAsync("team", q, ct));
    /// <summary>Read-only HR register, company scoped except SuperAdmin.</summary>
    [HttpGet, Authorize(Roles = "HRAdmin,SuperAdmin"), ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RegularisationResponse>>), 200)]
    public async Task<IActionResult> All([FromQuery] RegularisationQuery q, CancellationToken ct) => EnvelopeResult(await service.ListAsync("admin", q, ct));
    /// <summary>Read own, assigned-manager, or HR-authorized request.</summary>
    [HttpGet("{id:guid}"), ProducesResponseType(typeof(ApiResponse<RegularisationResponse>), 200)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => EnvelopeResult(await service.GetAsync(id, false, ct));
    /// <summary>Approve and atomically apply a correction and append evaluation revisions. No salary deduction.</summary>
    [HttpPost("{id:guid}/approve"), Authorize(Roles = "Manager"), ProducesResponseType(typeof(ApiResponse<RegularisationResponse>), 200)]
    public async Task<IActionResult> Approve(Guid id, ReviewRequest body, CancellationToken ct) => EnvelopeResult(await service.ReviewAsync(id, true, body, ct));
    /// <summary>Reject once with mandatory remarks.</summary>
    [HttpPost("{id:guid}/reject"), Authorize(Roles = "Manager"), ProducesResponseType(typeof(ApiResponse<RegularisationResponse>), 200)]
    public async Task<IActionResult> Reject(Guid id, ReviewRequest body, CancellationToken ct) => EnvelopeResult(await service.ReviewAsync(id, false, body, ct));
    /// <summary>Cancel own pending request with a reason.</summary>
    [HttpPost("{id:guid}/cancel"), ProducesResponseType(typeof(ApiResponse<RegularisationResponse>), 200)]
    public async Task<IActionResult> Cancel(Guid id, CancelRequest body, CancellationToken ct) => EnvelopeResult(await service.CancelAsync(id, body, ct));
    /// <summary>Immutable audit trail visible to the same readers as the request.</summary>
    [HttpGet("{id:guid}/audit"), ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Audit(Guid id, CancellationToken ct) => EnvelopeResult(await service.AuditAsync(id, ct));
}

/// <summary>Read effective attendance without fabricating raw evidence.</summary>
[ApiController, Authorize(Roles = "Employee,Manager,HRUser,HRAdmin,SuperAdmin,PayrollAdmin"), Route("api/attendance/effective"), Produces("application/json"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AttendanceEffectiveController(IAttendanceEffectiveStateService service, IEmployeeAccess access) : ControllerBase
{
    /// <summary>Own effective attendance, including correction-only days and latest derived evaluation.</summary>
    [HttpGet("me"), ProducesResponseType(typeof(ApiResponse<EffectiveAttendance>), 200)]
    public async Task<IActionResult> Mine([FromQuery, System.ComponentModel.DataAnnotations.Required] DateOnly? date, CancellationToken ct) => Ok(ApiResponse<EffectiveAttendance>.Ok(await service.GetAsync(access.EmployeeId ?? throw new LCAP.HRMS.Application.Common.Exceptions.ForbiddenException(), date!.Value, ct), HttpContext.TraceIdentifier));
    /// <summary>HR read-only effective attendance by employee and date.</summary>
    [HttpGet, Authorize(Roles = "HRAdmin,SuperAdmin"), ProducesResponseType(typeof(ApiResponse<EffectiveAttendance>), 200)]
    public async Task<IActionResult> Get(Guid employeeId, [FromQuery, System.ComponentModel.DataAnnotations.Required] DateOnly? date, CancellationToken ct) => Ok(ApiResponse<EffectiveAttendance>.Ok(await service.GetAsync(employeeId, date!.Value, ct), HttpContext.TraceIdentifier));
}
