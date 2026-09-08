using LCAP.HRMS.Domain.Regularisation;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Domain.Shifts;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;
public sealed class RegularisationRequestConfiguration : IEntityTypeConfiguration<AttendanceRegularisationRequest>
{
    public void Configure(EntityTypeBuilder<AttendanceRegularisationRequest> b)
    {
        b.ToTable("AttendanceRegularisationRequests", t =>
        {
            t.HasCheckConstraint("CK_Regularisation_Status", "[Status] BETWEEN 1 AND 7");
            t.HasCheckConstraint("CK_Regularisation_Type", "[RequestType] BETWEEN 1 AND 7");
        });
        b.Property(r => r.Status).IsConcurrencyToken();
        b.Property(r => r.EmployeeReason).HasMaxLength(2000).IsRequired();
        b.Property(r => r.SupportingNote).HasMaxLength(2000);
        b.Property(r => r.ReviewerRemarks).HasMaxLength(2000);
        b.Property(r => r.CancellationReason).HasMaxLength(2000);
        b.Property(r => r.AttachmentUrl).HasMaxLength(2048);
        b.Property(r => r.TimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(r => r.ShiftName).HasMaxLength(200).IsRequired();
        b.Property(r => r.AppliedBy).HasMaxLength(200);
        b.HasIndex(r => new { r.CompanyId, r.Status });
        b.HasIndex(r => new { r.EmployeeId, r.AttendanceDate });
        b.HasIndex(r => new { r.ReportingManagerId, r.Status });
        b.HasIndex(r => new { r.EmployeeId, r.AttendanceDate, r.RequestType }).IsUnique().HasFilter("[Status] IN (2, 3, 4)");
        b.HasOne<Company>().WithMany().HasForeignKey(r => r.CompanyId);
        b.HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId);
        b.HasOne(r => r.ReportingManager).WithMany().HasForeignKey(r => r.ReportingManagerId);
        b.HasOne<Employee>().WithMany().HasForeignKey(r => r.ReviewedByEmployeeId);
        b.HasOne<AttendanceRecord>().WithMany().HasForeignKey(r => r.AttendanceRecordId);
        b.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId);
        b.HasOne<Shift>().WithMany().HasForeignKey(r => r.ShiftId);
        b.HasOne<AttendanceCorrection>().WithMany().HasForeignKey(r => r.BaseCorrectionId);
    }
}

public sealed class AttendanceCorrectionConfiguration : IEntityTypeConfiguration<AttendanceCorrection>
{
    public void Configure(EntityTypeBuilder<AttendanceCorrection> b)
    {
        b.ToTable("AttendanceCorrections", t =>
        {
            t.HasCheckConstraint("CK_Correction_Version", "[Version] > 0");
            t.HasCheckConstraint("CK_Correction_Times", "[CorrectedCheckOutTime] IS NULL OR ([CorrectedCheckInTime] IS NOT NULL AND [CorrectedCheckOutTime] >= [CorrectedCheckInTime])");
        });
        b.Property(c => c.Reason).HasMaxLength(2000).IsRequired();
        b.Property(c => c.AppliedBy).HasMaxLength(200).IsRequired();
        b.HasIndex(c => c.RegularisationRequestId).IsUnique();
        b.HasIndex(c => new { c.EmployeeId, c.AttendanceDate, c.Version }).IsUnique();
        b.HasOne(c => c.Request).WithMany().HasForeignKey(c => c.RegularisationRequestId);
        b.HasOne<Company>().WithMany().HasForeignKey(c => c.CompanyId);
        b.HasOne<Employee>().WithMany().HasForeignKey(c => c.EmployeeId);
        b.HasOne<AttendanceRecord>().WithMany().HasForeignKey(c => c.AttendanceRecordId);
        b.HasOne<AttendanceRecord>().WithMany().HasForeignKey(c => c.OriginalAttendanceRecordId);
    }
}

public sealed class EvaluationRevisionConfiguration : IEntityTypeConfiguration<AttendanceEvaluationRevision>
{
    public void Configure(EntityTypeBuilder<AttendanceEvaluationRevision> b)
    {
        b.ToTable("AttendanceEvaluationRevisions", t => t.HasCheckConstraint("CK_EvaluationRevision_Version", "[Version] > 0"));
        b.Property(v => v.ResultJson).IsRequired();
        b.HasIndex(v => new { v.EmployeeId, v.AttendanceDate, v.Version }).IsUnique();
        b.HasIndex(v => new { v.CompanyId, v.AttendanceDate });
        b.HasOne<Company>().WithMany().HasForeignKey(v => v.CompanyId);
        b.HasOne<Employee>().WithMany().HasForeignKey(v => v.EmployeeId);
        b.HasOne<AttendanceRecord>().WithMany().HasForeignKey(v => v.AttendanceRecordId);
        b.HasOne<AttendanceCorrection>().WithMany().HasForeignKey(v => v.CorrectionId);
        b.HasOne<AttendanceEvaluation>().WithMany().HasForeignKey(v => v.OriginalEvaluationId);
        b.HasOne<AttendancePenaltyEvent>().WithMany().HasForeignKey(v => v.RelatedPenaltyEventId);
    }
}

public sealed class RegularisationAuditConfiguration : IEntityTypeConfiguration<RegularisationAuditEntry>
{
    public void Configure(EntityTypeBuilder<RegularisationAuditEntry> b)
    {
        b.ToTable("RegularisationAuditEntries");
        b.Property(a => a.Actor).HasMaxLength(200).IsRequired();
        b.Property(a => a.Action).HasMaxLength(100).IsRequired();
        b.Property(a => a.Reason).HasMaxLength(2000);
        b.HasIndex(a => a.RegularisationRequestId);
        b.HasOne<AttendanceRegularisationRequest>().WithMany().HasForeignKey(a => a.RegularisationRequestId);
        b.HasOne<Company>().WithMany().HasForeignKey(a => a.CompanyId);
        b.HasOne<Employee>().WithMany().HasForeignKey(a => a.EmployeeId);
    }
}
