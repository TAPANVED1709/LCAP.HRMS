using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Employees.DTOs;
using LCAP.HRMS.Domain.Enums;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class EmployeeApiTests
{
    private static EmployeeCreateRequest Valid(CompanyApiFactory factory,string code="TEST-EMP-001")
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return new() {CompanyId=CompanySeedData.LcapId,BranchId=db.Branches.First().Id,DepartmentId=db.Departments.First().Id,
            DesignationId=db.Designations.First().Id,ShiftId=db.Shifts.First().Id,WorkLocationId=db.WorkLocations.First().Id,
            EmployeeCode=code,FirstName="Test",LastName="Employee",MobileNumber="0000000000",DateOfJoining=new(2026,1,1),
            EmploymentType=EmploymentType.Permanent,EmployeeStatus=EmployeeStatus.Active};
    }
    private static async Task<EmployeeDetailResponse> Create(HttpClient client,EmployeeWriteRequest request)
    {
        var response=await client.PostAsJsonAsync("/api/employees",request);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!;
    }
    [Fact]
    public async Task Crud_duplicate_trim_audit_soft_delete_and_omitted_sensitive_list()
    {
        using var factory=new CompanyApiFactory();using var client=factory.CreateEmployeeClient();var r=Valid(factory," test-emp-001 ");
        // Deliberately synthetic format fixture; never a production seed.
        r.PAN="ABCDE1234F";r.AadhaarNumber="000000000000";r.BankAccountNumber="000000000000";
        var employee=await Create(client,r);Assert.Equal("TEST-EMP-001",employee.EmployeeCode);Assert.Equal("api-test-user",employee.CreatedBy);
        var list=await client.GetStringAsync("/api/employees");
        foreach(var field in new[]{"pan","aadhaarNumber","bankAccountNumber","uan","esicNumber"})Assert.DoesNotContain('"'+field+'"',list);
        Assert.DoesNotContain(r.PAN,list);Assert.DoesNotContain(r.AadhaarNumber,list);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/employees",r)).StatusCode);
        r.FirstName="Updated";r.IsActive=false;
        var updated=await client.PutAsJsonAsync("/api/employees/"+employee.Id,r);Assert.Equal(HttpStatusCode.OK,updated.StatusCode);
        Assert.False((await updated.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailResponse>>())!.Data!.IsActive);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync("/api/employees/"+employee.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/employees/"+employee.Id)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ApiResponse<EmployeeListResponse[]>>("/api/employees"))!.Data!);
        using var scope=factory.Services.CreateScope();var row=await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Employees.IgnoreQueryFilters().SingleAsync();
        Assert.True(row.IsDeleted);Assert.NotNull(row.UpdatedAt);Assert.Equal(employee.CreatedAt,row.CreatedAt);
    }
    [Theory]
    [InlineData("pan")][InlineData("aadhaar")][InlineData("ifsc")][InlineData("dob")][InlineData("joining")]
    [InlineData("confirmation")][InlineData("lastWorking")][InlineData("company")][InlineData("branch")]
    [InlineData("department")][InlineData("designation")][InlineData("shift")][InlineData("location")]
    [InlineData("firstName")][InlineData("mobile")][InlineData("email")][InlineData("enum")]
    public async Task Invalid_fields_rejected_without_echoing_values(string field)
    {
        using var factory=new CompanyApiFactory();using var client=factory.CreateEmployeeClient();var r=Valid(factory);
        switch(field){case "pan":r.PAN="sensitive-invalid";break;case "aadhaar":r.AadhaarNumber="sensitive-invalid";break;case "ifsc":r.IFSCCode="sensitive-invalid";break;
            case "dob":r.DateOfBirth=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));break;case "joining":r.DateOfJoining=null;break;
            case "confirmation":r.DateOfConfirmation=new(2025,1,1);break;case "lastWorking":r.LastWorkingDate=new(2025,1,1);break;
            case "company":r.CompanyId=Guid.NewGuid();break;case "branch":r.BranchId=Guid.NewGuid();break;case "department":r.DepartmentId=Guid.NewGuid();break;
            case "designation":r.DesignationId=Guid.NewGuid();break;case "shift":r.ShiftId=Guid.NewGuid();break;case "location":r.WorkLocationId=Guid.NewGuid();break;
            case "firstName":r.FirstName=" ";break;case "mobile":r.MobileNumber="invalid";break;case "email":r.OfficialEmail="invalid";break;case "enum":r.EmployeeStatus=(EmployeeStatus)999;break;}
        var response=await client.PostAsJsonAsync("/api/employees",r);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.DoesNotContain("sensitive-invalid",await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task Hierarchy_allows_chain_rejects_self_two_and_three_person_cycles()
    {
        using var factory=new CompanyApiFactory();using var client=factory.CreateEmployeeClient();var a=Valid(factory,"A");var ea=await Create(client,a);
        a.ReportingManagerId=ea.Id;Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync("/api/employees/"+ea.Id,a)).StatusCode);
        var b=Valid(factory,"B");b.ReportingManagerId=ea.Id;var eb=await Create(client,b);
        a.ReportingManagerId=eb.Id;Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync("/api/employees/"+ea.Id,a)).StatusCode);
        var c=Valid(factory,"C");c.ReportingManagerId=eb.Id;var ec=await Create(client,c);
        a.ReportingManagerId=ec.Id;Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync("/api/employees/"+ea.Id,a)).StatusCode);
        var chain=(await client.GetFromJsonAsync<ApiResponse<EmployeeLookupResponse[]>>($"/api/employees/{ec.Id}/reporting-chain"))!.Data!;
        Assert.Equal(new[]{eb.Id,ea.Id},chain.Select(e=>e.Id));
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync("/api/employees/"+ea.Id)).StatusCode);
        using var manager=factory.CreateEmployeeClient("Manager",a.CompanyId,ea.Id);
        var team=(await manager.GetFromJsonAsync<ApiResponse<EmployeeListResponse[]>>($"/api/employees/{ea.Id}/direct-reports"))!.Data!;
        Assert.Equal(eb.Id,Assert.Single(team).Id);
        Assert.Equal(HttpStatusCode.Forbidden,(await manager.GetAsync($"/api/employees/{eb.Id}/direct-reports")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await manager.GetAsync($"/api/employees/{ec.Id}")).StatusCode);
    }
    [Theory]
    [InlineData("HRAdmin",true,true)][InlineData("SuperAdmin",true,true)][InlineData("HRUser",false,false)]
    [InlineData("PayrollAdmin",false,true)][InlineData("Manager",false,false)][InlineData("Employee",false,false)]
    public async Task Role_and_company_scope_enforced(string role,bool write,bool sensitive)
    {
        using var factory=new CompanyApiFactory();using var admin=factory.CreateEmployeeClient();var r=Valid(factory);r.PAN="ABCDE1234F";var employee=await Create(admin,r);
        using var scoped=factory.CreateEmployeeClient(role,r.CompanyId,employee.Id);
        var profile=(await scoped.GetFromJsonAsync<ApiResponse<EmployeeDetailResponse>>("/api/employees/"+employee.Id))!.Data!;
        Assert.Equal(sensitive,profile.CanViewSensitive);Assert.Equal(sensitive?r.PAN:null,profile.PAN);
        Assert.Equal(write?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await scoped.PutAsJsonAsync("/api/employees/"+employee.Id,r)).StatusCode);
        using var other=factory.CreateEmployeeClient(role,Guid.NewGuid(),employee.Id);
        Assert.Equal(role=="SuperAdmin"?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await other.GetAsync("/api/employees/"+employee.Id)).StatusCode);
    }
    [Fact]
    public async Task Anonymous_unknown_roles_and_missing_scope_are_denied()
    {
        using var factory=new CompanyApiFactory();using var anonymous=factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync("/api/employees")).StatusCode);
        using var missing=factory.CreateEmployeeClient("HRAdmin");Assert.Equal(HttpStatusCode.Forbidden,(await missing.GetAsync("/api/employees")).StatusCode);
        using var unknown=factory.CreateEmployeeClient("Unknown");Assert.Equal(HttpStatusCode.Forbidden,(await unknown.GetAsync("/api/employees")).StatusCode);
    }
    [Fact]
    public async Task Ownership_and_company_scoped_uniqueness_and_master_guards()
    {
        using var factory=new CompanyApiFactory();using var client=factory.CreateEmployeeClient();var r=Valid(factory);await Create(client,r);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company=new Domain.Companies.Company{CompanyCode="TEST-OTHER",CompanyName="Test Other",PayrollDay=1,SalaryPaymentDay=1};
        db.Add(company);await db.SaveChangesAsync();
        var branch=new Domain.Branches.Branch{CompanyId=company.Id,BranchCode="OTHER",BranchName="Other"};
        var department=new Domain.Departments.Department{CompanyId=company.Id,DepartmentCode="OTHER",DepartmentName="Other"};
        var designation=new Domain.Designations.Designation{CompanyId=company.Id,DesignationCode="OTHER",DesignationName="Other"};
        var shift=new Domain.Shifts.Shift{CompanyId=company.Id,ShiftCode="OTHER",ShiftName="Other",StartTime=new(9,0),EndTime=new(17,0)};
        db.AddRange(branch,department,designation,shift);await db.SaveChangesAsync();
        var location=new Domain.WorkLocations.WorkLocation{CompanyId=company.Id,BranchId=branch.Id,LocationCode="OTHER",LocationName="Other"};db.Add(location);await db.SaveChangesAsync();
        foreach(var invalid in new[]{"branch","department","designation","shift","location"})
        {
            var bad=Valid(factory,"BAD");switch(invalid){case "branch":bad.BranchId=branch.Id;break;case "department":bad.DepartmentId=department.Id;break;case "designation":bad.DesignationId=designation.Id;break;case "shift":bad.ShiftId=shift.Id;break;case "location":bad.WorkLocationId=location.Id;break;}
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/employees",bad)).StatusCode);
        }
        r.CompanyId=company.Id;r.BranchId=branch.Id;r.DepartmentId=department.Id;r.DesignationId=designation.Id;r.ShiftId=shift.Id;r.WorkLocationId=location.Id;
        await Create(client,r);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync("/api/shifts/"+shift.Id)).StatusCode);
    }
}
