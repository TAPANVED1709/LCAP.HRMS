using LCAP.HRMS.Application.Employees.DTOs;
namespace LCAP.HRMS.Application.Employees;
public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeListResponse>> ListAsync(EmployeeQuery query, CancellationToken ct);
    Task<IReadOnlyList<EmployeeLookupResponse>> LookupAsync(EmployeeQuery query, CancellationToken ct);
    Task<EmployeeDetailResponse> GetAsync(Guid id, CancellationToken ct);
    Task<EmployeeDetailResponse> CreateAsync(EmployeeCreateRequest request, CancellationToken ct);
    Task<EmployeeDetailResponse> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<EmployeeListResponse>> DirectReportsAsync(Guid id, int skip, int take, CancellationToken ct);
    Task<IReadOnlyList<EmployeeLookupResponse>> ReportingChainAsync(Guid id, CancellationToken ct);
}
