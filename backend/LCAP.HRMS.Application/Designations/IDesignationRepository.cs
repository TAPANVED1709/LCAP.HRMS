using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Designations;

namespace LCAP.HRMS.Application.Designations;

public interface IDesignationRepository : IBaseRepository<Designation>
{
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
}
