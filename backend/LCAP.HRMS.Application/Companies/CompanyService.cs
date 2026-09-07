using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Application.Companies;

public sealed class CompanyService(ICompanyRepository repository, IUnitOfWork unitOfWork) : ICompanyService
{
    public async Task<IReadOnlyList<CompanyResponse>> ListAsync(int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0 || take is < 1 or > 1000)
            throw new ValidationException("Skip must be non-negative and take must be between 1 and 1000.");
        return (await repository.ListAsync(skip, take, cancellationToken: cancellationToken)).Select(Map).ToArray();
    }

    public async Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<CompanyResponse> CreateAsync(CompanyCreateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var code = NormalizeCode(request.CompanyCode);
        if (await repository.CodeExistsAsync(code, cancellationToken: cancellationToken))
            throw new ConflictException("CompanyCode is already in use.");
        var company = new Company();
        Apply(company, request);
        await repository.AddAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(company);
    }

    public async Task<CompanyResponse> UpdateAsync(Guid id, CompanyUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var company = await FindAsync(id, cancellationToken);
        if (await repository.CodeExistsAsync(NormalizeCode(request.CompanyCode), id, cancellationToken))
            throw new ConflictException("CompanyCode is already in use.");
        Apply(company, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(company);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Company> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Company was not found.");

    private static void Validate(CompanyWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new ValidationException(string.Join(" ", errors.Select(error => error.ErrorMessage)));
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Apply(Company company, CompanyWriteRequest request)
    {
        company.CompanyCode = NormalizeCode(request.CompanyCode);
        company.CompanyName = request.CompanyName.Trim();
        company.LegalName = Optional(request.LegalName);
        company.RegisteredAddress = Optional(request.RegisteredAddress);
        company.City = Optional(request.City);
        company.State = Optional(request.State);
        company.Country = request.Country.Trim();
        company.PinCode = Optional(request.PinCode);
        company.PAN = Optional(request.PAN);
        company.TAN = Optional(request.TAN);
        company.GSTIN = Optional(request.GSTIN);
        company.PFRegistrationNumber = Optional(request.PFRegistrationNumber);
        company.ESIRegistrationNumber = Optional(request.ESIRegistrationNumber);
        company.PayrollCurrency = request.PayrollCurrency.Trim().ToUpperInvariant();
        company.PayrollDay = request.PayrollDay;
        company.SalaryPaymentDay = request.SalaryPaymentDay;
        company.LogoUrl = Optional(request.LogoUrl);
        company.IsActive = request.IsActive;
    }

    private static CompanyResponse Map(Company company) => new()
    {
        Id = company.Id,
        CompanyCode = company.CompanyCode,
        CompanyName = company.CompanyName,
        LegalName = company.LegalName,
        RegisteredAddress = company.RegisteredAddress,
        City = company.City,
        State = company.State,
        Country = company.Country,
        PinCode = company.PinCode,
        PAN = company.PAN,
        TAN = company.TAN,
        GSTIN = company.GSTIN,
        PFRegistrationNumber = company.PFRegistrationNumber,
        ESIRegistrationNumber = company.ESIRegistrationNumber,
        PayrollCurrency = company.PayrollCurrency,
        PayrollDay = company.PayrollDay,
        SalaryPaymentDay = company.SalaryPaymentDay,
        LogoUrl = company.LogoUrl,
        IsActive = company.IsActive,
        CreatedAt = company.CreatedAt,
        CreatedBy = company.CreatedBy,
        UpdatedAt = company.UpdatedAt,
        UpdatedBy = company.UpdatedBy,
        IsDeleted = company.IsDeleted
    };
}
