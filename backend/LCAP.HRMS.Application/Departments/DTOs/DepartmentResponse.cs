namespace LCAP.HRMS.Application.Departments.DTOs;

public sealed class DepartmentResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; } = Guid.Empty;
    public string DepartmentCode { get; init; } = "";
    public string DepartmentName { get; init; } = "";
    public string? Description { get; init; } = null;
    public Guid? ParentDepartmentId { get; init; } = null;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
}
