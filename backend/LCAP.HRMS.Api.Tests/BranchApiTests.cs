using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Branches.DTOs;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class BranchApiTests
{
    private static BranchCreateRequest Valid(string code = "NEW") => new()
    {
        CompanyId = CompanySeedData.LcapId, BranchCode = code, BranchName = "New Branch",
        Email = "branch@example.com", Phone = "+91 1234567890"
    };

    [Fact]
    public async Task Seed_and_crud_include_defaults_audits_and_soft_deletion()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var seedList = await client.GetFromJsonAsync<ApiResponse<BranchResponse[]>>("/api/branches");
        var seed = Assert.Single(seedList!.Data!);
        Assert.Equal("PATNA-HO", seed.BranchCode);
        Assert.Equal("Patna Head Office", seed.BranchName);
        Assert.Equal("Patna", seed.City);
        Assert.Equal("Bihar", seed.State);
        Assert.Equal("India", seed.Country);
        Assert.Equal(CompanySeedData.LcapId, seed.CompanyId);
        Assert.True(seed.IsHeadOffice);
        Assert.True(seed.IsActive);

        var response = await client.PostAsJsonAsync("/api/branches", Valid(" new "));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ApiResponse<BranchResponse>>())!.Data!;
        Assert.Equal("NEW", created.BranchCode);
        Assert.Equal("Bihar", created.State);
        Assert.Equal("India", created.Country);
        Assert.Equal("api-test-user", created.CreatedBy);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(response.Headers.Location)).StatusCode);

        var updatedHttp = await client.PutAsJsonAsync($"/api/branches/{created.Id}", new BranchUpdateRequest
        {
            CompanyId = created.CompanyId, BranchCode = "NEW", BranchName = "Updated Branch",
            State = "Explicit state", IsActive = false, AddressLine1 = "Office address"
        });
        Assert.Equal(HttpStatusCode.OK, updatedHttp.StatusCode);
        var updated = (await updatedHttp.Content.ReadFromJsonAsync<ApiResponse<BranchResponse>>())!.Data!;
        Assert.Equal("Explicit state", updated.State);
        Assert.False(updated.IsActive);
        Assert.Null(updated.Email);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal("api-test-user", updated.UpdatedBy);
        Assert.NotNull(updated.UpdatedAt);
        var nested = await client.GetFromJsonAsync<ApiResponse<BranchResponse[]>>($"/api/companies/{created.CompanyId}/branches");
        Assert.Contains(nested!.Data!, item => item.Id == created.Id);

        var deleted = await client.DeleteAsync($"/api/branches/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/branches/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/branches/{created.Id}")).StatusCode);
        var list = await client.GetFromJsonAsync<ApiResponse<BranchResponse[]>>("/api/branches");
        Assert.DoesNotContain(list!.Data!, item => item.Id == created.Id);
        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Branches
            .IgnoreQueryFilters().SingleAsync(item => item.Id == created.Id);
        Assert.True(stored.IsDeleted);
        Assert.Equal("Updated Branch", stored.BranchName);
        Assert.Equal(created.CreatedAt, stored.CreatedAt);
    }

    [Fact]
    public async Task Code_is_unique_within_company_and_reserved_after_deletion()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/branches", Valid(" patna-ho "))).StatusCode);
        var otherCompany = await CreateCompany(client);
        var request = Valid("PATNA-HO");
        request.CompanyId = otherCompany.Id;
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/branches", request)).StatusCode);

        var conflictUpdate = await client.PutAsJsonAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}",
            new BranchUpdateRequest { CompanyId = otherCompany.Id, BranchCode = "patna-ho", BranchName = "Moved" });
        Assert.Equal(HttpStatusCode.Conflict, conflictUpdate.StatusCode);

        await client.DeleteAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/branches", Valid("PATNA-HO"))).StatusCode);
    }

    [Fact]
    public async Task Reassignment_checks_target_company_and_inherits_its_state()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var otherCompany = await CreateCompany(client);
        var empty = await client.GetFromJsonAsync<ApiResponse<BranchResponse[]>>($"/api/companies/{otherCompany.Id}/branches");
        Assert.Empty(empty!.Data!);
        // Patna now has a seeded work location. Exercise reassignment with a new, empty branch.
        var createdHttp = await client.PostAsJsonAsync("/api/branches", Valid("EMPTY"));
        Assert.Equal(HttpStatusCode.Created, createdHttp.StatusCode);
        var movable = (await createdHttp.Content.ReadFromJsonAsync<ApiResponse<BranchResponse>>())!.Data!;
        var response = await client.PutAsJsonAsync($"/api/branches/{movable.Id}",
            new BranchUpdateRequest { CompanyId = otherCompany.Id, BranchCode = "MOVED", BranchName = "Moved", State = "  " });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ApiResponse<BranchResponse>>())!.Data!;
        Assert.Equal(otherCompany.Id, updated.CompanyId);
        Assert.Equal("Target State", updated.State);
        Assert.DoesNotContain((await client.GetFromJsonAsync<ApiResponse<BranchResponse[]>>(
            $"/api/companies/{CompanySeedData.LcapId}/branches"))!.Data!, branch => branch.Id == movable.Id);
    }

    [Fact]
    public async Task Company_id_must_be_nonempty_and_reference_an_existing_company()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.CompanyId = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/branches", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/branches",
            new { branchCode = "MISSING", branchName = "Missing company" })).StatusCode);
        request.CompanyId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/branches", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{request.CompanyId}/branches")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}",
            new BranchUpdateRequest { CompanyId = request.CompanyId, BranchCode = "X", BranchName = "X" })).StatusCode);
    }

    [Fact]
    public async Task Deleted_company_hides_branches_without_deleting_their_rows()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/companies/{CompanySeedData.LcapId}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<BranchResponse[]>>("/api/branches"))!.Data!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/branches")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/branches", Valid())).StatusCode);
        using var scope = factory.Services.CreateScope();
        var retained = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Branches.IgnoreQueryFilters().SingleAsync();
        Assert.False(retained.IsDeleted);
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
        request.BranchName = name;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/branches", request)).StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "+91 1234567890")]
    [InlineData("valid@example.com", "not-a-phone")]
    public async Task Invalid_contacts_are_rejected(string email, string phone)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.Email = email;
        request.Phone = phone;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/branches", request)).StatusCode);
    }

    [Fact]
    public async Task Authentication_paging_and_missing_resources_return_proper_statuses()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/branches")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/branches")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/branches", Valid())).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/branches?take=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/companies/{CompanySeedData.LcapId}/branches?skip=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/branches/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/branches/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Swagger_documents_all_six_branch_operations()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var (path, method, status) in new[]
        {
            ("/api/branches", "get", "200"), ("/api/branches", "post", "201"),
            ("/api/branches/{id}", "get", "200"), ("/api/branches/{id}", "put", "200"),
            ("/api/branches/{id}", "delete", "204"), ("/api/companies/{companyId}/branches", "get", "200")
        })
        {
            var operation = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
    }

    private static async Task<CompanyResponse> CreateCompany(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/companies", new CompanyCreateRequest
        {
            CompanyCode = "OTHER", CompanyName = "Other Company", State = "Target State", PayrollDay = 1, SalaryPaymentDay = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
    }
}
