using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Departments.DTOs;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class DepartmentApiTests
{
    private static DepartmentCreateRequest Valid(string code = "NEW", Guid? parent = null) => new()
    {
        CompanyId = CompanySeedData.LcapId, DepartmentCode = code, DepartmentName = "New Department",
        ParentDepartmentId = parent
    };

    private static DepartmentUpdateRequest Update(DepartmentResponse department, Guid? parent = null) => new()
    {
        CompanyId = department.CompanyId, DepartmentCode = department.DepartmentCode,
        DepartmentName = department.DepartmentName, ParentDepartmentId = parent
    };

    private static async Task<DepartmentResponse> Create(HttpClient client, string code, Guid? parent = null)
    {
        var http = await client.PostAsJsonAsync("/api/departments", Valid(code, parent));
        Assert.Equal(HttpStatusCode.Created, http.StatusCode);
        Assert.NotNull(http.Headers.Location);
        return (await http.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>())!.Data!;
    }

    [Fact]
    public async Task Seeds_and_crud_preserve_audits_and_soft_delete()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var seed = (await client.GetFromJsonAsync<ApiResponse<DepartmentResponse[]>>("/api/departments"))!.Data!;
        Assert.Equal(5, seed.Length);
        var expected = new Dictionary<string, string>
        {
            ["HR"] = "Human Resources", ["FIN"] = "Finance", ["SALES"] = "Sales",
            ["OPS"] = "Operations", ["MGMT"] = "Management"
        };
        Assert.All(seed, item =>
        {
            Assert.Equal(expected[item.DepartmentCode], item.DepartmentName);
            Assert.Equal(CompanySeedData.LcapId, item.CompanyId);
            Assert.True(item.IsActive);
            Assert.Null(item.ParentDepartmentId);
        });

        var department = await Create(client, " new ");
        Assert.Equal("NEW", department.DepartmentCode);
        Assert.Equal("api-test-user", department.CreatedBy);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/departments/{department.Id}")).StatusCode);
        var request = Update(department);
        request.DepartmentName = "Updated";
        request.Description = "Description";
        request.IsActive = false;
        var result = await client.PutAsJsonAsync($"/api/departments/{department.Id}", request);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var updated = (await result.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>())!.Data!;
        Assert.Equal(department.CreatedAt, updated.CreatedAt);
        Assert.Equal("api-test-user", updated.UpdatedBy);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal("Description", updated.Description);
        Assert.False(updated.IsActive);
        var nested = await client.GetFromJsonAsync<ApiResponse<DepartmentResponse[]>>($"/api/companies/{department.CompanyId}/departments");
        Assert.Contains(nested!.Data!, item => item.Id == department.Id);
        var deleted = await client.DeleteAsync($"/api/departments/{department.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/departments/{department.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/departments/{department.Id}")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var retained = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Departments
            .IgnoreQueryFilters().SingleAsync(item => item.Id == department.Id);
        Assert.True(retained.IsDeleted);
        Assert.Equal("Updated", retained.DepartmentName);
        Assert.Equal(department.CreatedAt, retained.CreatedAt);
    }

    [Fact]
    public async Task Hierarchy_rejects_self_and_longer_cycles_and_can_clear_parent()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var a = await Create(client, "A");
        var b = await Create(client, "B", a.Id);
        var c = await Create(client, "C", b.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/departments/{a.Id}", Update(a, a.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/departments/{a.Id}", Update(a, c.Id))).StatusCode);
        var cleared = await client.PutAsJsonAsync($"/api/departments/{b.Id}", Update(b));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.Null((await cleared.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>())!.Data!.ParentDepartmentId);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/departments/{a.Id}", Update(a, c.Id))).StatusCode);
    }

    [Fact]
    public async Task Nondeleted_children_block_parent_deletion_until_reassigned_or_deleted()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var parent = await Create(client, "PARENT");
        var child = await Create(client, "CHILD", parent.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/departments/{parent.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/departments/{child.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/departments/{parent.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/departments", Valid("INVALID", parent.Id))).StatusCode);
    }

    [Fact]
    public async Task Codes_are_unique_per_company_and_parent_cannot_cross_companies()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/departments", Valid(" hr "))).StatusCode);
        var companyHttp = await client.PostAsJsonAsync("/api/companies", new CompanyCreateRequest
        {
            CompanyCode = "OTHER", CompanyName = "Other", PayrollDay = 1, SalaryPaymentDay = 1
        });
        var company = (await companyHttp.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        var other = Valid("HR");
        other.CompanyId = company.Id;
        var otherHttp = await client.PostAsJsonAsync("/api/departments", other);
        Assert.Equal(HttpStatusCode.Created, otherHttp.StatusCode);
        var otherDepartment = (await otherHttp.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>())!.Data!;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/departments", Valid("CROSS", otherDepartment.Id))).StatusCode);
        var moved = Update(otherDepartment);
        moved.CompanyId = CompanySeedData.LcapId;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/departments/{otherDepartment.Id}", moved)).StatusCode);
        var local = await Create(client, "LOCAL");
        var duplicate = Update(local);
        duplicate.DepartmentCode = "HR";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/departments/{local.Id}", duplicate)).StatusCode);
        await client.DeleteAsync($"/api/departments/{local.Id}");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/departments", Valid("local"))).StatusCode);
    }

    [Fact]
    public async Task Invalid_or_missing_company_and_parent_are_rejected()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.CompanyId = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/departments", request)).StatusCode);
        request.CompanyId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/departments", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{request.CompanyId}/departments")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/departments", Valid("BAD", Guid.Empty))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/departments", Valid("BAD", Guid.NewGuid()))).StatusCode);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("  ", "Name")]
    [InlineData("CODE", "")]
    [InlineData("CODE", "  ")]
    public async Task Code_and_name_are_required(string code, string name)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid(code);
        request.DepartmentName = name;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/departments", request)).StatusCode);
    }

    [Fact]
    public async Task Deleted_company_hides_departments_and_rejects_new_ones()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var department = await Create(client, "NEW");
        await client.DeleteAsync($"/api/companies/{CompanySeedData.LcapId}");
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<DepartmentResponse[]>>("/api/departments"))!.Data!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/departments/{department.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/departments", Valid("OTHER"))).StatusCode);
    }

    [Fact]
    public async Task Authentication_paging_and_swagger_cover_all_routes()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/departments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/departments", Valid())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/departments")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/departments?take=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/departments?skip=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/departments/{Guid.NewGuid()}")).StatusCode);
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var (path, method) in new[]
        {
            ("/api/departments", "get"), ("/api/departments", "post"), ("/api/departments/{id}", "get"),
            ("/api/departments/{id}", "put"), ("/api/departments/{id}", "delete"),
            ("/api/companies/{companyId}/departments", "get")
        })
        {
            var operation = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
    }
}
