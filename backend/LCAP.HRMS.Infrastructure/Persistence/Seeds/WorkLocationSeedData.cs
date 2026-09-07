using LCAP.HRMS.Domain.WorkLocations;

namespace LCAP.HRMS.Infrastructure.Persistence.Seeds;

public static class WorkLocationSeedData
{
    public static readonly Guid PatnaOfficeId = new("638b4a96-b60f-4cfb-b5a8-5bf4b304896a");

    public static WorkLocation PatnaOffice() => new()
    {
        Id = PatnaOfficeId,
        CompanyId = CompanySeedData.LcapId,
        BranchId = BranchSeedData.PatnaHeadOfficeId,
        LocationCode = "PATNA-OFFICE",
        LocationName = "Patna Office",
        City = "Patna",
        State = "Bihar",
        Country = "India",
        Latitude = null,
        Longitude = null,
        AllowedRadiusMeters = 100,
        IsGeoFenceEnabled = true,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "system:seed"
    };
}
