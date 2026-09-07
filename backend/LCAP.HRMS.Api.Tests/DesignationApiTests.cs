using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Designations.DTOs;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class DesignationApiTests
{
    private static DesignationCreateRequest Valid(string code = "NEW") => new()
    {
        CompanyId = CompanySeedData.LcapId, DesignationCode = code, DesignationName = "New Designation"
    };

    private static DesignationUpdateRequest Update(DesignationResponse designation) => new()
    {
        CompanyId = designation.CompanyId, DesignationCode = designation.DesignationCode,
        DesignationName = designation.DesignationName
    };

    [Fact]
    public async Task Seeds_and_crud_preserve_defaults_audits_and_soft_deletion()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var seed = (await client.GetFromJsonAsync<ApiResponse<DesignationResponse[]>>("/api/designations"))!.Data!;
        var expected = new Dictionary<string, string>
        {
            ["EXEC"] = "Executive", ["SREXEC"] = "Senior Executive", ["TL"] = "Team Leader",
            ["MGR"] = "Manager", ["HRM"] = "HR Manager", ["PAYADMIN"] = "Payroll Admin"
        };
        Assert.Equal(6, seed.Length);
        Assert.All(seed, item =>
        {
            Assert.Equal(expected[item.DesignationCode], item.DesignationName);
            Assert.Equal(CompanySeedData.LcapId, item.CompanyId);
            Assert.True(item.IsActive);
            Assert.False(item.IsManagerial);
            Assert.Null(item.Level);
            Assert.Null(item.Grade);
        });

        var createdHttp = await client.PostAsJsonAsync("/api/designations", Valid(" new "));
        Assert.Equal(HttpStatusCode.Created, createdHttp.StatusCode);
        Assert.NotNull(createdHttp.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(createdHttp.Headers.Location)).StatusCode);
        var designation = (await createdHttp.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>())!.Data!;
        Assert.Equal("NEW", designation.DesignationCode);
        Assert.False(designation.IsManagerial);
        Assert.Null(designation.Level);
        Assert.Equal("api-test-user", designation.CreatedBy);

        var request = Update(designation);
        request.DesignationName = "Updated";
        request.Description = "Description";
        request.Grade = "G3";
        request.Level = 3;
        request.IsManagerial = true;
        request.IsActive = false;
        var updatedHttp = await client.PutAsJsonAsync($"/api/designations/{designation.Id}", request);
        Assert.Equal(HttpStatusCode.OK, updatedHttp.StatusCode);
        var updated = (await updatedHttp.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>())!.Data!;
        Assert.Equal(3, updated.Level);
        Assert.Equal("G3", updated.Grade);
        Assert.True(updated.IsManagerial);
        Assert.False(updated.IsActive);
        Assert.Equal(designation.CreatedAt, updated.CreatedAt);
        Assert.Equal("api-test-user", updated.UpdatedBy);
        var nested = await client.GetFromJsonAsync<ApiResponse<DesignationResponse[]>>($"/api/companies/{designation.CompanyId}/designations");
        Assert.Contains(nested!.Data!, item => item.Id == designation.Id);

        var clearedHttp = await client.PutAsJsonAsync($"/api/designations/{designation.Id}", Update(designation));
        Assert.Equal(HttpStatusCode.OK, clearedHttp.StatusCode);
        var cleared = (await clearedHttp.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>())!.Data!;
        Assert.Null(cleared.Level);
        Assert.Null(cleared.Grade);
        Assert.False(cleared.IsManagerial);

        var deleted = await client.DeleteAsync($"/api/designations/{designation.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/designations/{designation.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/designations/{designation.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/designations/{designation.Id}", request)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var retained = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Designations
            .IgnoreQueryFilters().SingleAsync(item => item.Id == designation.Id);
        Assert.True(retained.IsDeleted);
        Assert.Equal(designation.CreatedAt, retained.CreatedAt);
    }

    [Fact]
    public async Task Codes_are_company_scoped_and_reserved_after_deletion()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/designations", Valid(" exec "))).StatusCode);
        var companyHttp = await client.PostAsJsonAsync("/api/companies", new CompanyCreateRequest
        {
            CompanyCode = "OTHER", CompanyName = "Other", PayrollDay = 1, SalaryPaymentDay = 1
        });
        var company = (await companyHttp.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<DesignationResponse[]>>($"/api/companies/{company.Id}/designations"))!.Data!);
        var request = Valid("EXEC");
        request.CompanyId = company.Id;
        var otherHttp = await client.PostAsJsonAsync("/api/designations", request);
        Assert.Equal(HttpStatusCode.Created, otherHttp.StatusCode);
        var other = (await otherHttp.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>())!.Data!;
        var move = Update(other);
        move.CompanyId = CompanySeedData.LcapId;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/designations/{other.Id}", move)).StatusCode);
        move.DesignationCode = "MOVED";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/designations/{other.Id}", move)).StatusCode);
        var companyRows = (await client.GetFromJsonAsync<ApiResponse<DesignationResponse[]>>($"/api/companies/{company.Id}/designations"))!.Data!;
        Assert.Empty(companyRows);
        await client.DeleteAsync($"/api/designations/{other.Id}");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/designations", Valid("moved"))).StatusCode);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("  ", "Name")]
    [InlineData("CODE", "")]
    [InlineData("CODE", "  ")]
    public async Task Required_code_and_name_are_validated(string code, string name)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid(code);
        request.DesignationName = name;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/designations", request)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Level_accepts_nullable_integer_without_extra_business_limits(int? level)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.Level = level;
        var response = await client.PostAsJsonAsync("/api/designations", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(level, (await response.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>())!.Data!.Level);
    }

    [Fact]
    public async Task Invalid_company_lengths_and_level_type_are_rejected()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.CompanyId = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/designations", request)).StatusCode);
        request.CompanyId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/designations", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{request.CompanyId}/designations")).StatusCode);
        request = Valid();
        request.Grade = new string('G', 51);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/designations", request)).StatusCode);
        request.Grade = null;
        request.Description = new string('X', 1001);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/designations", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/designations",
            new { companyId = CompanySeedData.LcapId, designationCode = "BAD", designationName = "Bad", level = 1.5 })).StatusCode);
    }

    [Fact]
    public async Task Deleted_company_hides_designations_and_rejects_new_ones()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var seeded = (await client.GetFromJsonAsync<ApiResponse<DesignationResponse[]>>("/api/designations"))!.Data![0];
        await client.DeleteAsync($"/api/companies/{CompanySeedData.LcapId}");
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<DesignationResponse[]>>("/api/designations"))!.Data!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/designations/{seeded.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/designations", Valid())).StatusCode);
    }

    [Fact]
    public async Task Authentication_paging_and_swagger_cover_all_six_routes()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/designations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/designations", Valid())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/designations")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/designations?take=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/designations?skip=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/designations/{Guid.NewGuid()}")).StatusCode);
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var (path, method) in new[]
        {
            ("/api/designations", "get"), ("/api/designations", "post"), ("/api/designations/{id}", "get"),
            ("/api/designations/{id}", "put"), ("/api/designations/{id}", "delete"), ("/api/companies/{companyId}/designations", "get")
        })
        {
            var operation = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
    }
}
