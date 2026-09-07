using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Companies_PayrollDay", "[PayrollDay] BETWEEN 1 AND 31");
            table.HasCheckConstraint("CK_Companies_SalaryPaymentDay", "[SalaryPaymentDay] BETWEEN 1 AND 31");
            table.HasCheckConstraint("CK_Companies_CompanyCode", "LTRIM(RTRIM([CompanyCode])) <> ''");
            table.HasCheckConstraint("CK_Companies_CompanyName", "LTRIM(RTRIM([CompanyName])) <> ''");
        });
        builder.Property(company => company.CompanyCode).HasMaxLength(50).IsRequired();
        builder.Property(company => company.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(company => company.LegalName).HasMaxLength(250);
        builder.Property(company => company.RegisteredAddress).HasMaxLength(1000);
        builder.Property(company => company.City).HasMaxLength(100);
        builder.Property(company => company.State).HasMaxLength(100);
        builder.Property(company => company.Country).HasMaxLength(100).IsRequired();
        builder.Property(company => company.PinCode).HasMaxLength(20);
        builder.Property(company => company.PAN).HasMaxLength(50);
        builder.Property(company => company.TAN).HasMaxLength(50);
        builder.Property(company => company.GSTIN).HasMaxLength(50);
        builder.Property(company => company.PFRegistrationNumber).HasMaxLength(50);
        builder.Property(company => company.ESIRegistrationNumber).HasMaxLength(50);
        builder.Property(company => company.PayrollCurrency).HasMaxLength(3).IsRequired();
        builder.Property(company => company.LogoUrl).HasMaxLength(2048);
        builder.Property(company => company.CompanyCode).UseCollation("Latin1_General_100_CI_AS");
        builder.HasIndex(company => company.CompanyCode).IsUnique().HasDatabaseName("UX_Companies_CompanyCode");
        builder.Property(company => company.Country).HasDefaultValue("India");
        builder.Property(company => company.PayrollCurrency).HasDefaultValue("INR");
        builder.HasData(CompanySeedData.Lcap());
    }
}
