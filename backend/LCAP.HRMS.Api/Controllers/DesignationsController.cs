using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Designations;
using LCAP.HRMS.Application.Designations.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

/// <summary>Maintains designations and their owning company.</summary>
[ApiController]
[Authorize]
[Route("api/designations")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public sealed class DesignationsController(IDesignationService service) : ControllerBase
{
    /// <summary>Lists designations across companies.</summary>
    /// <remarks>Returns a page ordered by Id, including inactive designations. Deleted designations and designations of deleted companies are hidden.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DesignationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignationResponse>>>> List(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<DesignationResponse>>.Ok(
            await service.ListAsync(skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Lists designations belonging to a company.</summary>
    /// <remarks>Returns 404 for a missing or deleted company; returns an empty array when an existing company has no designations.</remarks>
    [HttpGet("/api/companies/{companyId:guid}/designations")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DesignationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignationResponse>>>> ListByCompany(Guid companyId,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<DesignationResponse>>.Ok(
            await service.ListByCompanyAsync(companyId, skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Gets a designation by ID.</summary>
    /// <remarks>Returns 404 for a missing designation, deleted designation, or deleted owning company.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DesignationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DesignationResponse>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DesignationResponse>.Ok(await service.GetByIdAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Creates a designation within an existing company.</summary>
    /// <remarks>
    /// CompanyId must be a non-empty ID of a non-deleted company. DesignationCode and DesignationName are required.
    /// Codes are trimmed, uppercased, and unique within the company, including deleted designations.
    /// Grade and Description are optional text. Level is a nullable integer.
    /// IsManagerial defaults to false.
    /// No grade or level is inferred from the designation title.
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<DesignationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DesignationResponse>>> Create(DesignationCreateRequest request, CancellationToken cancellationToken)
    {
        var designation = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = designation.Id },
            ApiResponse<DesignationResponse>.Ok(designation, HttpContext.TraceIdentifier, "Designation created successfully."));
    }

    /// <summary>Replaces a designation's writable details.</summary>
    /// <remarks>
    /// Supply the full writable representation. CompanyId may be changed to an existing, non-deleted company;
    /// the designation code must be available there. Omitted Level and Grade are cleared.
    /// Optional omitted fields are cleared. Audit fields and deletion state cannot be supplied.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<DesignationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DesignationResponse>>> Update(Guid id, DesignationUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DesignationResponse>.Ok(await service.UpdateAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier, "Designation updated successfully."));

    /// <summary>Soft-deletes a designation.</summary>
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
