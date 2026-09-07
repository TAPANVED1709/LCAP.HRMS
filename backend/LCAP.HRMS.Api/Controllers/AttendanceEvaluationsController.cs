using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.AttendancePolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LCAP.HRMS.Api.Controllers;
/// <summary>Read-only late evaluation and pending penalty events. No manual editing or payroll consumption.</summary>
[ApiController, Authorize(Roles = "Employee,Manager,HRUser,PayrollAdmin,HRAdmin,SuperAdmin"), Produces("application/json"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ApiResponse<object>), 400), ProducesResponseType(typeof(ApiResponse<object>), 401), ProducesResponseType(typeof(ApiResponse<object>), 403), ProducesResponseType(typeof(ApiResponse<object>), 404)]
public sealed class AttendanceEvaluationsController(IAttendanceEvaluationService service) : ControllerBase
{
    /// <summary>Gets own or company-authorized immutable evaluation; null means not yet evaluated.</summary>
    [HttpGet("api/attendance/{attendanceId:guid}/evaluation"), ProducesResponseType(typeof(ApiResponse<EvaluationResponse>), 200)]
    public async Task<IActionResult> Get(Guid attendanceId, CancellationToken ct) => Ok(ApiResponse<EvaluationResponse?>.Ok(await service.GetAsync(attendanceId, ct), HttpContext.TraceIdentifier));
    /// <summary>HR search for evaluations, including date range, employee, branch, late and penalty filters.</summary>
    [HttpGet("api/attendance/evaluations"), Authorize(Roles = "HRAdmin,SuperAdmin"), ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvaluationResponse>>), 200)]
    public async Task<IActionResult> List([FromQuery] EvaluationQuery q, CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<EvaluationResponse>>.Ok(await service.ListAsync(q, ct), HttpContext.TraceIdentifier));
    /// <summary>Gets own or HR-authorized sequence state under the currently effective policy.</summary>
    [HttpGet("api/employees/{employeeId:guid}/late-summary"), ProducesResponseType(typeof(ApiResponse<LateSummary>), 200)]
    public async Task<IActionResult> Summary(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<LateSummary>.Ok(await service.SummaryAsync(employeeId, ct), HttpContext.TraceIdentifier));
    /// <summary>Company-scoped HR penalty-event register. Values are not salary calculations.</summary>
    [HttpGet("api/attendance-penalties"), Authorize(Roles = "HRAdmin,SuperAdmin"), ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PenaltyResponse>>), 200)]
    public async Task<IActionResult> Penalties([FromQuery] EvaluationQuery q, CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<PenaltyResponse>>.Ok(await service.PenaltiesAsync(q, ct), HttpContext.TraceIdentifier));
    /// <summary>Reads own or HR-authorized penalty event; exact GPS and monetary values are omitted.</summary>
    [HttpGet("api/attendance-penalties/{id:guid}"), ProducesResponseType(typeof(ApiResponse<PenaltyResponse>), 200)]
    public async Task<IActionResult> Penalty(Guid id, CancellationToken ct) => Ok(ApiResponse<PenaltyResponse>.Ok(await service.PenaltyAsync(id, ct), HttpContext.TraceIdentifier));
    /// <summary>Gets own or HR-authorized employee penalty history.</summary>
    [HttpGet("api/employees/{employeeId:guid}/attendance-penalties"), ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PenaltyResponse>>), 200)]
    public async Task<IActionResult> EmployeePenalties(Guid employeeId, int skip = 0, int take = 100, CancellationToken ct = default) => Ok(ApiResponse<IReadOnlyList<PenaltyResponse>>.Ok(await service.PenaltiesAsync(new(EmployeeId: employeeId, Skip: skip, Take: take), ct), HttpContext.TraceIdentifier));
}

