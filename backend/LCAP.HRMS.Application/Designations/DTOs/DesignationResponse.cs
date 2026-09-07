namespace LCAP.HRMS.Application.Designations.DTOs;

public sealed class DesignationResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; } = Guid.Empty;
    public string DesignationCode { get; init; } = "";
    public string DesignationName { get; init; } = "";
    public string? Description { get; init; } = null;
    public string? Grade { get; init; } = null;
    public int? Level { get; init; } = null;
    public bool IsManagerial { get; init; } = false;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
}
