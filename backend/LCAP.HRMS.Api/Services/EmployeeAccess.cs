using LCAP.HRMS.Application.Employees;
namespace LCAP.HRMS.Api.Services;

public sealed class EmployeeAccess(IHttpContextAccessor accessor) : IEmployeeAccess
{
    private System.Security.Claims.ClaimsPrincipal? User => accessor.HttpContext?.User;
    private bool Role(string role) => User?.IsInRole(role) == true;
    private Guid? Id(string claim) => Guid.TryParse(User?.FindFirst(claim)?.Value, out var id) && id != Guid.Empty ? id : null;
    public bool IsSuperAdmin => Role("SuperAdmin");
    public bool CanWrite => IsSuperAdmin || Role("HRAdmin");
    public bool CanReadDirectory => CanWrite || Role("HRUser") || Role("PayrollAdmin");
    public bool CanReadSensitive => CanWrite || Role("PayrollAdmin");
    public bool IsManager => Role("Manager");
    public Guid? CompanyId => Id("company_id");
    public Guid? EmployeeId => Id("employee_id");
}
