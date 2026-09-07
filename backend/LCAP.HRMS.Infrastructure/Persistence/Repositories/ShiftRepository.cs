using LCAP.HRMS.Application.Shifts;
using LCAP.HRMS.Domain.Shifts;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class ShiftRepository(ApplicationDbContext context) : GenericRepository<Shift>(context), IShiftRepository
{
    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Context.Shifts.IgnoreQueryFilters().AnyAsync(shift => shift.CompanyId == companyId && shift.ShiftCode == code
            && (!excludingId.HasValue || shift.Id != excludingId.Value), cancellationToken);
}
