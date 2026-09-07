using System.Data;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Application.Employees.DTOs;
using LCAP.HRMS.Domain.Employees;
using Microsoft.EntityFrameworkCore;
namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class EmployeeRepository(ApplicationDbContext context) : GenericRepository<Employee>(context), IEmployeeRepository
{
    private IQueryable<Employee> Query(EmployeeQuery q)
    {
        var query = Context.Employees.AsNoTracking();
        if (q.CompanyId is {} company) query = query.Where(e=>e.CompanyId == company);
        if (q.BranchId is {} branch) query = query.Where(e=>e.BranchId == branch);
        if (q.DepartmentId is {} department) query = query.Where(e=>e.DepartmentId == department);
        if (q.ManagerId is {} manager) query = query.Where(e=>e.ReportingManagerId == manager);
        if (q.EmployeeId is {} employee) query = query.Where(e=>e.Id == employee);
        if (q.Status is {} status) query = query.Where(e=>e.EmployeeStatus == status);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            query = query.Where(e=>e.EmployeeCode.Contains(term) || (e.FirstName + " " + (e.MiddleName ?? "") + " " + (e.LastName ?? "")).Contains(term));
        }
        return query.OrderBy(e=>e.EmployeeCode).ThenBy(e=>e.Id).Skip(q.Skip).Take(q.Take);
    }
    public async Task<IReadOnlyList<EmployeeListResponse>> SearchAsync(EmployeeQuery q, CancellationToken ct) =>
        await Query(q).Select(e=>new EmployeeListResponse
        {
            Id=e.Id,CompanyId=e.CompanyId,BranchId=e.BranchId,DepartmentId=e.DepartmentId,
            EmployeeCode=e.EmployeeCode,FullName=e.FirstName + (e.MiddleName == null ? "" : " " + e.MiddleName) + (e.LastName == null ? "" : " " + e.LastName),
            OfficialEmail=e.OfficialEmail,MobileNumber=e.MobileNumber,Branch=e.Branch.BranchName,
            Department=e.Department.DepartmentName,Designation=e.Designation.DesignationName,
            ReportingManagerName=e.ReportingManager == null ? null : e.ReportingManager.FirstName + (e.ReportingManager.LastName == null ? "" : " " + e.ReportingManager.LastName),
            DateOfJoining=e.DateOfJoining,EmployeeStatus=e.EmployeeStatus,IsActive=e.IsActive
        }).ToListAsync(ct);
    public async Task<IReadOnlyList<EmployeeLookupResponse>> LookupAsync(EmployeeQuery q, CancellationToken ct) =>
        await Query(q).Select(e=>new EmployeeLookupResponse(e.Id,e.CompanyId,e.EmployeeCode,
            e.FirstName + (e.LastName == null ? "" : " " + e.LastName))).ToListAsync(ct);
    public Task<Employee?> DetailAsync(Guid id, CancellationToken ct) => Context.Employees.AsNoTracking()
        .Include(e=>e.Company).Include(e=>e.Branch).Include(e=>e.Department).Include(e=>e.Designation)
        .Include(e=>e.Shift).Include(e=>e.WorkLocation).Include(e=>e.ReportingManager).SingleOrDefaultAsync(e=>e.Id==id,ct);
    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? exceptId, CancellationToken ct) =>
        Context.Employees.IgnoreQueryFilters().AnyAsync(e=>e.CompanyId==companyId && e.EmployeeCode==code && e.Id!=exceptId,ct);
    public async Task<IReadOnlyList<(Guid Id, Guid? ManagerId)>> HierarchyAsync(Guid companyId, CancellationToken ct)
    {
        // Include retained employees so inactive/deleted links cannot conceal a cycle.
        var rows = await Context.Employees.IgnoreQueryFilters().AsNoTracking().Where(e=>e.CompanyId==companyId)
            .Select(e=>new {e.Id,e.ReportingManagerId}).ToListAsync(ct);
        return rows.Select(e=>(e.Id,e.ReportingManagerId)).ToArray();
    }
    public async Task<T> WriteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        // Serialize hierarchy edits across instances. The lock belongs to the SQL transaction.
        // ExecuteStrategy also accommodates the production SQL retry policy.
        return await Context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            Context.ChangeTracker.Clear();
            await using var transaction = await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            if (Context.Database.IsSqlServer())
                await Context.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'LCAP.EmployeeHierarchy', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'Employee hierarchy is busy. Retry the request.', 1;",ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        });
    }
}
