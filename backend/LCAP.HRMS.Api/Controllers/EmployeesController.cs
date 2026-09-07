using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Application.Employees.DTOs;
using LCAP.HRMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LCAP.HRMS.Api.Controllers;

/// <summary>Employee directory, organisation assignments and reporting hierarchy.</summary>
/// <remarks>JWT role, company_id and employee_id claims control access. Lists omit statutory and bank fields. HRAdmin/SuperAdmin write; HRAdmin/SuperAdmin/PayrollAdmin may read sensitive details.</remarks>
[ApiController, Route("api/employees"), Authorize(Roles="SuperAdmin,HRAdmin,HRUser,Manager,Employee,PayrollAdmin")]
[Produces("application/json")]
[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ApiResponse<object>),400)]
[ProducesResponseType(typeof(ApiResponse<object>),401)]
[ProducesResponseType(typeof(ApiResponse<object>),403)]
[ProducesResponseType(typeof(ApiResponse<object>),404)]
[ProducesResponseType(typeof(ApiResponse<object>),409)]
public sealed class EmployeesController(IEmployeeService service,IEmployeeAccess access) : ControllerBase
{
    /// <summary>Searches employees with bounded paging; no statutory or bank identifiers are returned.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeListResponse>>),200)]
    public async Task<IActionResult> List(Guid? companyId,Guid? branchId,Guid? departmentId,string? search,EmployeeStatus? status,int skip=0,int take=100,CancellationToken cancellationToken=default) =>
        Ok(ApiResponse<IReadOnlyList<EmployeeListResponse>>.Ok(await service.ListAsync(new(CompanyId:companyId,BranchId:branchId,DepartmentId:departmentId,Search:search,Status:status,Skip:skip,Take:take),cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Lists employees by company within the caller's scope.</summary>
    [HttpGet("/api/companies/{companyId:guid}/employees")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeListResponse>>),200)]
    public async Task<IActionResult> ByCompany(Guid companyId,int skip=0,int take=100,CancellationToken cancellationToken=default) =>
        Ok(ApiResponse<IReadOnlyList<EmployeeListResponse>>.Ok(await service.ListAsync(new(CompanyId:companyId,Skip:skip,Take:take),cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Lists employees by branch within the caller's scope.</summary>
    [HttpGet("/api/branches/{branchId:guid}/employees")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeListResponse>>),200)]
    public async Task<IActionResult> ByBranch(Guid branchId,int skip=0,int take=100,CancellationToken cancellationToken=default) =>
        Ok(ApiResponse<IReadOnlyList<EmployeeListResponse>>.Ok(await service.ListAsync(new(BranchId:branchId,Skip:skip,Take:take),cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Lists employees by department within the caller's scope.</summary>
    [HttpGet("/api/departments/{departmentId:guid}/employees")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeListResponse>>),200)]
    public async Task<IActionResult> ByDepartment(Guid departmentId,int skip=0,int take=100,CancellationToken cancellationToken=default) =>
        Ok(ApiResponse<IReadOnlyList<EmployeeListResponse>>.Ok(await service.ListAsync(new(DepartmentId:departmentId,Skip:skip,Take:take),cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Returns a minimal employee lookup, subject to company and role scope.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeLookupResponse>>),200)]
    public async Task<IActionResult> Lookup(Guid? companyId,int skip=0,int take=100,CancellationToken cancellationToken=default) =>
        Ok(ApiResponse<IReadOnlyList<EmployeeLookupResponse>>.Ok(await service.LookupAsync(new(CompanyId:companyId,Skip:skip,Take:take),cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Gets an employee profile; sensitive fields are null for non-privileged readers.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDetailResponse>),200)]
    public async Task<IActionResult> Get(Guid id,CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDetailResponse>.Ok(await service.GetAsync(id,cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Creates an employee. Requires HRAdmin or SuperAdmin and consistent assignments.</summary>
    [HttpPost, Authorize(Roles="HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDetailResponse>),201)]
    public async Task<IActionResult> Create(EmployeeCreateRequest request,CancellationToken cancellationToken)
    {
        var result=await service.CreateAsync(request,cancellationToken);
        return CreatedAtAction(nameof(Get),new{id=result.Id},ApiResponse<EmployeeDetailResponse>.Ok(result,HttpContext.TraceIdentifier));
    }
    /// <summary>Replaces writable employee fields. Reporting cycles and company transfers are rejected.</summary>
    [HttpPut("{id:guid}"), Authorize(Roles="HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDetailResponse>),200)]
    public async Task<IActionResult> Update(Guid id,EmployeeUpdateRequest request,CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDetailResponse>.Ok(await service.UpdateAsync(id,request,cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Soft-deletes an employee; managers must first have reports reassigned.</summary>
    [HttpDelete("{id:guid}"), Authorize(Roles="HRAdmin,SuperAdmin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id,CancellationToken cancellationToken) { await service.DeleteAsync(id,cancellationToken);return NoContent(); }
    /// <summary>Lists direct reports. A Manager may only request their own team.</summary>
    [HttpGet("{id:guid}/direct-reports")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeListResponse>>),200)]
    public async Task<IActionResult> DirectReports(Guid id,int skip=0,int take=100,CancellationToken cancellationToken=default) => Ok(ApiResponse<IReadOnlyList<EmployeeListResponse>>.Ok(await service.DirectReportsAsync(id,skip,take,cancellationToken),HttpContext.TraceIdentifier));
    /// <summary>Lists the authenticated manager's direct reports using employee_id.</summary>
    [HttpGet("my-team"), Authorize(Roles="Manager,HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeListResponse>>),200)]
    public Task<IActionResult> MyTeam(int skip=0,int take=100,CancellationToken cancellationToken=default) => access.EmployeeId is {} id ? DirectReports(id,skip,take,cancellationToken) : throw new LCAP.HRMS.Application.Common.Exceptions.ForbiddenException();
    /// <summary>Returns managers from immediate manager upwards; no sensitive identifiers.</summary>
    [HttpGet("{id:guid}/reporting-chain")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeLookupResponse>>),200)]
    public async Task<IActionResult> Chain(Guid id,CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyList<EmployeeLookupResponse>>.Ok(await service.ReportingChainAsync(id,cancellationToken),HttpContext.TraceIdentifier));
}
