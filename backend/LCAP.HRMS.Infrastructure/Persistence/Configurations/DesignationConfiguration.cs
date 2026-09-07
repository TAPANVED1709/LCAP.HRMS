using LCAP.HRMS.Domain.Designations;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("Designations", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Designations_DesignationCode", "LTRIM(RTRIM([DesignationCode])) <> ''");
            table.HasCheckConstraint("CK_Designations_DesignationName", "LTRIM(RTRIM([DesignationName])) <> ''");
        });
        builder.Property(designation => designation.DesignationCode).HasMaxLength(50).IsRequired()
            .UseCollation("Latin1_General_100_CI_AS");
        builder.Property(designation => designation.DesignationName).HasMaxLength(200).IsRequired();
        builder.Property(designation => designation.Description).HasMaxLength(1000);
        builder.Property(designation => designation.Grade).HasMaxLength(50);
        builder.Property(designation => designation.IsManagerial).HasDefaultValue(false);
        builder.HasOne(designation => designation.Company).WithMany(company => company.Designations)
            .HasForeignKey(designation => designation.CompanyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(designation => new { designation.CompanyId, designation.DesignationCode })
            .IsUnique().HasDatabaseName("UX_Designations_CompanyId_DesignationCode");
        builder.HasData(DesignationSeedData.ForLcap());
    }
}
