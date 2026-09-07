using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Companies.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

/// <summary>Maintains company details, statutory identifiers, and payroll scheduling settings.</summary>
[ApiController]
[Authorize]
[Route("api/companies")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public sealed class CompaniesController(ICompanyService service) : ControllerBase
{
    /// <summary>Lists non-deleted companies.</summary>
    /// <remarks>Returns a page ordered by Id. Inactive companies are included. Requires a bearer token.</remarks>
    /// <param name="skip">Number of rows to skip; defaults to 0.</param>
    /// <param name="take">Page size between 1 and 1000; defaults to 100.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CompanyResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CompanyResponse>>>> List(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 1000)] int take = 100, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<CompanyResponse>>.Ok(
            await service.ListAsync(skip, take, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Gets a company by its ID.</summary>
    /// <remarks>Returns 404 for a missing or soft-deleted company.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CompanyResponse>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CompanyResponse>.Ok(await service.GetByIdAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    /// <summary>Creates a company.</summary>
    /// <remarks>
    /// CompanyCode and CompanyName are required. Codes are trimmed, uppercased, and unique even across deleted companies.
    /// Country defaults to India and PayrollCurrency defaults to INR.
    /// PayrollDay and SalaryPaymentDay are required integers from 1 through 31.
    /// PAN, TAN, GSTIN, and other unspecified statutory fields may be null.
    /// Example:
    /// {"companyCode":"EXAMPLE","companyName":"Example Company","state":"Bihar","country":"India","payrollCurrency":"INR","payrollDay":1,"salaryPaymentDay":7,"isActive":true}
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CompanyResponse>>> Create(
        CompanyCreateRequest request, CancellationToken cancellationToken)
    {
        var company = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = company.Id },
            ApiResponse<CompanyResponse>.Ok(company, HttpContext.TraceIdentifier, "Company created successfully."));
    }

    /// <summary>Replaces a company's writable fields.</summary>
    /// <remarks>
    /// Send the complete writable representation. Omitted optional fields are cleared.
    /// ID, audit fields, and deletion state cannot be changed through this request.
    /// PayrollDay and SalaryPaymentDay must be between 1 and 31.
    /// Returns 409 if CompanyCode belongs to another company, including a soft-deleted company.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CompanyResponse>>> Update(Guid id,
        CompanyUpdateRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CompanyResponse>.Ok(await service.UpdateAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier, "Company updated successfully."));

    /// <summary>Soft-deletes a company.</summary>
    /// <remarks>
    /// Sets IsDeleted and updates audit fields; the database row is retained.
    /// Returns 204 without a body on success, or 404 if already deleted or missing.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
