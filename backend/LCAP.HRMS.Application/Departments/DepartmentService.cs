using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Departments.DTOs;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Domain.Departments;

namespace LCAP.HRMS.Application.Departments;

public sealed class DepartmentService(IDepartmentRepository repository, ICompanyRepository companies,
    IUnitOfWork unitOfWork) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        return (await repository.ListAsync(skip, take, cancellationToken: cancellationToken)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<DepartmentResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        await RequireCompanyAsync(companyId, cancellationToken);
        return (await repository.ListAsync(skip, take, department => department.CompanyId == companyId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<DepartmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<DepartmentResponse> CreateAsync(DepartmentCreateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        await RequireCompanyAsync(request.CompanyId, cancellationToken);
        var department = new Department();
        await ValidateParentAsync(department.Id, request.CompanyId, request.ParentDepartmentId, cancellationToken);
        if (await repository.CodeExistsAsync(request.CompanyId, NormalizeCode(request.DepartmentCode), cancellationToken: cancellationToken))
            throw new ConflictException("DepartmentCode is already in use within this company.");
        Apply(department, request);
        await repository.AddAsync(department, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(department);
    }

    public async Task<DepartmentResponse> UpdateAsync(Guid id, DepartmentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var department = await FindAsync(id, cancellationToken);
        if (department.CompanyId != request.CompanyId)
            throw new ValidationException("A department's owning company cannot be changed.");
        await ValidateParentAsync(id, request.CompanyId, request.ParentDepartmentId, cancellationToken);
        if (await repository.CodeExistsAsync(request.CompanyId, NormalizeCode(request.DepartmentCode), id, cancellationToken))
            throw new ConflictException("DepartmentCode is already in use within this company.");
        Apply(department, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(department);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await FindAsync(id, cancellationToken);
        if (await repository.HasChildrenAsync(id, cancellationToken))
            throw new ConflictException("Reassign or delete child departments before deleting this department.");
        repository.Remove(department);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Department> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department was not found.");

    private async Task RequireCompanyAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await companies.GetByIdAsync(id, cancellationToken) is null)
            throw new NotFoundException("Company was not found.");
    }

    private async Task ValidateParentAsync(Guid id, Guid companyId, Guid? parentId, CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid> { id };
        while (parentId is Guid candidate)
        {
            if (!visited.Add(candidate))
                throw new ValidationException("A department cannot be its own parent or create a hierarchy cycle.");
            var parent = await repository.GetByIdAsync(candidate, cancellationToken)
                ?? throw new NotFoundException("Parent department was not found.");
            if (parent.CompanyId != companyId)
                throw new ValidationException("Parent department must belong to the same company.");
            parentId = parent.ParentDepartmentId;
        }
    }

    private static void Validate(DepartmentWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new ValidationException(string.Join(" ", errors.Select(error => error.ErrorMessage)));
    }

    private static void ValidatePage(int skip, int take)
    {
        if (skip < 0 || take is < 1 or > 1000)
            throw new ValidationException("Skip must be non-negative and take must be between 1 and 1000.");
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static void Apply(Department department, DepartmentWriteRequest request)
    {
        department.CompanyId = request.CompanyId;
        department.DepartmentCode = NormalizeCode(request.DepartmentCode);
        department.DepartmentName = request.DepartmentName.Trim();
        department.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        department.ParentDepartmentId = request.ParentDepartmentId;
        department.IsActive = request.IsActive;
    }

    private static DepartmentResponse Map(Department department) => new()
    {
        Id = department.Id,
        CompanyId = department.CompanyId,
        DepartmentCode = department.DepartmentCode,
        DepartmentName = department.DepartmentName,
        Description = department.Description,
        ParentDepartmentId = department.ParentDepartmentId,
        IsActive = department.IsActive,
        CreatedAt = department.CreatedAt,
        CreatedBy = department.CreatedBy,
        UpdatedAt = department.UpdatedAt,
        UpdatedBy = department.UpdatedBy,
        IsDeleted = department.IsDeleted
    };
}
