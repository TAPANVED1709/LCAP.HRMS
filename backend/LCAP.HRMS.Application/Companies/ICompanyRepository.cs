using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Application.Companies;

public interface ICompanyRepository : IBaseRepository<Company>
{
    // Includes soft-deleted rows: company codes remain reserved.
    Task<bool> CodeExistsAsync(string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
}
