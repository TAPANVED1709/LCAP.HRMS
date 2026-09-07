namespace LCAP.HRMS.Application.Shifts.DTOs;

public sealed class ShiftResponse
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; } = Guid.Empty;
    public string ShiftCode { get; init; } = "";
    public string ShiftName { get; init; } = "";
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int GracePeriodMinutes { get; init; }
    public int? MinimumHalfDayMinutes { get; init; }
    public int? MinimumFullDayMinutes { get; init; }
    public bool IsNightShift { get; init; } = false;
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
}
