using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Branches;

namespace LCAP.HRMS.Application.Branches;

public interface IBranchRepository : IBaseRepository<Branch>
{
    Task<bool> HasWorkLocationsAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
}
