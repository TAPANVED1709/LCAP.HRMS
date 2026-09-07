using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.WorkLocations.DTOs;
using LCAP.HRMS.Application.Branches.DTOs;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class WorkLocationApiTests
{
    private static WorkLocationCreateRequest Valid(string code = "TEST") => new()
    {
        CompanyId = CompanySeedData.LcapId, BranchId = BranchSeedData.PatnaHeadOfficeId,
        LocationCode = code, LocationName = "Test location"
    };

    [Fact]
    public async Task Seed_and_crud_preserve_null_coordinates_defaults_and_audits()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var seed = Assert.Single((await client.GetFromJsonAsync<ApiResponse<WorkLocationResponse[]>>("/api/work-locations"))!.Data!);
        Assert.Equal("Patna Office", seed.LocationName);
        Assert.Equal("Patna", seed.City);
        Assert.Equal("Bihar", seed.State);
        Assert.Equal("India", seed.Country);
        Assert.Equal(CompanySeedData.LcapId, seed.CompanyId);
        Assert.Equal(BranchSeedData.PatnaHeadOfficeId, seed.BranchId);
        Assert.Null(seed.Latitude);
        Assert.Null(seed.Longitude);
        Assert.Equal(100, seed.AllowedRadiusMeters);
        Assert.True(seed.IsGeoFenceEnabled);

        var request = Valid(" test ");
        request.IsGeoFenceEnabled = false;
        var http = await client.PostAsJsonAsync("/api/work-locations", request);
        Assert.Equal(HttpStatusCode.Created, http.StatusCode);
        Assert.NotNull(http.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(http.Headers.Location)).StatusCode);
        var created = (await http.Content.ReadFromJsonAsync<ApiResponse<WorkLocationResponse>>())!.Data!;
        Assert.Equal("TEST", created.LocationCode);
        Assert.Equal(100, created.AllowedRadiusMeters);
        Assert.False(created.IsGeoFenceEnabled);
        Assert.Null(created.Latitude);
        var update = new WorkLocationUpdateRequest
        {
            CompanyId = created.CompanyId, BranchId = created.BranchId, LocationCode = created.LocationCode,
            LocationName = "Updated", AllowedRadiusMeters = 250, IsActive = false
        };
        var updatedHttp = await client.PutAsJsonAsync($"/api/work-locations/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updatedHttp.StatusCode);
        var updated = (await updatedHttp.Content.ReadFromJsonAsync<ApiResponse<WorkLocationResponse>>())!.Data!;
        Assert.True(updated.IsGeoFenceEnabled);
        Assert.Equal(250, updated.AllowedRadiusMeters);
        Assert.False(updated.IsActive);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal("api-test-user", updated.UpdatedBy);
        var list = (await client.GetFromJsonAsync<ApiResponse<WorkLocationResponse[]>>($"/api/branches/{created.BranchId}/work-locations"))!.Data!;
        Assert.Contains(list, item => item.Id == created.Id);
        var deleted = await client.DeleteAsync($"/api/work-locations/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/work-locations/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/work-locations/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/work-locations", Valid("test"))).StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True((await context.WorkLocations.IgnoreQueryFilters().SingleAsync(item => item.Id == created.Id)).IsDeleted);
        var storedSeed = await context.WorkLocations.SingleAsync(item => item.Id == seed.Id);
        Assert.Null(storedSeed.Latitude);
        Assert.Null(storedSeed.Longitude);
    }

    [Theory]
    [InlineData("-90.0000001", "0")]
    [InlineData("90.0000001", "0")]
    [InlineData("0", "-180.0000001")]
    [InlineData("0", "180.0000001")]
    [InlineData("1.00000001", "0")]
    [InlineData("0", "1.00000001")]
    [InlineData(null, "0")]
    [InlineData("0", null)]
    public async Task Invalid_coordinates_are_rejected(string? latitude, string? longitude)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.Latitude = latitude is null ? null : decimal.Parse(latitude, CultureInfo.InvariantCulture);
        request.Longitude = longitude is null ? null : decimal.Parse(longitude, CultureInfo.InvariantCulture);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
    }

    [Theory]
    [InlineData("-90", "-180")]
    [InlineData("90", "180")]
    [InlineData("0", "0")]
    public async Task Coordinate_boundaries_are_accepted_for_test_records(string latitude, string longitude)
    {
        // Synthetic boundary values only in an isolated test database, never the Patna seed.
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid("BOUNDARY");
        request.Latitude = decimal.Parse(latitude, CultureInfo.InvariantCulture);
        request.Longitude = decimal.Parse(longitude, CultureInfo.InvariantCulture);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Radius_must_be_positive(int radius)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.AllowedRadiusMeters = radius;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
    }

    [Fact]
    public async Task Company_branch_validation_and_branch_move_guard_are_enforced()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.CompanyId = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        request = Valid();
        request.BranchId = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        request.BranchId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        var companyHttp = await client.PostAsJsonAsync("/api/companies", new CompanyCreateRequest
        {
            CompanyCode = "OTHER", CompanyName = "Other", PayrollDay = 1, SalaryPaymentDay = 1
        });
        var company = (await companyHttp.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        request = Valid();
        request.CompanyId = company.Id;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        var move = new BranchUpdateRequest { CompanyId = company.Id, BranchCode = "PATNA-HO", BranchName = "Patna" };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}", move)).StatusCode);
        await client.DeleteAsync($"/api/work-locations/{WorkLocationSeedData.PatnaOfficeId}");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}", move)).StatusCode);
    }

    [Fact]
    public async Task Location_can_move_to_matching_branch_and_codes_are_branch_scoped()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var otherHttp = await client.PostAsJsonAsync("/api/branches", new BranchCreateRequest
        {
            CompanyId = CompanySeedData.LcapId, BranchCode = "OTHER", BranchName = "Other"
        });
        var branch = (await otherHttp.Content.ReadFromJsonAsync<ApiResponse<BranchResponse>>())!.Data!;
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<WorkLocationResponse[]>>($"/api/branches/{branch.Id}/work-locations"))!.Data!);
        var request = Valid("PATNA-OFFICE");
        request.BranchId = branch.Id;
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        var update = new WorkLocationUpdateRequest
        {
            CompanyId = branch.CompanyId, BranchId = branch.Id, LocationCode = "PATNA-OFFICE", LocationName = "Move"
        };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/work-locations/{WorkLocationSeedData.PatnaOfficeId}", update)).StatusCode);
        update.LocationCode = "MOVED";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/work-locations/{WorkLocationSeedData.PatnaOfficeId}", update)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deleted_branch_or_company_hides_locations(bool deleteCompany)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var path = deleteCompany ? $"/api/companies/{CompanySeedData.LcapId}" : $"/api/branches/{BranchSeedData.PatnaHeadOfficeId}";
        await client.DeleteAsync(path);
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<WorkLocationResponse[]>>("/api/work-locations"))!.Data!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/work-locations/{WorkLocationSeedData.PatnaOfficeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/work-locations", Valid())).StatusCode);
    }

    [Fact]
    public async Task Auth_paging_required_fields_and_swagger_are_correct()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/work-locations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}/work-locations")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/work-locations?take=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/branches/{BranchSeedData.PatnaHeadOfficeId}/work-locations?skip=-1")).StatusCode);
        var request = Valid(" ");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        request = Valid();
        request.LocationName = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/work-locations", request)).StatusCode);
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var (path, method) in new[]
        {
            ("/api/work-locations", "get"), ("/api/work-locations", "post"), ("/api/work-locations/{id}", "get"),
            ("/api/work-locations/{id}", "put"), ("/api/work-locations/{id}", "delete"), ("/api/branches/{branchId}/work-locations", "get")
        })
        {
            var operation = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
    }
}
