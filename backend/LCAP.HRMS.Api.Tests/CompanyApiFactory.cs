using System.Security.Claims;
using System.Text.Encodings.Web;
using LCAP.HRMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LCAP.HRMS.Api.Tests;

public sealed class CompanyApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    private readonly TimeProvider? _clock;
    private readonly LCAP.HRMS.Application.Attendance.IGeoDistanceService? _distance;
    public Action<IServiceCollection>? CustomizeServices { get; set; }
    public CompanyApiFactory(TimeProvider? clock = null, LCAP.HRMS.Application.Attendance.IGeoDistanceService? distance = null)
    {
        _clock = clock;
        _distance = distance;
        _connection.Open();
        _connection.CreateCollation("Latin1_General_100_CI_AS",
            (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            if (_clock is not null) { services.RemoveAll<TimeProvider>(); services.AddSingleton(_clock); }
            if (_distance is not null) { services.RemoveAll<LCAP.HRMS.Application.Attendance.IGeoDistanceService>(); services.AddSingleton(_distance); }
            CustomizeServices?.Invoke(services);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        });
    }

    public HttpClient CreateCompanyClient(bool authenticated = true)
    {
        var client = CreateClient();
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        if (authenticated)
            client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        return client;
    }

    public HttpClient CreateEmployeeClient(string role = "SuperAdmin", Guid? companyId = null, Guid? employeeId = null)
    {
        var client = CreateCompanyClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (companyId is {} company) client.DefaultRequestHeaders.Add("X-Test-Company", company.ToString());
        if (employeeId is {} employee) client.DefaultRequestHeaders.Add("X-Test-Employee", employee.ToString());
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.Authorization != "Test allowed")
                return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> {new("sub", "api-test-user")};
            foreach (var (header, claim) in new[] { ("X-Test-Role", ClaimTypes.Role), ("X-Test-Company", "company_id"), ("X-Test-Employee", "employee_id") })
                if (Request.Headers.TryGetValue(header,out var value)) claims.Add(new(claim,value.ToString()));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
