using LCAP.HRMS.Application.Departments;
using LCAP.HRMS.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class DepartmentRepository(ApplicationDbContext context) : GenericRepository<Department>(context), IDepartmentRepository
{
    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Context.Departments.IgnoreQueryFilters().AnyAsync(department => department.CompanyId == companyId && department.DepartmentCode == code
            && (!excludingId.HasValue || department.Id != excludingId.Value), cancellationToken);

    public Task<bool> HasChildrenAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        Context.Departments.AnyAsync(department => department.ParentDepartmentId == departmentId, cancellationToken);
}
