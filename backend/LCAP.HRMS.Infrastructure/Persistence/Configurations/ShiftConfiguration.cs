using LCAP.HRMS.Domain.Shifts;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Shifts_ShiftCode", "LTRIM(RTRIM([ShiftCode])) <> ''");
            table.HasCheckConstraint("CK_Shifts_ShiftName", "LTRIM(RTRIM([ShiftName])) <> ''");
            table.HasCheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]");
            table.HasCheckConstraint("CK_Shifts_GracePeriod", "[GracePeriodMinutes] >= 0");
            table.HasCheckConstraint("CK_Shifts_HalfDay", "[MinimumHalfDayMinutes] IS NULL OR [MinimumHalfDayMinutes] >= 0");
            table.HasCheckConstraint("CK_Shifts_FullDay", "[MinimumFullDayMinutes] IS NULL OR [MinimumFullDayMinutes] >= 0");
            table.HasCheckConstraint("CK_Shifts_ThresholdOrder", "[MinimumHalfDayMinutes] IS NULL OR [MinimumFullDayMinutes] IS NULL OR [MinimumHalfDayMinutes] <= [MinimumFullDayMinutes]");
            table.HasCheckConstraint("CK_Shifts_NightShift", "[EndTime] > [StartTime] OR [IsNightShift] = 1");
        });
        builder.Property(x => x.ShiftCode).HasMaxLength(50).IsRequired().UseCollation("Latin1_General_100_CI_AS");
        builder.Property(x => x.ShiftName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time(7)").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time(7)").IsRequired();
        builder.HasOne(x => x.Company).WithMany(x => x.Shifts)
            .HasForeignKey(x => x.CompanyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanyId, x.ShiftCode }).IsUnique()
            .HasDatabaseName("UX_Shifts_CompanyId_ShiftCode");
        builder.HasData(ShiftSeedData.General());
    }
}
