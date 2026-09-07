namespace LCAP.HRMS.Application.WorkLocations.DTOs;

public sealed class WorkLocationResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; } = Guid.Empty;
    public Guid BranchId { get; init; } = Guid.Empty;
    public string LocationCode { get; init; } = "";
    public string LocationName { get; init; } = "";
    public string? Address { get; init; } = null;
    public string? City { get; init; } = null;
    public string? State { get; init; } = null;
    public string Country { get; init; } = "India";
    public string? PinCode { get; init; } = null;
    public decimal? Latitude { get; init; } = null;
    public decimal? Longitude { get; init; } = null;
    public int AllowedRadiusMeters { get; init; } = 100;
    public bool IsGeoFenceEnabled { get; init; } = true;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
}
