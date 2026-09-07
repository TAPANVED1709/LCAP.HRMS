using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Shifts.DTOs;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class ShiftApiTests
{
    private static ShiftCreateRequest Valid(string code = "NEW") => new()
    {
        CompanyId = CompanySeedData.LcapId, ShiftCode = code, ShiftName = "Test Shift",
        StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), GracePeriodMinutes = 15
    };

    [Fact]
    public async Task Seed_and_crud_support_overnight_times_audits_and_soft_delete()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var seed = Assert.Single((await client.GetFromJsonAsync<ApiResponse<ShiftResponse[]>>("/api/shifts"))!.Data!);
        Assert.Equal("GENERAL", seed.ShiftCode);
        Assert.Equal("General Shift", seed.ShiftName);
        Assert.Equal(new TimeOnly(9, 30), seed.StartTime);
        Assert.Equal(new TimeOnly(18, 30), seed.EndTime);
        Assert.Equal(15, seed.GracePeriodMinutes);
        Assert.Null(seed.MinimumHalfDayMinutes);
        Assert.Null(seed.MinimumFullDayMinutes);
        Assert.False(seed.IsNightShift);
        var created = await client.PostAsJsonAsync("/api/shifts", Valid(" night "));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var row = (await created.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>())!.Data!;
        Assert.Equal("NIGHT", row.ShiftCode);
        Assert.True(row.IsNightShift);
        Assert.Equal(TimeSpan.FromHours(8), row.EndTime - row.StartTime);
        Assert.Equal("api-test-user", row.CreatedBy);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
        var update = new ShiftUpdateRequest
        {
            CompanyId = row.CompanyId, ShiftCode = row.ShiftCode, ShiftName = "Midnight",
            StartTime = TimeOnly.MinValue, EndTime = new TimeOnly(8, 0),
            GracePeriodMinutes = 0, MinimumHalfDayMinutes = 240, MinimumFullDayMinutes = 480, IsActive = false
        };
        var updatedHttp = await client.PutAsJsonAsync($"/api/shifts/{row.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updatedHttp.StatusCode);
        var updated = (await updatedHttp.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>())!.Data!;
        Assert.False(updated.IsNightShift);
        Assert.False(updated.IsActive);
        Assert.Equal(TimeOnly.MinValue, updated.StartTime);
        Assert.Equal(240, updated.MinimumHalfDayMinutes);
        Assert.Equal(480, updated.MinimumFullDayMinutes);
        Assert.Equal(row.CreatedAt, updated.CreatedAt);
        Assert.Equal("api-test-user", updated.UpdatedBy);
        Assert.Contains((await client.GetFromJsonAsync<ApiResponse<ShiftResponse[]>>($"/api/companies/{row.CompanyId}/shifts"))!.Data!, x => x.Id == row.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/shifts/{row.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/shifts/{row.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/shifts/{row.Id}", update)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var retained = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Shifts.IgnoreQueryFilters().SingleAsync(x => x.Id == row.Id);
        Assert.True(retained.IsDeleted);
        Assert.Equal("api-test-user", retained.UpdatedBy);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/shifts", Valid("night"))).StatusCode);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("end")]
    [InlineData("equal")]
    [InlineData("grace")]
    [InlineData("half")]
    [InlineData("full")]
    [InlineData("order")]
    [InlineData("code")]
    [InlineData("name")]
    [InlineData("company")]
    public async Task Invalid_requests_return_400(string field)
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        var request = Valid();
        switch (field)
        {
            case "start": request.StartTime = null; break;
            case "end": request.EndTime = null; break;
            case "equal": request.EndTime = request.StartTime; break;
            case "grace": request.GracePeriodMinutes = -1; break;
            case "half": request.MinimumHalfDayMinutes = -1; break;
            case "full": request.MinimumFullDayMinutes = -1; break;
            case "order": request.MinimumHalfDayMinutes = 480; request.MinimumFullDayMinutes = 240; break;
            case "code": request.ShiftCode = " "; break;
            case "name": request.ShiftName = " "; break;
            case "company": request.CompanyId = Guid.Empty; break;
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/shifts", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/shifts/{ShiftSeedData.GeneralId}", request)).StatusCode);
    }

    [Fact]
    public async Task Omitted_and_malformed_times_are_rejected()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/shifts",
            new { companyId = CompanySeedData.LcapId, shiftCode = "X", shiftName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/shifts",
            new { companyId = CompanySeedData.LcapId, shiftCode = "X", shiftName = "X", startTime = "25:00:00", endTime = "06:00:00" })).StatusCode);
    }

    [Fact]
    public async Task Codes_are_company_scoped_and_deleted_company_is_hidden()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/shifts", Valid(" general "))).StatusCode);
        var companyHttp = await client.PostAsJsonAsync("/api/companies", new CompanyCreateRequest
        { CompanyCode = "OTHER", CompanyName = "Other", PayrollDay = 1, SalaryPaymentDay = 1 });
        var company = (await companyHttp.Content.ReadFromJsonAsync<ApiResponse<CompanyResponse>>())!.Data!;
        var request = Valid("GENERAL");
        request.CompanyId = company.Id;
        var created = await client.PostAsJsonAsync("/api/shifts", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var row = (await created.Content.ReadFromJsonAsync<ApiResponse<ShiftResponse>>())!.Data!;
        await client.DeleteAsync($"/api/companies/{company.Id}");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/shifts/{row.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/shifts", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{company.Id}/shifts")).StatusCode);
    }

    [Fact]
    public async Task Authentication_paging_and_swagger_cover_all_routes()
    {
        using var factory = new CompanyApiFactory();
        using var client = factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/shifts")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Test", "allowed");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/shifts?take=0")).StatusCode);
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var (path, method) in new[]
        {
            ("/api/shifts", "get"), ("/api/shifts", "post"), ("/api/shifts/{id}", "get"),
            ("/api/shifts/{id}", "put"), ("/api/shifts/{id}", "delete"), ("/api/companies/{companyId}/shifts", "get")
        })
        {
            var operation = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        }
    }
}
