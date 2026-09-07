using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Domain.Departments;

public sealed class Department : BaseEntity
{
    public Guid CompanyId { get; set; } = Guid.Empty;

    public string DepartmentCode { get; set; } = "";

    public string DepartmentName { get; set; } = "";

    public string? Description { get; set; } = null;

    public Guid? ParentDepartmentId { get; set; } = null;

    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> Children { get; set; } = new List<Department>();
}
