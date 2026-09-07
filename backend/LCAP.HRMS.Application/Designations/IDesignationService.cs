using LCAP.HRMS.Application.Designations.DTOs;

namespace LCAP.HRMS.Application.Designations;

public interface IDesignationService
{
    Task<IReadOnlyList<DesignationResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DesignationResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<DesignationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DesignationResponse> CreateAsync(DesignationCreateRequest request, CancellationToken cancellationToken = default);
    Task<DesignationResponse> UpdateAsync(Guid id, DesignationUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
