using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Domain.Designations;

public sealed class Designation : BaseEntity
{
    public Guid CompanyId { get; set; } = Guid.Empty;

    public string DesignationCode { get; set; } = "";

    public string DesignationName { get; set; } = "";

    public string? Description { get; set; } = null;

    public string? Grade { get; set; } = null;

    public int? Level { get; set; } = null;

    public bool IsManagerial { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
}
