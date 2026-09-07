using LCAP.HRMS.Application.Branches;
using LCAP.HRMS.Domain.Branches;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository(ApplicationDbContext context) : GenericRepository<Branch>(context), IBranchRepository
{
    public Task<bool> HasWorkLocationsAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        Context.WorkLocations.IgnoreQueryFilters().AnyAsync(location => location.BranchId == branchId, cancellationToken);
    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Context.Branches.IgnoreQueryFilters().AnyAsync(branch => branch.CompanyId == companyId && branch.BranchCode == code
            && (!excludingId.HasValue || branch.Id != excludingId.Value), cancellationToken);
}
