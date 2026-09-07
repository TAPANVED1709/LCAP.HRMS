using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Shifts;

namespace LCAP.HRMS.Application.Shifts;

public interface IShiftRepository : IBaseRepository<Shift>
{
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
}
