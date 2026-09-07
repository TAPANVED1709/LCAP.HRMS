using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Infrastructure.Persistence.Seeds;

public static class CompanySeedData
{
    public static readonly Guid LcapId = new("ec8af472-df65-43d4-9abf-9378e553e455");

    // Deterministic migration-managed seed. Never use Guid.NewGuid() or UtcNow here.
    public static Company Lcap() => new()
    {
        Id = LcapId,
        CompanyCode = "LCAP",
        CompanyName = "LCAP",
        State = "Bihar",
        Country = "India",
        PayrollCurrency = "INR",
        PayrollDay = 1,
        SalaryPaymentDay = 1,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "system:seed",
        IsDeleted = false
        // Statutory identifiers and other unspecified optional values remain null.
    };
}
