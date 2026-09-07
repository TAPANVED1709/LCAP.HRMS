using LCAP.HRMS.Domain.WorkLocations;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        builder.ToTable("WorkLocations", "dbo", table =>
        {
            table.HasCheckConstraint("CK_WorkLocations_Latitude", "[Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90");
            table.HasCheckConstraint("CK_WorkLocations_Longitude", "[Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180");
            table.HasCheckConstraint("CK_WorkLocations_CoordinatePair", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)");
            table.HasCheckConstraint("CK_WorkLocations_Radius", "[AllowedRadiusMeters] > 0");
            table.HasCheckConstraint("CK_WorkLocations_LocationCode", "LTRIM(RTRIM([LocationCode])) <> ''");
            table.HasCheckConstraint("CK_WorkLocations_LocationName", "LTRIM(RTRIM([LocationName])) <> ''");
        });
        builder.Property(location => location.LocationCode).HasMaxLength(50).IsRequired();
        builder.Property(location => location.LocationName).HasMaxLength(200).IsRequired();
        builder.Property(location => location.Address).HasMaxLength(1000);
        builder.Property(location => location.City).HasMaxLength(100);
        builder.Property(location => location.State).HasMaxLength(100);
        builder.Property(location => location.Country).HasMaxLength(100).IsRequired();
        builder.Property(location => location.PinCode).HasMaxLength(20);
        builder.Property(location => location.LocationCode).UseCollation("Latin1_General_100_CI_AS");
        builder.Property(location => location.Latitude).HasColumnType("decimal(10,7)").HasPrecision(10, 7);
        builder.Property(location => location.Longitude).HasColumnType("decimal(10,7)").HasPrecision(10, 7);
        builder.Property(location => location.AllowedRadiusMeters).HasDefaultValue(100).ValueGeneratedNever();
        builder.Property(location => location.IsGeoFenceEnabled).HasDefaultValue(true).HasSentinel(true);
        builder.Property(location => location.Country).HasDefaultValue("India");
        builder.HasOne(location => location.Company).WithMany(company => company.WorkLocations)
            .HasForeignKey(location => location.CompanyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(location => location.Branch).WithMany(branch => branch.WorkLocations)
            .HasForeignKey(location => location.BranchId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(location => new { location.BranchId, location.LocationCode })
            .IsUnique().HasDatabaseName("UX_WorkLocations_BranchId_LocationCode");
        builder.HasData(WorkLocationSeedData.PatnaOffice());
    }
}
