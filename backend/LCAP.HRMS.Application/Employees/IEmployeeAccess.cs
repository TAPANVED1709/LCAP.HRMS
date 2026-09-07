namespace LCAP.HRMS.Application.Employees;

public interface IEmployeeAccess
{
    bool IsSuperAdmin { get; }
    bool CanWrite { get; }
    bool CanReadDirectory { get; }
    bool CanReadSensitive { get; }
    bool IsManager { get; }
    Guid? CompanyId { get; }
    Guid? EmployeeId { get; }
}
