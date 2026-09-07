using LCAP.HRMS.Application.Designations;
using LCAP.HRMS.Domain.Designations;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class DesignationRepository(ApplicationDbContext context) : GenericRepository<Designation>(context), IDesignationRepository
{
    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Context.Designations.IgnoreQueryFilters().AnyAsync(designation => designation.CompanyId == companyId && designation.DesignationCode == code
            && (!excludingId.HasValue || designation.Id != excludingId.Value), cancellationToken);
}
