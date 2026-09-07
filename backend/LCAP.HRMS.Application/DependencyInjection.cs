using LCAP.HRMS.Application.Shifts;
using Microsoft.Extensions.DependencyInjection;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Branches;
using LCAP.HRMS.Application.Departments;
using LCAP.HRMS.Application.Designations;
using LCAP.HRMS.Application.WorkLocations;

namespace LCAP.HRMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDesignationService, DesignationService>();
        services.AddScoped<IWorkLocationService, WorkLocationService>();
        services.AddScoped<IShiftService, ShiftService>();
        return services;
    }
}
