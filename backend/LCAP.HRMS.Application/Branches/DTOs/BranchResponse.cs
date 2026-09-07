namespace LCAP.HRMS.Application.Branches.DTOs;

public sealed class BranchResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; } = Guid.Empty;
    public string BranchCode { get; init; } = "";
    public string BranchName { get; init; } = "";
    public string? AddressLine1 { get; init; } = null;
    public string? AddressLine2 { get; init; } = null;
    public string? City { get; init; } = null;
    public string? State { get; init; } = null;
    public string Country { get; init; } = "India";
    public string? PinCode { get; init; } = null;
    public string? Email { get; init; } = null;
    public string? Phone { get; init; } = null;
    public bool IsHeadOffice { get; init; } = false;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
}
