using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Departments;
using LCAP.HRMS.Application.Departments.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

/// <summary>Maintains departments and their owning company.</summary>
[ApiController]
[Authorize]
[Route("api/departments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public sealed class DepartmentsController(IDepartmentService service) : ControllerBase
{
    /// <summary>Lists departments across companies.</summary>
    /// <remarks>Returns a page ordered by Id, including inactive departments. Deleted departments and departments of deleted companies are hidden.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentResponse>>>> List(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<DepartmentResponse>>.Ok(
            await service.ListAsync(skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Lists departments belonging to a company.</summary>
    /// <remarks>Returns 404 for a missing or deleted company; returns an empty array when an existing company has no departments.</remarks>
    [HttpGet("/api/companies/{companyId:guid}/departments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentResponse>>>> ListByCompany(Guid companyId,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<DepartmentResponse>>.Ok(
            await service.ListByCompanyAsync(companyId, skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Gets a department by ID.</summary>
    /// <remarks>Returns 404 for a missing department, deleted department, or deleted owning company.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DepartmentResponse>.Ok(await service.GetByIdAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Creates a department within an existing company.</summary>
    /// <remarks>
    /// CompanyId must be a non-empty ID of a non-deleted company. DepartmentCode and DepartmentName are required.
    /// Codes are trimmed, uppercased, and unique within the company, including deleted departments.
    /// ParentDepartmentId is optional, must reference the same company, and cannot create a hierarchy cycle.
    /// DepartmentName is required. Description is optional.
    /// The owning company is immutable after creation.
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> Create(DepartmentCreateRequest request, CancellationToken cancellationToken)
    {
        var department = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = department.Id },
            ApiResponse<DepartmentResponse>.Ok(department, HttpContext.TraceIdentifier, "Department created successfully."));
    }

    /// <summary>Replaces a department's writable details.</summary>
    /// <remarks>
    /// Supply the full writable representation. CompanyId must remain the owning company;
    /// the department code must remain unique there. Null ParentDepartmentId makes this a root department.
    /// Optional omitted fields are cleared. Audit fields and deletion state cannot be supplied.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> Update(Guid id, DepartmentUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DepartmentResponse>.Ok(await service.UpdateAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier, "Department updated successfully."));

    /// <summary>Soft-deletes a department.</summary>
    /// <remarks>Preserves the row and stamps audit fields. Returns 409 if non-deleted children exist; reassign or delete them first. Returns 404 if missing or already hidden.</remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
