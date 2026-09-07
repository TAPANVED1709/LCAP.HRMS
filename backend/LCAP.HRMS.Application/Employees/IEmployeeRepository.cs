using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Employees.DTOs;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Enums;
namespace LCAP.HRMS.Application.Employees;

public sealed record EmployeeQuery(Guid? CompanyId = null, Guid? BranchId = null, Guid? DepartmentId = null,
    Guid? ManagerId = null, Guid? EmployeeId = null, string? Search = null, EmployeeStatus? Status = null, int Skip = 0, int Take = 100);
public interface IEmployeeRepository : IBaseRepository<Employee>
{
    Task<IReadOnlyList<EmployeeListResponse>> SearchAsync(EmployeeQuery query, CancellationToken ct);
    Task<IReadOnlyList<EmployeeLookupResponse>> LookupAsync(EmployeeQuery query, CancellationToken ct);
    Task<Employee?> DetailAsync(Guid id, CancellationToken ct);
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? exceptId, CancellationToken ct);
    Task<IReadOnlyList<(Guid Id, Guid? ManagerId)>> HierarchyAsync(Guid companyId, CancellationToken ct);
    Task<T> WriteAsync<T>(Func<Task<T>> action, CancellationToken ct);
}
