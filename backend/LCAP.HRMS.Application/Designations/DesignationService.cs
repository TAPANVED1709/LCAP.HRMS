using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Designations.DTOs;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Domain.Designations;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Application.Designations;

public sealed class DesignationService(IDesignationRepository repository, ICompanyRepository companies, IUnitOfWork unitOfWork) : IDesignationService
{
    public async Task<IReadOnlyList<DesignationResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        return (await repository.ListAsync(skip, take, cancellationToken: cancellationToken)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<DesignationResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        await FindCompanyAsync(companyId, cancellationToken);
        return (await repository.ListAsync(skip, take, designation => designation.CompanyId == companyId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<DesignationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<DesignationResponse> CreateAsync(DesignationCreateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var company = await FindCompanyAsync(request.CompanyId, cancellationToken);
        if (await repository.CodeExistsAsync(company.Id, NormalizeCode(request.DesignationCode), cancellationToken: cancellationToken))
            throw new ConflictException("DesignationCode is already in use within this company.");
        var designation = new Designation();
        Apply(designation, request);
        await repository.AddAsync(designation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(designation);
    }

    public async Task<DesignationResponse> UpdateAsync(Guid id, DesignationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var designation = await FindAsync(id, cancellationToken);
        var company = await FindCompanyAsync(request.CompanyId, cancellationToken);
        if (await repository.CodeExistsAsync(company.Id, NormalizeCode(request.DesignationCode), id, cancellationToken))
            throw new ConflictException("DesignationCode is already in use within this company.");
        Apply(designation, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(designation);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Designation> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Designation was not found.");

    private async Task<Company> FindCompanyAsync(Guid id, CancellationToken cancellationToken) =>
        await companies.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Company was not found.");

    private static void Validate(DesignationWriteRequest request)
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

    private static void Apply(Designation designation, DesignationWriteRequest request)
    {
        designation.CompanyId = request.CompanyId;
        designation.DesignationCode = NormalizeCode(request.DesignationCode);
        designation.DesignationName = request.DesignationName.Trim();
        designation.Description = Optional(request.Description);
        designation.Grade = Optional(request.Grade);
        designation.Level = request.Level;
        designation.IsManagerial = request.IsManagerial;
        designation.IsActive = request.IsActive;
    }

    private static DesignationResponse Map(Designation designation) => new()
    {
        Id = designation.Id,
        CompanyId = designation.CompanyId,
        DesignationCode = designation.DesignationCode,
        DesignationName = designation.DesignationName,
        Description = designation.Description,
        Grade = designation.Grade,
        Level = designation.Level,
        IsManagerial = designation.IsManagerial,
        IsActive = designation.IsActive,
        CreatedAt = designation.CreatedAt,
        CreatedBy = designation.CreatedBy,
        UpdatedAt = designation.UpdatedAt,
        UpdatedBy = designation.UpdatedBy,
        IsDeleted = designation.IsDeleted
    };
}
