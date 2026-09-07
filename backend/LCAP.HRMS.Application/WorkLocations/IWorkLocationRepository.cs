using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.WorkLocations;

namespace LCAP.HRMS.Application.WorkLocations;

public interface IWorkLocationRepository : IBaseRepository<WorkLocation>
{
    Task<bool> CodeExistsAsync(Guid branchId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
}
