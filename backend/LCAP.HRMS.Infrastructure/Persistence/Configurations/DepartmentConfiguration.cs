using LCAP.HRMS.Domain.Departments;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Departments_DepartmentCode", "LTRIM(RTRIM([DepartmentCode])) <> ''");
            table.HasCheckConstraint("CK_Departments_DepartmentName", "LTRIM(RTRIM([DepartmentName])) <> ''");
            table.HasCheckConstraint("CK_Departments_NotOwnParent", "[ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> [Id]");
        });
        builder.Property(department => department.DepartmentCode).HasMaxLength(50).IsRequired()
            .UseCollation("Latin1_General_100_CI_AS");
        builder.Property(department => department.DepartmentName).HasMaxLength(200).IsRequired();
        builder.Property(department => department.Description).HasMaxLength(1000);
        builder.HasOne(department => department.Company).WithMany(company => company.Departments)
            .HasForeignKey(department => department.CompanyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        // Composite FK enforces same-company ancestry even for writes outside the service.
        builder.HasAlternateKey(department => new { department.CompanyId, department.Id });
        builder.HasOne(department => department.ParentDepartment).WithMany(department => department.Children)
            .HasForeignKey(department => new { department.CompanyId, department.ParentDepartmentId })
            .HasPrincipalKey(department => new { department.CompanyId, department.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(department => new { department.CompanyId, department.DepartmentCode })
            .IsUnique().HasDatabaseName("UX_Departments_CompanyId_DepartmentCode");
        builder.HasIndex(department => department.ParentDepartmentId);
        builder.HasData(DepartmentSeedData.ForLcap());
    }
}
