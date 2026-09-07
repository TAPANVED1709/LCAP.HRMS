using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Departments;

namespace LCAP.HRMS.Application.Departments;

public interface IDepartmentRepository : IBaseRepository<Department>
{
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
