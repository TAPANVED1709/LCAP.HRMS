using LCAP.HRMS.Application.Attendance;
using Xunit;
namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class GeoAttendanceTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 1, 111195.0802)]
    [InlineData(0, 179.5, 0, -179.5, 111195.0802)]
    [InlineData(90, 0, -90, 0, 20015114.442)]
    public void Known_great_circle_distances(double a, double b, double c, double d, double expected) =>
        Assert.InRange(new GeoDistanceService().DistanceMeters(a, b, c, d), Math.Max(0, expected - .01), expected + .01);
    [Fact]
    public void Date_resolver_uses_branch_override_and_overnight_start_date()
    {
        var company = Guid.NewGuid(); var branch = Guid.NewGuid(); var options = new AttendanceOptions(); options.CompanyTimeZones[company.ToString()] = "Asia/Kolkata"; options.BranchTimeZones[branch.ToString()] = "UTC";
        var resolver = new AttendanceDateResolver(options); var now = new DateTimeOffset(2026, 9, 7, 20, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 9, 8), AttendanceDateResolver.Resolve(now, resolver.Zone(company, Guid.Empty), new(9, 0), new(18, 0)));
        Assert.Equal(new DateOnly(2026, 9, 7), AttendanceDateResolver.Resolve(now, resolver.Zone(company, Guid.Empty), new(22, 0), new(6, 0)));
        Assert.Equal(new DateOnly(2026, 9, 7), AttendanceDateResolver.Resolve(now, resolver.Zone(company, branch), new(9, 0), new(18, 0)));
    }
}

