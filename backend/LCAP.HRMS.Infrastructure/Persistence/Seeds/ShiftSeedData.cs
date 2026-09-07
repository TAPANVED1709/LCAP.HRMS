using LCAP.HRMS.Domain.Shifts;

namespace LCAP.HRMS.Infrastructure.Persistence.Seeds;

public static class ShiftSeedData
{
    public static readonly Guid GeneralId = Guid.Parse("a0c85f4b-a175-441a-973e-9f66d6ed0001");
    public static Shift General() => new()
    {
        Id = GeneralId,
        CompanyId = CompanySeedData.LcapId,
        ShiftCode = "GENERAL",
        ShiftName = "General Shift",
        StartTime = new TimeOnly(9, 30),
        EndTime = new TimeOnly(18, 30),
        GracePeriodMinutes = 15,
        MinimumHalfDayMinutes = null,
        MinimumFullDayMinutes = null,
        IsNightShift = false,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "system:seed"
    };
}
