using LCAP.HRMS.Application.Attendance;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace LCAP.HRMS.Api.Tests;

public sealed class AttendanceConfigurationTests
{
    [Fact]
    public void Timezone_dictionary_binds_from_configuration()
    {
        var company = Guid.NewGuid();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"Attendance:CompanyTimeZones:{company}"] = "Asia/Kolkata"
        }).Build();
        var options = config.GetSection("Attendance").Get<AttendanceOptions>()!;
        var zone = new AttendanceDateResolver(options).Zone(company, Guid.NewGuid());
        Assert.Equal(TimeSpan.FromHours(5.5), zone.GetUtcOffset(DateTimeOffset.UtcNow));
    }
}


