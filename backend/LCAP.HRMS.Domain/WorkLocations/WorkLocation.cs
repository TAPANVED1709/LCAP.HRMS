using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Domain.Branches;

namespace LCAP.HRMS.Domain.WorkLocations;

public sealed class WorkLocation : BaseEntity
{
    public Guid CompanyId { get; set; } = Guid.Empty;

    public Guid BranchId { get; set; } = Guid.Empty;

    public string LocationCode { get; set; } = "";

    public string LocationName { get; set; } = "";

    public string? Address { get; set; } = null;

    public string? City { get; set; } = null;

    public string? State { get; set; } = null;

    public string Country { get; set; } = "India";

    public string? PinCode { get; set; } = null;

    public decimal? Latitude { get; set; } = null;

    public decimal? Longitude { get; set; } = null;

    public int AllowedRadiusMeters { get; set; } = 100;

    public bool IsGeoFenceEnabled { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
