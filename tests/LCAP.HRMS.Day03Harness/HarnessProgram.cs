using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LCAP.HRMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LCAP.HRMS.Day03Harness;

// Test-only host. References the real API without changing its production configuration.
public static class HarnessProgram
{
    public static async Task Main(string[] args)
    {
        var root = Path.GetFullPath(args[0]);
        var database = args[1];
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^LCAP_HRMS_Day03_[A-Za-z0-9_]+$"))
            throw new ArgumentException("Only isolated Day03 test databases are accepted.");
        var connection = $"Server=localhost;Database={database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
        using var factory = new SqlApiFactory(root, connection);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using (var scope = factory.Services.CreateScope())
        {
            var actual = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.GetDbConnection().Database;
            if (actual != database) throw new InvalidOperationException("Refusing to use a non-test database.");
        }
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: SqlApiFactory.Issuer, audience: "lcap-hrms-api",
            claims: [new Claim("sub", "day03-integration-tester"), new Claim("role", "SuperAdmin")],
            notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(factory.Key, SecurityAlgorithms.HmacSha256)));
        var output = Path.Combine(root, "artifacts", "day03");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "access-token.txt"), token);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [], ContentRootPath = root,
            WebRootPath = Path.Combine(root, "frontend", "lcap-hrms-web", "dist", "lcap-hrms-web", "browser")
        });
        builder.WebHost.UseUrls("http://127.0.0.1:5093");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        var app = builder.Build();
        app.MapMethods("/api/{**path}", ["GET","POST","PUT","DELETE","OPTIONS"], Forward);
        app.MapGet("/swagger/{**path}", Forward);
        // Isolated loopback-only test issuer. This host is never deployed with the product.
        app.MapPost("/test/token", (TestIdentity identity) =>
        {
            var claims = new List<Claim> {new("sub", "day03-role-test"),new("role",identity.Role)};
            if(identity.CompanyId is {} company) claims.Add(new("company_id",company.ToString()));
            if(identity.EmployeeId is {} employee) claims.Add(new("employee_id",employee.ToString()));
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(SqlApiFactory.Issuer,"lcap-hrms-api",claims,
                DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddHours(1),new SigningCredentials(factory.Key,SecurityAlgorithms.HmacSha256)));
        });
        app.UseStaticFiles();
        // Explicit synthetic GPS route for the browser audit only; production assets are unchanged.
        app.MapGet("/test/attendance", async context => {
            var html = await File.ReadAllTextAsync(Path.Combine(builder.Environment.WebRootPath,"index.html"));
            var shim = "<script>navigator.geolocation.getCurrentPosition=(ok,fail,options)=>setTimeout(()=>ok({coords:{latitude:0,longitude:0,accuracy:10},timestamp:Date.now()}),150);</script>";
            context.Response.ContentType="text/html";
            await context.Response.WriteAsync(html.Replace("<head>","<head>"+shim));
        });
        app.MapFallbackToFile("index.html");
        Console.WriteLine($"Day03 SQL integration host ready at http://127.0.0.1:5093; database={database}");
        await app.RunAsync();

        async Task Forward(HttpContext context)
        {
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0)
            {
                request.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType is string contentType)
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
            foreach (var header in new[] { "Authorization", "Origin", "Access-Control-Request-Method", "Access-Control-Request-Headers" })
                if (context.Request.Headers.TryGetValue(header, out var value)) request.Headers.TryAddWithoutValidation(header, value.ToArray());
            using var response = await client.SendAsync(request);
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers) context.Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in response.Content.Headers) context.Response.Headers[header.Key] = header.Value.ToArray();
            context.Response.Headers.Remove("transfer-encoding");
            // Kestrel forbids body writes for responses that have no HTTP body.
            if (context.Response.StatusCode is not (204 or 304) && context.Response.StatusCode >= 200)
                await response.Content.CopyToAsync(context.Response.Body);
        }
    }
}
public sealed class SqlApiFactory(string root, string connection) : WebApplicationFactory<global::Program>
{
    public const string Issuer = "https://day03.test.invalid";
    public SymmetricSecurityKey Key { get; } = new(RandomNumberGenerator.GetBytes(32));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.Combine(root, "backend", "LCAP.HRMS.Api"));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(Key);
                options.Authority = null;
                options.MetadataAddress = string.Empty;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer, ValidAudience = "lcap-hrms-api", IssuerSigningKey = Key,
                    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                    ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.Zero, RoleClaimType = "role"
                };
            });
        });
    }
}


public sealed record TestIdentity(string Role, Guid? CompanyId, Guid? EmployeeId);

