using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Branches.DTOs;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Application.Branches;

public sealed class BranchService(IBranchRepository repository, ICompanyRepository companies, IUnitOfWork unitOfWork) : IBranchService
{
    public async Task<IReadOnlyList<BranchResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        return (await repository.ListAsync(skip, take, cancellationToken: cancellationToken)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<BranchResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        await FindCompanyAsync(companyId, cancellationToken);
        return (await repository.ListAsync(skip, take, branch => branch.CompanyId == companyId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<BranchResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<BranchResponse> CreateAsync(BranchCreateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var company = await FindCompanyAsync(request.CompanyId, cancellationToken);
        if (await repository.CodeExistsAsync(company.Id, NormalizeCode(request.BranchCode), cancellationToken: cancellationToken))
            throw new ConflictException("BranchCode is already in use within this company.");
        var branch = new Branch();
        Apply(branch, request, company);
        await repository.AddAsync(branch, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(branch);
    }

    public async Task<BranchResponse> UpdateAsync(Guid id, BranchUpdateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var branch = await FindAsync(id, cancellationToken);
        var company = await FindCompanyAsync(request.CompanyId, cancellationToken);
        if (branch.CompanyId != company.Id && await repository.HasWorkLocationsAsync(id, cancellationToken))
            throw new ConflictException("A branch with work-location records cannot change company. Reassign its work locations first.");
        if (await repository.CodeExistsAsync(company.Id, NormalizeCode(request.BranchCode), id, cancellationToken))
            throw new ConflictException("BranchCode is already in use within this company.");
        Apply(branch, request, company);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(branch);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Branch> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Branch was not found.");

    private async Task<Company> FindCompanyAsync(Guid id, CancellationToken cancellationToken) =>
        await companies.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Company was not found.");

    private static void Validate(BranchWriteRequest request)
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
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Apply(Branch branch, BranchWriteRequest request, Company company)
    {
        branch.CompanyId = request.CompanyId;
        branch.BranchCode = NormalizeCode(request.BranchCode);
        branch.BranchName = request.BranchName.Trim();
        branch.AddressLine1 = Optional(request.AddressLine1);
        branch.AddressLine2 = Optional(request.AddressLine2);
        branch.City = Optional(request.City);
        branch.State = Optional(request.State) ?? company.State;
        branch.Country = request.Country.Trim();
        branch.PinCode = Optional(request.PinCode);
        branch.Email = Optional(request.Email);
        branch.Phone = Optional(request.Phone);
        branch.IsHeadOffice = request.IsHeadOffice;
        branch.IsActive = request.IsActive;
    }

    private static BranchResponse Map(Branch branch) => new()
    {
        Id = branch.Id,
        CompanyId = branch.CompanyId,
        BranchCode = branch.BranchCode,
        BranchName = branch.BranchName,
        AddressLine1 = branch.AddressLine1,
        AddressLine2 = branch.AddressLine2,
        City = branch.City,
        State = branch.State,
        Country = branch.Country,
        PinCode = branch.PinCode,
        Email = branch.Email,
        Phone = branch.Phone,
        IsHeadOffice = branch.IsHeadOffice,
        IsActive = branch.IsActive,
        CreatedAt = branch.CreatedAt,
        CreatedBy = branch.CreatedBy,
        UpdatedAt = branch.UpdatedAt,
        UpdatedBy = branch.UpdatedBy,
        IsDeleted = branch.IsDeleted
    };
}
