using LCAP.HRMS.Domain.Branches;

namespace LCAP.HRMS.Infrastructure.Persistence.Seeds;

public static class BranchSeedData
{
    public static readonly Guid PatnaHeadOfficeId = new("b3e36a15-4efb-441e-bbd8-f6cf916f6107");

    public static Branch PatnaHeadOffice() => new()
    {
        Id = PatnaHeadOfficeId,
        CompanyId = CompanySeedData.LcapId,
        BranchCode = "PATNA-HO",
        BranchName = "Patna Head Office",
        City = "Patna",
        State = "Bihar",
        Country = "India",
        IsHeadOffice = true,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "system:seed",
        IsDeleted = false
    };
}
