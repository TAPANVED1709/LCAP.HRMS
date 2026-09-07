using LCAP.HRMS.Application.Shifts;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Branches;
using LCAP.HRMS.Application.Departments;
using LCAP.HRMS.Application.Designations;
using LCAP.HRMS.Application.WorkLocations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LCAP.HRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IBaseRepository<>), typeof(GenericRepository<>));
        services.AddScoped<LCAP.HRMS.Application.Employees.IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDesignationRepository, DesignationRepository>();
        services.AddScoped<IWorkLocationRepository, WorkLocationRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        return services;
    }
}
