using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class CompanyApiTests
{
    private static CompanyCreateRequest Valid(string code = "NEW") => new()
    {
        CompanyCode = code, CompanyName = "New Company", State = "Bihar", PayrollDay = 1, SalaryPaymentDay = 7
    };

    [Fact]
    public async Task Crud_returns_expected_statuses_audits_and_soft_delete()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var createdHttp = await client.PostAsJsonAsync("/api/companies", Valid(" new "));
        Assert.Equal(HttpStatusCode.Created, createdHttp.StatusCode);
        var created = (await createdHttp.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        Assert.Equal("NEW", created.CompanyCode);
        Assert.Equal("India", created.Country);
        Assert.Equal("INR", created.PayrollCurrency);
        Assert.Equal("api-test-user", created.CreatedBy);
        Assert.Null(created.PAN);
        Assert.Null(created.TAN);
        Assert.Null(created.GSTIN);
        Assert.NotNull(createdHttp.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(createdHttp.Headers.Location)).StatusCode);

        var update = new CompanyUpdateRequest
        {
            CompanyCode = "NEW", CompanyName = "Updated", PayrollDay = 31, SalaryPaymentDay = 15, IsActive = false
        };
        var updatedHttp = await client.PutAsJsonAsync($"/api/companies/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updatedHttp.StatusCode);
        var updated = (await updatedHttp.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal("Updated", updated.CompanyName);
        Assert.False(updated.IsActive);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal("api-test-user", updated.UpdatedBy);

        var deleted = await client.DeleteAsync($"/api/companies/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/companies/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/companies/{created.Id}", update)).StatusCode);
        var list = await client.GetFromJsonAsync<ApiResponse<CompanyResponse[]>>("/api/companies");
        Assert.DoesNotContain(list!.Data!, item => item.Id == created.Id);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var retained = await context.Companies.IgnoreQueryFilters().SingleAsync(item => item.Id == created.Id);
        Assert.True(retained.IsDeleted);
        Assert.Equal("Updated", retained.CompanyName);
        Assert.Equal(created.CreatedAt, retained.CreatedAt);
    }

    [Fact]
    public async Task Seed_has_requested_values_without_invented_identifiers()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var list = await client.GetFromJsonAsync<ApiResponse<CompanyResponse[]>>("/api/companies");
        var seed = Assert.Single(list!.Data!);
        Assert.Equal("LCAP", seed.CompanyCode);
        Assert.Equal("LCAP", seed.CompanyName);
        Assert.Equal("Bihar", seed.State);
        Assert.Equal("India", seed.Country);
        Assert.Equal("INR", seed.PayrollCurrency);
        Assert.Equal(1, seed.PayrollDay);
        Assert.Equal(1, seed.SalaryPaymentDay);
        Assert.True(seed.IsActive);
        Assert.Null(seed.PAN);
        Assert.Null(seed.TAN);
        Assert.Null(seed.GSTIN);
    }

    [Fact]
    public async Task Duplicate_codes_return_conflict_including_deleted_codes()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var duplicate = await client.PostAsJsonAsync("/api/companies", Valid(" lcap "));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var failure = await duplicate.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(failure!.Success);
        Assert.NotEmpty(failure.TraceId);
        var response = await client.PostAsJsonAsync("/api/companies", Valid());
        var company = (await response.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/companies/{company.Id}",
            new CompanyUpdateRequest { CompanyCode = "lcap", CompanyName = "Duplicate", PayrollDay = 1, SalaryPaymentDay = 1 })).StatusCode);
        await client.DeleteAsync($"/api/companies/{company.Id}");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/companies", Valid("new"))).StatusCode);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(32, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 32)]
    public async Task Invalid_days_return_validation_errors(int payrollDay, int salaryDay)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        request.PayrollDay = payrollDay;
        request.SalaryPaymentDay = salaryDay;
        var response = await client.PostAsJsonAsync("/api/companies", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(envelope!.Success);
        Assert.NotEmpty(envelope.Errors);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    [InlineData("CODE", "")]
    [InlineData("CODE", "   ")]
    public async Task Required_code_and_name_are_validated(string code, string name)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid(code);
        request.CompanyName = name;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/companies", request)).StatusCode);
    }

    [Fact]
    public async Task Authentication_paging_and_not_found_are_enforced()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/companies")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/companies", Valid())).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/companies?take=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/companies?skip=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/companies", new { companyCode = "A", companyName = "A" })).StatusCode);
    }

    [Fact]
    public async Task Swagger_documents_operations_validation_responses_and_bearer_auth()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (path, method, status) in new[]
        {
            ("/api/companies", "get", "200"),
            ("/api/companies", "post", "201"),
            ("/api/companies/{id}", "get", "200"),
            ("/api/companies/{id}", "put", "200"),
            ("/api/companies/{id}", "delete", "204")
        })
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
        var post = paths.GetProperty("/api/companies").GetProperty("post");
        Assert.True(post.GetProperty("responses").TryGetProperty("409", out _));
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("CompanyCreateRequest");
        Assert.Equal(1, schema.GetProperty("properties").GetProperty("payrollDay").GetProperty("minimum").GetInt32());
        Assert.Equal(31, schema.GetProperty("properties").GetProperty("salaryPaymentDay").GetProperty("maximum").GetInt32());
    }
}
