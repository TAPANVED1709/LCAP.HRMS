using LCAP.HRMS.Application.Departments.DTOs;

namespace LCAP.HRMS.Application.Departments;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> CreateAsync(DepartmentCreateRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> UpdateAsync(Guid id, DepartmentUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
