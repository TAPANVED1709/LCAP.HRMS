using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.WorkLocations.DTOs;
using LCAP.HRMS.Application.Branches;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Domain.WorkLocations;
using LCAP.HRMS.Domain.Branches;

namespace LCAP.HRMS.Application.WorkLocations;

public sealed class WorkLocationService(IWorkLocationRepository repository, IBranchRepository branches, IUnitOfWork unitOfWork) : IWorkLocationService
{
    public async Task<IReadOnlyList<WorkLocationResponse>> ListAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        return (await repository.ListAsync(skip, take, cancellationToken: cancellationToken)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<WorkLocationResponse>> ListByBranchAsync(Guid branchId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        ValidatePage(skip, take);
        await FindBranchAsync(branchId, cancellationToken);
        return (await repository.ListAsync(skip, take, location => location.BranchId == branchId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<WorkLocationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<WorkLocationResponse> CreateAsync(WorkLocationCreateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        await ValidateBranchAsync(request, cancellationToken);
        if (await repository.CodeExistsAsync(request.BranchId, NormalizeCode(request.LocationCode), cancellationToken: cancellationToken))
            throw new ConflictException("LocationCode is already in use within this branch.");
        var location = new WorkLocation();
        Apply(location, request);
        await repository.AddAsync(location, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(location);
    }

    public async Task<WorkLocationResponse> UpdateAsync(Guid id, WorkLocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var location = await FindAsync(id, cancellationToken);
        await ValidateBranchAsync(request, cancellationToken);
        if (await repository.CodeExistsAsync(request.BranchId, NormalizeCode(request.LocationCode), id, cancellationToken))
            throw new ConflictException("LocationCode is already in use within this branch.");
        Apply(location, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(location);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        repository.Remove(await FindAsync(id, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<WorkLocation> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Work location was not found.");

    private async Task<Branch> FindBranchAsync(Guid id, CancellationToken cancellationToken) =>
        await branches.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Branch was not found.");

    private async Task ValidateBranchAsync(WorkLocationWriteRequest request, CancellationToken cancellationToken)
    {
        var branch = await FindBranchAsync(request.BranchId, cancellationToken);
        if (branch.CompanyId != request.CompanyId)
            throw new ValidationException("CompanyId must match the branch's owning company.");
    }

    private static void Validate(WorkLocationWriteRequest request)
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

    private static void Apply(WorkLocation location, WorkLocationWriteRequest request)
    {
        location.CompanyId = request.CompanyId;
        location.BranchId = request.BranchId;
        location.LocationCode = NormalizeCode(request.LocationCode);
        location.LocationName = request.LocationName.Trim();
        location.Address = Optional(request.Address);
        location.City = Optional(request.City);
        location.State = Optional(request.State);
        location.Country = request.Country.Trim();
        location.PinCode = Optional(request.PinCode);
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.AllowedRadiusMeters = request.AllowedRadiusMeters;
        location.IsGeoFenceEnabled = request.IsGeoFenceEnabled;
        location.IsActive = request.IsActive;
    }

    private static WorkLocationResponse Map(WorkLocation location) => new()
    {
        Id = location.Id,
        CompanyId = location.CompanyId,
        BranchId = location.BranchId,
        LocationCode = location.LocationCode,
        LocationName = location.LocationName,
        Address = location.Address,
        City = location.City,
        State = location.State,
        Country = location.Country,
        PinCode = location.PinCode,
        Latitude = location.Latitude,
        Longitude = location.Longitude,
        AllowedRadiusMeters = location.AllowedRadiusMeters,
        IsGeoFenceEnabled = location.IsGeoFenceEnabled,
        IsActive = location.IsActive,
        CreatedAt = location.CreatedAt,
        CreatedBy = location.CreatedBy,
        UpdatedAt = location.UpdatedAt,
        UpdatedBy = location.UpdatedBy,
        IsDeleted = location.IsDeleted
    };
}
