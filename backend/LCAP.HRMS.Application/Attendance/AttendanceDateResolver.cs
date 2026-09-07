namespace LCAP.HRMS.Application.Attendance;

public sealed class AttendanceOptions
{
    public decimal MaximumGpsAccuracyMeters { get; set; } = 100;
    public int MaximumLocationAgeSeconds { get; set; } = 120;
    public int MaximumFutureClockSkewSeconds { get; set; } = 30;
    public string DefaultTimeZone { get; set; } = "UTC";
    public Dictionary<string, string> CompanyTimeZones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> BranchTimeZones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
public sealed class AttendanceDateResolver(AttendanceOptions options)
{
    public TimeZoneInfo Zone(Guid companyId, Guid branchId) => TimeZoneInfo.FindSystemTimeZoneById(
        options.BranchTimeZones.GetValueOrDefault(branchId.ToString()) ?? options.CompanyTimeZones.GetValueOrDefault(companyId.ToString()) ?? options.DefaultTimeZone);
    public static string DisplayZone(TimeZoneInfo zone) => TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var iana) ? iana : zone.Id;
    public static DateOnly Resolve(DateTimeOffset now, TimeZoneInfo zone, TimeOnly start, TimeOnly end)
    {
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var date = DateOnly.FromDateTime(local.DateTime);
        // Before an overnight shift's end belongs to the preceding start date.
        return end < start && TimeOnly.FromDateTime(local.DateTime) < end ? date.AddDays(-1) : date;
    }
}

