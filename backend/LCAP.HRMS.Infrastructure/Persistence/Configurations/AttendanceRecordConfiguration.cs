using LCAP.HRMS.Domain.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.ToTable("AttendanceRecords", "dbo", t =>
        {
            t.HasCheckConstraint("CK_AttendanceRecords_Times", "[CheckInTime] IS NOT NULL AND ([CheckOutTime] IS NULL OR [CheckOutTime] >= [CheckInTime])");
            t.HasCheckConstraint("CK_AttendanceRecords_Status", "([Status] = 1 AND [CheckOutTime] IS NULL) OR ([Status] = 2 AND [CheckOutTime] IS NOT NULL)");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckInLatitude", "[CheckInLatitude] IS NULL OR [CheckInLatitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckInLongitude", "[CheckInLongitude] IS NULL OR [CheckInLongitude] BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckInAccuracy", "[CheckInAccuracyMeters] IS NULL OR [CheckInAccuracyMeters] > 0");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckInDistance", "[CheckInDistanceMeters] IS NULL OR [CheckInDistanceMeters] >= 0");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckOutLatitude", "[CheckOutLatitude] IS NULL OR [CheckOutLatitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckOutLongitude", "[CheckOutLongitude] IS NULL OR [CheckOutLongitude] BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckOutAccuracy", "[CheckOutAccuracyMeters] IS NULL OR [CheckOutAccuracyMeters] > 0");
            t.HasCheckConstraint("CK_AttendanceRecords_CheckOutDistance", "[CheckOutDistanceMeters] IS NULL OR [CheckOutDistanceMeters] >= 0");
        });
        b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(x => x.CheckInSource).HasMaxLength(30).IsRequired();
        b.Property(x => x.CheckOutSource).HasMaxLength(30);
        b.Property(x => x.DeviceIdentifier).HasMaxLength(200);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.CheckInLatitude).HasPrecision(10, 7);
        b.Property(x => x.CheckInLongitude).HasPrecision(10, 7);
        b.Property(x => x.CheckInAccuracyMeters).HasPrecision(12, 3);
        b.Property(x => x.CheckInDistanceMeters).HasPrecision(14, 3);
        b.Property(x => x.CheckOutLatitude).HasPrecision(10, 7);
        b.Property(x => x.CheckOutLongitude).HasPrecision(10, 7);
        b.Property(x => x.CheckOutAccuracyMeters).HasPrecision(12, 3);
        b.Property(x => x.CheckOutDistanceMeters).HasPrecision(14, 3);
        // Deleted attendance still reserves its date; there is no attendance deletion API.
        b.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique().HasDatabaseName("UX_AttendanceRecords_EmployeeId_AttendanceDate");
        b.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("[IsDeleted] = 0 AND [CheckOutTime] IS NULL").HasDatabaseName("UX_AttendanceRecords_EmployeeId_Open");
        b.HasIndex(x => new { x.CompanyId, x.AttendanceDate });
        b.HasIndex(x => new { x.WorkLocationId, x.AttendanceDate });
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.WorkLocation).WithMany().HasForeignKey(x => x.WorkLocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}
