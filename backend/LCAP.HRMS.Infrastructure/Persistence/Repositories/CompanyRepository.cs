using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository(ApplicationDbContext context) : GenericRepository<Company>(context), ICompanyRepository
{
    public Task<bool> CodeExistsAsync(string code, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Context.Companies.IgnoreQueryFilters().AnyAsync(company => company.CompanyCode == code
            && (!excludingId.HasValue || company.Id != excludingId.Value), cancellationToken);
}
