using LCAP.HRMS.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
 public void Configure(EntityTypeBuilder<Employee> b)
 {
 b.ToTable("Employees", "dbo", t => {
 t.HasCheckConstraint("CK_Employees_SelfManager", "[ReportingManagerId] IS NULL OR [ReportingManagerId] <> [Id]");
 t.HasCheckConstraint("CK_Employees_Code", "LTRIM(RTRIM([EmployeeCode])) <> ''");
 t.HasCheckConstraint("CK_Employees_Confirmation", "[DateOfConfirmation] IS NULL OR [DateOfConfirmation] >= [DateOfJoining]");
 t.HasCheckConstraint("CK_Employees_LastWorking", "[LastWorkingDate] IS NULL OR [LastWorkingDate] >= [DateOfJoining]");
 });
 b.Ignore(x=>x.FullName);
 b.Property(x=>x.EmployeeCode).HasMaxLength(50).IsRequired();
 b.Property(x=>x.FirstName).HasMaxLength(100).IsRequired();
 b.Property(x=>x.MiddleName).HasMaxLength(100);
 b.Property(x=>x.LastName).HasMaxLength(100);
 b.Property(x=>x.MobileNumber).HasMaxLength(25).IsRequired();
 b.Property(x=>x.AlternateMobileNumber).HasMaxLength(25);
 b.Property(x=>x.PersonalEmail).HasMaxLength(254);
 b.Property(x=>x.OfficialEmail).HasMaxLength(254);
 b.Property(x=>x.Gender).HasMaxLength(50);
 b.Property(x=>x.BloodGroup).HasMaxLength(10);
 b.Property(x=>x.ExitReason).HasMaxLength(1000);
 b.Property(x=>x.PAN).HasMaxLength(10);
 b.Property(x=>x.AadhaarNumber).HasMaxLength(12);
 b.Property(x=>x.UAN).HasMaxLength(20);
 b.Property(x=>x.ESICNumber).HasMaxLength(20);
 b.Property(x=>x.BankName).HasMaxLength(200);
 b.Property(x=>x.AccountHolderName).HasMaxLength(200);
 b.Property(x=>x.BankAccountNumber).HasMaxLength(34);
 b.Property(x=>x.IFSCCode).HasMaxLength(11);
 b.Property(x=>x.ProfilePhotoUrl).HasMaxLength(2048);
 b.Property(x=>x.Notes).HasMaxLength(2000);
 b.Property(x=>x.EmployeeCode).UseCollation("Latin1_General_100_CI_AS");
 b.HasIndex(x=>new{x.CompanyId,x.EmployeeCode}).IsUnique().HasDatabaseName("UX_Employees_CompanyId_EmployeeCode");
 b.HasIndex(x=>x.EmployeeStatus);
 b.HasIndex(x=>new{x.CompanyId,x.IsDeleted});
 b.HasOne(x=>x.Company).WithMany().HasForeignKey(x=>x.CompanyId).OnDelete(DeleteBehavior.Restrict);
 b.HasOne(x=>x.Branch).WithMany().HasForeignKey(x=>x.BranchId).OnDelete(DeleteBehavior.Restrict);
 b.HasOne(x=>x.Department).WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
 b.HasOne(x=>x.Designation).WithMany().HasForeignKey(x=>x.DesignationId).OnDelete(DeleteBehavior.Restrict);
 b.HasOne(x=>x.Shift).WithMany().HasForeignKey(x=>x.ShiftId).OnDelete(DeleteBehavior.Restrict);
 b.HasOne(x=>x.WorkLocation).WithMany().HasForeignKey(x=>x.WorkLocationId).OnDelete(DeleteBehavior.Restrict);
 b.HasOne(x=>x.ReportingManager).WithMany().HasForeignKey(x=>x.ReportingManagerId).OnDelete(DeleteBehavior.Restrict);
 }
}
