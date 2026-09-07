using LCAP.HRMS.Domain.Departments;

namespace LCAP.HRMS.Infrastructure.Persistence.Seeds;

public static class DepartmentSeedData
{
    public static Department[] ForLcap() =>
    [
        Create("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4201", "HR", "Human Resources"),
        Create("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4202", "FIN", "Finance"),
        Create("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4203", "SALES", "Sales"),
        Create("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4204", "OPS", "Operations"),
        Create("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4205", "MGMT", "Management")
    ];

    private static Department Create(string id, string code, string name) => new()
    {
        Id = Guid.Parse(id),
        CompanyId = CompanySeedData.LcapId,
        DepartmentCode = code,
        DepartmentName = name,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "system:seed"
    };
}
