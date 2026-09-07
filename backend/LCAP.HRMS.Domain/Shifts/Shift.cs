using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Domain.Companies;

namespace LCAP.HRMS.Domain.Shifts;

public sealed class Shift : BaseEntity
{
    public Guid CompanyId { get; set; } = Guid.Empty;

    public string ShiftCode { get; set; } = "";

    public string ShiftName { get; set; } = "";

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int GracePeriodMinutes { get; set; }
    public int? MinimumHalfDayMinutes { get; set; }
    public int? MinimumFullDayMinutes { get; set; }
    public bool IsNightShift { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
}
