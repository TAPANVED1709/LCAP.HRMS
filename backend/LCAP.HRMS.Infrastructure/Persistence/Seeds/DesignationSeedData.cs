using LCAP.HRMS.Domain.Designations;

namespace LCAP.HRMS.Infrastructure.Persistence.Seeds;

public static class DesignationSeedData
{
    public static Designation[] ForLcap() =>
    [
        Create("42d49a28-e6b8-46c7-aac1-11019a39d001", "EXEC", "Executive"),
        Create("42d49a28-e6b8-46c7-aac1-11019a39d002", "SREXEC", "Senior Executive"),
        Create("42d49a28-e6b8-46c7-aac1-11019a39d003", "TL", "Team Leader"),
        Create("42d49a28-e6b8-46c7-aac1-11019a39d004", "MGR", "Manager"),
        Create("42d49a28-e6b8-46c7-aac1-11019a39d005", "HRM", "HR Manager"),
        Create("42d49a28-e6b8-46c7-aac1-11019a39d006", "PAYADMIN", "Payroll Admin")
    ];

    private static Designation Create(string id, string code, string name) => new()
    {
        Id = Guid.Parse(id),
        CompanyId = CompanySeedData.LcapId,
        DesignationCode = code,
        DesignationName = name,
        IsManagerial = false,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "system:seed"
    };
}
