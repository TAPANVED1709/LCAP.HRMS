using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Application.Employees.DTOs;
namespace LCAP.HRMS.Application.Employees;
public static class EmployeeMapping
{
 public static void Apply(Employee e, EmployeeWriteRequest r)
 {
 e.CompanyId = r.CompanyId;
 e.BranchId = r.BranchId;
 e.DepartmentId = r.DepartmentId;
 e.DesignationId = r.DesignationId;
 e.ShiftId = r.ShiftId;
 e.WorkLocationId = r.WorkLocationId;
 e.ReportingManagerId = r.ReportingManagerId;
 e.EmployeeCode = r.EmployeeCode.Trim().ToUpperInvariant();
 e.FirstName = r.FirstName.Trim();
 e.MiddleName = r.MiddleName?.Trim() is { Length: > 0 } valueMiddleName ? valueMiddleName : null;
 e.LastName = r.LastName?.Trim() is { Length: > 0 } valueLastName ? valueLastName : null;
 e.MobileNumber = r.MobileNumber.Trim();
 e.AlternateMobileNumber = r.AlternateMobileNumber?.Trim() is { Length: > 0 } valueAlternateMobileNumber ? valueAlternateMobileNumber : null;
 e.PersonalEmail = r.PersonalEmail?.Trim() is { Length: > 0 } valuePersonalEmail ? valuePersonalEmail : null;
 e.OfficialEmail = r.OfficialEmail?.Trim() is { Length: > 0 } valueOfficialEmail ? valueOfficialEmail : null;
 e.DateOfBirth = r.DateOfBirth;
 e.Gender = r.Gender?.Trim() is { Length: > 0 } valueGender ? valueGender : null;
 e.BloodGroup = r.BloodGroup?.Trim() is { Length: > 0 } valueBloodGroup ? valueBloodGroup : null;
 e.DateOfJoining = r.DateOfJoining!.Value;
 e.DateOfConfirmation = r.DateOfConfirmation;
 e.EmploymentType = r.EmploymentType!.Value;
 e.EmployeeStatus = r.EmployeeStatus!.Value;
 e.ProbationEndDate = r.ProbationEndDate;
 e.DateOfResignation = r.DateOfResignation;
 e.LastWorkingDate = r.LastWorkingDate;
 e.ExitReason = r.ExitReason?.Trim() is { Length: > 0 } valueExitReason ? valueExitReason : null;
 e.PAN = r.PAN?.Trim() is { Length: > 0 } valuePAN ? valuePAN : null;
 e.AadhaarNumber = r.AadhaarNumber?.Trim() is { Length: > 0 } valueAadhaarNumber ? valueAadhaarNumber : null;
 e.UAN = r.UAN?.Trim() is { Length: > 0 } valueUAN ? valueUAN : null;
 e.ESICNumber = r.ESICNumber?.Trim() is { Length: > 0 } valueESICNumber ? valueESICNumber : null;
 e.BankName = r.BankName?.Trim() is { Length: > 0 } valueBankName ? valueBankName : null;
 e.AccountHolderName = r.AccountHolderName?.Trim() is { Length: > 0 } valueAccountHolderName ? valueAccountHolderName : null;
 e.BankAccountNumber = r.BankAccountNumber?.Trim() is { Length: > 0 } valueBankAccountNumber ? valueBankAccountNumber : null;
 e.IFSCCode = r.IFSCCode?.Trim() is { Length: > 0 } valueIFSCCode ? valueIFSCCode : null;
 e.ProfilePhotoUrl = r.ProfilePhotoUrl?.Trim() is { Length: > 0 } valueProfilePhotoUrl ? valueProfilePhotoUrl : null;
 e.Notes = r.Notes?.Trim() is { Length: > 0 } valueNotes ? valueNotes : null;
 e.IsActive = r.IsActive;
 }
 public static EmployeeDetailResponse Detail(Employee e, bool sensitive) => new()
 {
 CompanyId = e.CompanyId,
 BranchId = e.BranchId,
 DepartmentId = e.DepartmentId,
 DesignationId = e.DesignationId,
 ShiftId = e.ShiftId,
 WorkLocationId = e.WorkLocationId,
 ReportingManagerId = e.ReportingManagerId,
 EmployeeCode = e.EmployeeCode,
 FirstName = e.FirstName,
 MiddleName = e.MiddleName,
 LastName = e.LastName,
 MobileNumber = e.MobileNumber,
 AlternateMobileNumber = e.AlternateMobileNumber,
 PersonalEmail = e.PersonalEmail,
 OfficialEmail = e.OfficialEmail,
 DateOfBirth = e.DateOfBirth,
 Gender = e.Gender,
 BloodGroup = e.BloodGroup,
 DateOfJoining = e.DateOfJoining,
 DateOfConfirmation = e.DateOfConfirmation,
 EmploymentType = e.EmploymentType,
 EmployeeStatus = e.EmployeeStatus,
 ProbationEndDate = e.ProbationEndDate,
 DateOfResignation = e.DateOfResignation,
 LastWorkingDate = e.LastWorkingDate,
 ExitReason = e.ExitReason,
 PAN = sensitive ? e.PAN : null,
 AadhaarNumber = sensitive ? e.AadhaarNumber : null,
 UAN = sensitive ? e.UAN : null,
 ESICNumber = sensitive ? e.ESICNumber : null,
 BankName = sensitive ? e.BankName : null,
 AccountHolderName = sensitive ? e.AccountHolderName : null,
 BankAccountNumber = sensitive ? e.BankAccountNumber : null,
 IFSCCode = sensitive ? e.IFSCCode : null,
 ProfilePhotoUrl = e.ProfilePhotoUrl,
 Notes = e.Notes,
 IsActive = e.IsActive,
 Id=e.Id, FullName=e.FullName, Company=e.Company.CompanyName, Branch=e.Branch.BranchName, Department=e.Department.DepartmentName, Designation=e.Designation.DesignationName, Shift=e.Shift.ShiftName, WorkLocation=e.WorkLocation.LocationName, ReportingManagerName=e.ReportingManager?.FullName, CanViewSensitive=sensitive, CreatedAt=e.CreatedAt,CreatedBy=e.CreatedBy,UpdatedAt=e.UpdatedAt,UpdatedBy=e.UpdatedBy
 };
}
