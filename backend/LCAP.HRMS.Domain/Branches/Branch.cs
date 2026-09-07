using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Domain.WorkLocations;

namespace LCAP.HRMS.Domain.Branches;

public sealed class Branch : BaseEntity
{
    public Guid CompanyId { get; set; } = Guid.Empty;

    public string BranchCode { get; set; } = "";

    public string BranchName { get; set; } = "";

    public string? AddressLine1 { get; set; } = null;

    public string? AddressLine2 { get; set; } = null;

    public string? City { get; set; } = null;

    public string? State { get; set; } = null;

    public string Country { get; set; } = "India";

    public string? PinCode { get; set; } = null;

    public string? Email { get; set; } = null;

    public string? Phone { get; set; } = null;

    public bool IsHeadOffice { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public ICollection<WorkLocation> WorkLocations { get; set; } = new List<WorkLocation>();
}
