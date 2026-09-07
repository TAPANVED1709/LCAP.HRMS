using LCAP.HRMS.Application.WorkLocations;
using LCAP.HRMS.Domain.WorkLocations;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class WorkLocationRepository(ApplicationDbContext context) : GenericRepository<WorkLocation>(context), IWorkLocationRepository
{
    public Task<bool> CodeExistsAsync(Guid branchId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Context.WorkLocations.IgnoreQueryFilters().AnyAsync(location => location.BranchId == branchId && location.LocationCode == code
            && (!excludingId.HasValue || location.Id != excludingId.Value), cancellationToken);
}
