using LCAP.HRMS.Application.Branches.DTOs;

namespace LCAP.HRMS.Application.Branches;

public interface IBranchService
{
    Task<IReadOnlyList<BranchResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<BranchResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BranchResponse> CreateAsync(BranchCreateRequest request, CancellationToken cancellationToken = default);
    Task<BranchResponse> UpdateAsync(Guid id, BranchUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
