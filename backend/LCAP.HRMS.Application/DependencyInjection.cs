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
        services.AddScoped<Regularisation.IRegularisationService, Regularisation.RegularisationService>();
        services.AddScoped<Regularisation.IAttendanceEffectiveStateService, Regularisation.AttendanceEffectiveStateService>();
        services.AddScoped<Regularisation.CorrectionEvaluationService>();
        services.AddScoped<AttendancePolicies.IAttendancePolicyService, AttendancePolicies.AttendancePolicyService>();
        services.AddScoped<AttendancePolicies.IAttendancePolicyResolver, AttendancePolicies.CompanyDefaultAttendancePolicyResolver>();
        services.AddScoped<AttendancePolicies.IAttendanceEvaluationService, AttendancePolicies.AttendanceEvaluationService>();
        services.AddScoped<Attendance.IAttendanceService, Attendance.AttendanceService>();
        services.AddSingleton<Attendance.IGeoDistanceService, Attendance.GeoDistanceService>();
        services.AddSingleton<Attendance.AttendanceDateResolver>();
        services.AddScoped<Employees.IEmployeeService, Employees.EmployeeService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDesignationService, DesignationService>();
        services.AddScoped<IWorkLocationService, WorkLocationService>();
        services.AddScoped<IShiftService, ShiftService>();
        return services;
    }
}
