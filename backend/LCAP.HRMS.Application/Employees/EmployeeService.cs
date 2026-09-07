using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Employees.DTOs;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Domain.Departments;
using LCAP.HRMS.Domain.Designations;
using LCAP.HRMS.Domain.Shifts;
using LCAP.HRMS.Domain.WorkLocations;
namespace LCAP.HRMS.Application.Employees;

public sealed class EmployeeService(IEmployeeRepository repository, IUnitOfWork unitOfWork, IEmployeeAccess access,
    IBaseRepository<Company> companies, IBaseRepository<Branch> branches, IBaseRepository<Department> departments,
    IBaseRepository<Designation> designations, IBaseRepository<Shift> shifts, IBaseRepository<WorkLocation> locations) : IEmployeeService
{
    private void CompanyAccess(Guid companyId)
    {
        if (!access.IsSuperAdmin && (access.CompanyId is null || access.CompanyId != companyId)) throw new ForbiddenException();
    }
    private EmployeeQuery Scope(EmployeeQuery q)
    {
        if (q.Skip < 0 || q.Take is < 1 or > 1000 || q.Search?.Length > 200 || (q.Status is {} status && !Enum.IsDefined(status))) throw new ValidationException("Invalid employee query.");
        if (!access.IsSuperAdmin)
        {
            if (access.CompanyId is not {} company) throw new ForbiddenException();
            if (q.CompanyId is {} requested && requested != company) throw new ForbiddenException();
            q = q with { CompanyId=company };
        }
        if (!access.CanReadDirectory)
        {
            if (access.EmployeeId is not {} id) throw new ForbiddenException();
            q = access.IsManager ? q with { ManagerId=id } : q with { EmployeeId=id };
        }
        return q;
    }
    public Task<IReadOnlyList<EmployeeListResponse>> ListAsync(EmployeeQuery query,CancellationToken ct) => repository.SearchAsync(Scope(query),ct);
    public Task<IReadOnlyList<EmployeeLookupResponse>> LookupAsync(EmployeeQuery query,CancellationToken ct) => repository.LookupAsync(Scope(query),ct);
    private async Task<Employee> Find(Guid id,CancellationToken ct) => await repository.GetByIdAsync(id,ct) ?? throw new NotFoundException("Employee was not found.");
    private async Task ReadAccess(Employee employee,CancellationToken ct)
    {
        CompanyAccess(employee.CompanyId);
        if (access.CanReadDirectory || employee.Id==access.EmployeeId) return;
        if (access.IsManager && access.EmployeeId is {} manager)
        {
            var hierarchy=(await repository.HierarchyAsync(employee.CompanyId,ct)).ToDictionary(e=>e.Id,e=>e.ManagerId);
            var next=employee.ReportingManagerId; var seen=new HashSet<Guid>();
            while(next is {} parent && seen.Add(parent))
            { if(parent==manager)return; next=hierarchy.GetValueOrDefault(parent); }
        }
        throw new ForbiddenException();
    }
    public async Task<EmployeeDetailResponse> GetAsync(Guid id,CancellationToken ct)
    {
        var employee=await repository.DetailAsync(id,ct) ?? throw new NotFoundException("Employee was not found.");
        await ReadAccess(employee,ct);
        return EmployeeMapping.Detail(employee,access.CanReadSensitive);
    }
    public async Task<EmployeeDetailResponse> CreateAsync(EmployeeCreateRequest request,CancellationToken ct)
    {
        if(!access.CanWrite)throw new ForbiddenException();
        var id=await repository.WriteAsync(async()=>
        {
            await Validate(request,null,ct);
            var employee=new Employee(); EmployeeMapping.Apply(employee,request);
            await repository.AddAsync(employee,ct); await unitOfWork.SaveChangesAsync(ct); return employee.Id;
        },ct);
        return await GetAsync(id,ct);
    }
    public async Task<EmployeeDetailResponse> UpdateAsync(Guid id,EmployeeUpdateRequest request,CancellationToken ct)
    {
        if(!access.CanWrite)throw new ForbiddenException();
        await repository.WriteAsync(async()=>
        {
            var employee=await Find(id,ct); CompanyAccess(employee.CompanyId);
            if(employee.CompanyId!=request.CompanyId)throw new ConflictException("Company cannot be changed for an existing employee. Use an explicit transfer workflow.");
            await Validate(request,id,ct); EmployeeMapping.Apply(employee,request);
            await unitOfWork.SaveChangesAsync(ct); return id;
        },ct);
        return await GetAsync(id,ct);
    }
    public async Task DeleteAsync(Guid id,CancellationToken ct)
    {
        if(!access.CanWrite)throw new ForbiddenException();
        await repository.WriteAsync(async()=>
        {
            var employee=await Find(id,ct); CompanyAccess(employee.CompanyId);
            if((await repository.HierarchyAsync(employee.CompanyId,ct)).Any(e=>e.ManagerId==id))
                throw new ConflictException("Reassign reporting relationships before deleting this manager.");
            repository.Remove(employee); await unitOfWork.SaveChangesAsync(ct); return id;
        },ct);
    }
    public async Task<IReadOnlyList<EmployeeListResponse>> DirectReportsAsync(Guid id,int skip,int take,CancellationToken ct)
    {
        var manager=await Find(id,ct); CompanyAccess(manager.CompanyId);
        if(!access.CanReadDirectory && (!access.IsManager || access.EmployeeId!=id))throw new ForbiddenException();
        if(skip<0 || take is <1 or >1000)throw new ValidationException("Invalid employee page.");
        return await repository.SearchAsync(new(CompanyId:manager.CompanyId,ManagerId:id,Skip:skip,Take:take),ct);
    }
    public async Task<IReadOnlyList<EmployeeLookupResponse>> ReportingChainAsync(Guid id,CancellationToken ct)
    {
        var employee=await Find(id,ct); await ReadAccess(employee,ct);
        var rows=new List<EmployeeLookupResponse>(); var next=employee.ReportingManagerId; var seen=new HashSet<Guid>{id};
        while(next is {} parent)
        {
            if(!seen.Add(parent))throw new ConflictException("Reporting hierarchy contains a cycle.");
            var manager=await Find(parent,ct); CompanyAccess(manager.CompanyId);
            rows.Add(new(manager.Id,manager.CompanyId,manager.EmployeeCode,manager.FullName)); next=manager.ReportingManagerId;
        }
        return rows;
    }
    private async Task Validate(EmployeeWriteRequest r,Guid? id,CancellationToken ct)
    {
        var errors=new List<ValidationResult>();
        if(!Validator.TryValidateObject(r,new ValidationContext(r),errors,true)) throw new ValidationException(string.Join(" ",errors.Select(e=>e.ErrorMessage)));
        CompanyAccess(r.CompanyId);
        var company=await companies.GetByIdAsync(r.CompanyId,ct);
        var branch=await branches.GetByIdAsync(r.BranchId,ct);
        var department=await departments.GetByIdAsync(r.DepartmentId,ct);
        var designation=await designations.GetByIdAsync(r.DesignationId,ct);
        var shift=await shifts.GetByIdAsync(r.ShiftId,ct);
        var location=await locations.GetByIdAsync(r.WorkLocationId,ct);
        if(company is null || branch is null || department is null || designation is null || shift is null || location is null)
            throw new ValidationException("One or more organisation assignments do not exist.");
        if(branch.CompanyId!=r.CompanyId || department.CompanyId!=r.CompanyId || designation.CompanyId!=r.CompanyId || shift.CompanyId!=r.CompanyId || location.CompanyId!=r.CompanyId || location.BranchId!=r.BranchId)
            throw new ValidationException("Organisation assignments must belong to the selected company and branch.");
        if(await repository.CodeExistsAsync(r.CompanyId,r.EmployeeCode.Trim().ToUpperInvariant(),id,ct)) throw new ConflictException("EmployeeCode is already in use within this company.");
        if(r.ReportingManagerId is {} managerId)
        {
            if(managerId==id)throw new ConflictException("An employee cannot report to themselves.");
            var manager=await repository.GetByIdAsync(managerId,ct);
            if(manager is null || manager.CompanyId!=r.CompanyId)throw new ValidationException("Reporting manager must be an existing employee in the same company.");
            var hierarchy=(await repository.HierarchyAsync(r.CompanyId,ct)).ToDictionary(e=>e.Id,e=>e.ManagerId);
            var seen=new HashSet<Guid>(); if(id is {} current)seen.Add(current);
            Guid? next=managerId;
            while(next is {} parent)
            { if(!seen.Add(parent))throw new ConflictException("Reporting relationship would create a cycle."); next=hierarchy.GetValueOrDefault(parent); }
        }
    }
}
