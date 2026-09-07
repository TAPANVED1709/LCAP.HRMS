using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LCAP.HRMS.Api.Controllers;
/// <summary>Company-scoped attendance policy management. Policies describe events, never salary deductions.</summary>
[ApiController, Route("api/attendance-policies"), Authorize(Roles = "HRAdmin,SuperAdmin"), Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), 400), ProducesResponseType(typeof(ApiResponse<object>), 401), ProducesResponseType(typeof(ApiResponse<object>), 403), ProducesResponseType(typeof(ApiResponse<object>), 409)]
public sealed class AttendancePoliciesController(IAttendancePolicyService service) : ControllerBase
{
    /// <summary>Gets a bounded, company-scoped policy page.</summary>
    [HttpGet, ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AttendancePolicy>>), 200)]
    public async Task<IActionResult> List(Guid? companyId, int skip = 0, int take = 100, CancellationToken cancellationToken = default) => Ok(ApiResponse<IReadOnlyList<AttendancePolicy>>.Ok(await service.ListAsync(new(companyId, skip, take), cancellationToken), HttpContext.TraceIdentifier));
    /// <summary>Gets a policy by ID.</summary>
    [HttpGet("{id:guid}"), ProducesResponseType(typeof(ApiResponse<AttendancePolicy>), 200), ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(ApiResponse<AttendancePolicy>.Ok(await service.GetAsync(id, ct), HttpContext.TraceIdentifier));
    /// <summary>Selects the company default policy effective on the required attendance date. Null means unconfigured.</summary>
    [HttpGet("current"), ProducesResponseType(typeof(ApiResponse<AttendancePolicy>), 200)]
    public async Task<IActionResult> Current([FromQuery] Guid companyId, [FromQuery, System.ComponentModel.DataAnnotations.Required] DateOnly? date, CancellationToken ct) => Ok(ApiResponse<AttendancePolicy?>.Ok(await service.CurrentAsync(companyId, date!.Value, ct), HttpContext.TraceIdentifier));
    /// <summary>Creates a policy; overlapping active default periods are rejected.</summary>
    [HttpPost, ProducesResponseType(typeof(ApiResponse<AttendancePolicy>), 201)]
    public async Task<IActionResult> Create(AttendancePolicyRequest r, CancellationToken ct) { var p = await service.SaveAsync(r, null, ct); return CreatedAtAction(nameof(Get), new { id = p.Id }, ApiResponse<AttendancePolicy>.Ok(p, HttpContext.TraceIdentifier, "Policy created.")); }
    /// <summary>Updates configuration/activation and increments revision without reevaluating history.</summary>
    [HttpPut("{id:guid}"), ProducesResponseType(typeof(ApiResponse<AttendancePolicy>), 200)]
    public async Task<IActionResult> Update(Guid id, AttendancePolicyRequest r, CancellationToken ct) => Ok(ApiResponse<AttendancePolicy>.Ok(await service.SaveAsync(r, id, ct), HttpContext.TraceIdentifier, "Policy updated."));
    /// <summary>Soft-deletes and deactivates a policy. Historical references are retained.</summary>
    [HttpDelete("{id:guid}"), ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteAsync(id, ct); return NoContent(); }
}

