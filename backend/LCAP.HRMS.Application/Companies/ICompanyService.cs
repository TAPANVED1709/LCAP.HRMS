using LCAP.HRMS.Application.Companies.DTOs;

namespace LCAP.HRMS.Application.Companies;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanyResponse> CreateAsync(CompanyCreateRequest request, CancellationToken cancellationToken = default);
    Task<CompanyResponse> UpdateAsync(Guid id, CompanyUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
