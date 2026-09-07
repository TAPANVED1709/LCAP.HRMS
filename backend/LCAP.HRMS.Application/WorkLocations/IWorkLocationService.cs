using LCAP.HRMS.Application.WorkLocations.DTOs;

namespace LCAP.HRMS.Application.WorkLocations;

public interface IWorkLocationService
{
    Task<IReadOnlyList<WorkLocationResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkLocationResponse>> ListByBranchAsync(Guid branchId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<WorkLocationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkLocationResponse> CreateAsync(WorkLocationCreateRequest request, CancellationToken cancellationToken = default);
    Task<WorkLocationResponse> UpdateAsync(Guid id, WorkLocationUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
