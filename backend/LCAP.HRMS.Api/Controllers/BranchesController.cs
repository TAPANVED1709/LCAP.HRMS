using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Branches;
using LCAP.HRMS.Application.Branches.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

/// <summary>Maintains branches and their owning company.</summary>
[ApiController]
[Authorize]
[Route("api/branches")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public sealed class BranchesController(IBranchService service) : ControllerBase
{
    /// <summary>Lists branches across companies.</summary>
    /// <remarks>Returns a page ordered by Id, including inactive branches. Deleted branches and branches of deleted companies are hidden.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BranchResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchResponse>>>> List(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<BranchResponse>>.Ok(
            await service.ListAsync(skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Lists branches belonging to a company.</summary>
    /// <remarks>Returns 404 for a missing or deleted company; returns an empty array when an existing company has no branches.</remarks>
    [HttpGet("/api/companies/{companyId:guid}/branches")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BranchResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchResponse>>>> ListByCompany(Guid companyId,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<BranchResponse>>.Ok(
            await service.ListByCompanyAsync(companyId, skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Gets a branch by ID.</summary>
    /// <remarks>Returns 404 for a missing branch, deleted branch, or deleted owning company.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BranchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BranchResponse>.Ok(await service.GetByIdAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Creates a branch within an existing company.</summary>
    /// <remarks>
    /// CompanyId must be a non-empty ID of a non-deleted company. BranchCode and BranchName are required.
    /// Codes are trimmed, uppercased, and unique within the company, including deleted branches.
    /// Blank or omitted State inherits the company's State. Country defaults to India.
    /// Email and Phone are optional and validated when supplied.
    /// IsHeadOffice is a flag; no one-head-office restriction is imposed.
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<BranchResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> Create(BranchCreateRequest request, CancellationToken cancellationToken)
    {
        var branch = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = branch.Id },
            ApiResponse<BranchResponse>.Ok(branch, HttpContext.TraceIdentifier, "Branch created successfully."));
    }

    /// <summary>Replaces a branch's writable details.</summary>
    /// <remarks>
    /// Supply the full writable representation. CompanyId may be changed to an existing, non-deleted company;
    /// the branch code must be available there. Blank State inherits from the target company.
    /// Company reassignment returns 409 if this branch has work-location records, including deleted ones.
    /// Optional omitted fields are cleared. Audit fields and deletion state cannot be supplied.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<BranchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> Update(Guid id, BranchUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<BranchResponse>.Ok(await service.UpdateAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier, "Branch updated successfully."));

    /// <summary>Soft-deletes a branch.</summary>
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
