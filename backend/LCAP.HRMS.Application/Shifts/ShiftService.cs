using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Shifts.DTOs;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Domain.Shifts;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Application.Shifts;

public sealed class ShiftService(IShiftRepository repository, ICompanyRepository companies, IUnitOfWork unitOfWork) : IShiftService
{
    public async Task<IReadOnlyList<ShiftResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        return (await repository.ListAsync(skip, take, cancellationToken: cancellationToken)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<ShiftResponse>> ListByCompanyAsync(Guid companyId, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        await FindCompanyAsync(companyId, cancellationToken);
        return (await repository.ListAsync(skip, take, shift => shift.CompanyId == companyId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<ShiftResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<ShiftResponse> CreateAsync(ShiftCreateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var company = await FindCompanyAsync(request.CompanyId, cancellationToken);
        if (await repository.CodeExistsAsync(company.Id, NormalizeCode(request.ShiftCode), cancellationToken: cancellationToken))
            throw new ConflictException("ShiftCode is already in use within this company.");
        var shift = new Shift();
        Apply(shift, request);
        await repository.AddAsync(shift, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(shift);
    }

    public async Task<ShiftResponse> UpdateAsync(Guid id, ShiftUpdateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var shift = await FindAsync(id, cancellationToken);
        var company = await FindCompanyAsync(request.CompanyId, cancellationToken);
        if (await repository.CodeExistsAsync(company.Id, NormalizeCode(request.ShiftCode), id, cancellationToken))
            throw new ConflictException("ShiftCode is already in use within this company.");
        Apply(shift, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(shift);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Shift> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Shift was not found.");

    private async Task<Company> FindCompanyAsync(Guid id, CancellationToken cancellationToken) =>
        await companies.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Company was not found.");

    private static void Validate(ShiftWriteRequest request)
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

    private static void Apply(Shift shift, ShiftWriteRequest request)
    {
        shift.CompanyId = request.CompanyId;
        shift.ShiftCode = NormalizeCode(request.ShiftCode);
        shift.ShiftName = request.ShiftName.Trim();
        shift.StartTime = request.StartTime!.Value;
        shift.EndTime = request.EndTime!.Value;
        shift.GracePeriodMinutes = request.GracePeriodMinutes;
        shift.MinimumHalfDayMinutes = request.MinimumHalfDayMinutes;
        shift.MinimumFullDayMinutes = request.MinimumFullDayMinutes;
        shift.IsNightShift = request.IsNightShift || shift.EndTime < shift.StartTime;
        shift.IsActive = request.IsActive;
    }

    private static ShiftResponse Map(Shift shift) => new()
    {
        Id = shift.Id,
        CompanyId = shift.CompanyId,
        ShiftCode = shift.ShiftCode,
        ShiftName = shift.ShiftName,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        GracePeriodMinutes = shift.GracePeriodMinutes,
        MinimumHalfDayMinutes = shift.MinimumHalfDayMinutes,
        MinimumFullDayMinutes = shift.MinimumFullDayMinutes,
        IsNightShift = shift.IsNightShift,
        IsActive = shift.IsActive,
        CreatedAt = shift.CreatedAt,
        CreatedBy = shift.CreatedBy,
        UpdatedAt = shift.UpdatedAt,
        UpdatedBy = shift.UpdatedBy,
        IsDeleted = shift.IsDeleted
    };
}
