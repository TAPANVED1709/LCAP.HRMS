using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Shifts;
using LCAP.HRMS.Application.Shifts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

/// <summary>Maintains shifts and their owning company.</summary>
[ApiController]
[Authorize]
[Route("api/shifts")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public sealed class ShiftsController(IShiftService service) : ControllerBase
{
    /// <summary>Lists shifts across companies.</summary>
    /// <remarks>Returns a page ordered by Id, including inactive shifts. Deleted shifts and shifts of deleted companies are hidden.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShiftResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShiftResponse>>>> List(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<ShiftResponse>>.Ok(
            await service.ListAsync(skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Lists shifts belonging to a company.</summary>
    /// <remarks>Returns 404 for a missing or deleted company; returns an empty array when an existing company has no shifts.</remarks>
    [HttpGet("/api/companies/{companyId:guid}/shifts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShiftResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShiftResponse>>>> ListByCompany(Guid companyId,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<ShiftResponse>>.Ok(
            await service.ListByCompanyAsync(companyId, skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Gets a shift by ID.</summary>
    /// <remarks>Returns 404 for a missing shift, deleted shift, or deleted owning company.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShiftResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShiftResponse>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShiftResponse>.Ok(await service.GetByIdAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Creates a shift within an existing company.</summary>
    /// <remarks>
    /// CompanyId must be a non-empty ID of a non-deleted company. ShiftCode and ShiftName are required.
    /// Codes are trimmed, uppercased, and unique within the company, including deleted shifts.
    /// StartTime and EndTime are required local times in HH:mm:ss format; equal times are invalid.
    /// An earlier EndTime means the next day and automatically sets IsNightShift to true.
    /// GracePeriodMinutes is non-negative. Half/full-day thresholds are nullable non-negative minutes; half cannot exceed full.
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShiftResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShiftResponse>>> Create(ShiftCreateRequest request, CancellationToken cancellationToken)
    {
        var shift = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = shift.Id },
            ApiResponse<ShiftResponse>.Ok(shift, HttpContext.TraceIdentifier, "Shift created successfully."));
    }

    /// <summary>Replaces a shift's writable details.</summary>
    /// <remarks>
    /// Supply the full writable representation. CompanyId may be changed to an existing, non-deleted company;
    /// the shift code must be available there. Omitted thresholds are cleared.
    /// Optional omitted fields are cleared. Audit fields and deletion state cannot be supplied.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShiftResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShiftResponse>>> Update(Guid id, ShiftUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShiftResponse>.Ok(await service.UpdateAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier, "Shift updated successfully."));

    /// <summary>Soft-deletes a shift.</summary>
    /// <remarks>Preserves the row and stamps audit fields. Returns 204 without a body, or 404 if missing or already hidden.</remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
