using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.WorkLocations;
using LCAP.HRMS.Application.WorkLocations.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

/// <summary>Maintains branch work locations and geofence configuration.</summary>
[ApiController]
[Authorize]
[Route("api/work-locations")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public sealed class WorkLocationsController(IWorkLocationService service) : ControllerBase
{
    /// <summary>Lists work locations.</summary>
    /// <remarks>Ordered by Id. Includes inactive locations; excludes deleted locations, branches, and companies.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkLocationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkLocationResponse>>>> List(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<WorkLocationResponse>>.Ok(
            await service.ListAsync(skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Lists work locations belonging to a branch.</summary>
    /// <remarks>Returns 404 for a missing/deleted branch or company. An existing branch without locations returns an empty array.</remarks>
    [HttpGet("/api/branches/{branchId:guid}/work-locations")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkLocationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkLocationResponse>>>> ListByBranch(Guid branchId,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<WorkLocationResponse>>.Ok(
            await service.ListByBranchAsync(branchId, skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Gets a work location by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkLocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkLocationResponse>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkLocationResponse>.Ok(await service.GetByIdAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Creates a work location.</summary>
    /// <remarks>
    /// CompanyId must match the branch's company. LocationCode and LocationName are required.
    /// Codes are unique per branch, including soft-deleted records.
    /// Latitude (-90 to 90) and Longitude (-180 to 180) support seven decimal places.
    /// Supply both coordinates or leave both null; never substitute zero for unknown coordinates.
    /// AllowedRadiusMeters defaults to 100 and must be greater than zero.
    /// IsGeoFenceEnabled defaults to true. This is configuration only: null coordinates are not a usable geofence.
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<WorkLocationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<WorkLocationResponse>>> Create(WorkLocationCreateRequest request, CancellationToken cancellationToken)
    {
        var location = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = location.Id },
            ApiResponse<WorkLocationResponse>.Ok(location, HttpContext.TraceIdentifier, "Work location created successfully."));
    }

    /// <summary>Replaces a work location's writable fields.</summary>
    /// <remarks>PUT permits reassignment to a matching company/branch pair. Omitted coordinates are cleared, radius defaults to 100, and IsGeoFenceEnabled defaults to true. Audit fields are server-managed.</remarks>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<WorkLocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<WorkLocationResponse>>> Update(Guid id, WorkLocationUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkLocationResponse>.Ok(await service.UpdateAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier, "Work location updated successfully."));

    /// <summary>Soft-deletes a work location.</summary>
    /// <remarks>Preserves the physical row and stamps audit fields. Returns 404 if missing or already hidden.</remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
