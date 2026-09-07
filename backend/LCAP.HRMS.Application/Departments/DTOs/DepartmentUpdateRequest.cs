namespace LCAP.HRMS.Application.Departments.DTOs;

// PUT replaces writable fields; the owning company cannot be changed.
public sealed class DepartmentUpdateRequest : DepartmentWriteRequest;
