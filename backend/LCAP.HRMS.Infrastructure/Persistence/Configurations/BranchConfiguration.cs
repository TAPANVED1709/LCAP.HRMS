using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Branches_BranchCode", "LTRIM(RTRIM([BranchCode])) <> ''");
            table.HasCheckConstraint("CK_Branches_BranchName", "LTRIM(RTRIM([BranchName])) <> ''");
        });
        builder.Property(branch => branch.BranchCode).HasMaxLength(50).IsRequired();
        builder.Property(branch => branch.BranchName).HasMaxLength(200).IsRequired();
        builder.Property(branch => branch.AddressLine1).HasMaxLength(500);
        builder.Property(branch => branch.AddressLine2).HasMaxLength(500);
        builder.Property(branch => branch.City).HasMaxLength(100);
        builder.Property(branch => branch.State).HasMaxLength(100);
        builder.Property(branch => branch.Country).HasMaxLength(100).IsRequired();
        builder.Property(branch => branch.PinCode).HasMaxLength(20);
        builder.Property(branch => branch.Email).HasMaxLength(254);
        builder.Property(branch => branch.Phone).HasMaxLength(30);
        builder.Property(branch => branch.BranchCode).UseCollation("Latin1_General_100_CI_AS");
        builder.Property(branch => branch.Country).HasDefaultValue("India");
        builder.HasOne(branch => branch.Company).WithMany(company => company.Branches)
            .HasForeignKey(branch => branch.CompanyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        // The leading CompanyId also supports queries for branches within a company.
        builder.HasIndex(branch => new { branch.CompanyId, branch.BranchCode })
            .IsUnique().HasDatabaseName("UX_Branches_CompanyId_BranchCode");
        builder.HasData(BranchSeedData.PatnaHeadOffice());
    }
}
