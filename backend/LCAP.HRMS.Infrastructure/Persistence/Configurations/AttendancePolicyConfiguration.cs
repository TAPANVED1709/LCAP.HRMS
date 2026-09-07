using LCAP.HRMS.Domain.AttendancePolicies;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LCAP.HRMS.Infrastructure.Persistence.Configurations;

public sealed class AttendancePolicyConfiguration : IEntityTypeConfiguration<AttendancePolicy>
{
    public void Configure(EntityTypeBuilder<AttendancePolicy> b)
    {
        b.ToTable("AttendancePolicies", t =>
        {
            t.HasCheckConstraint("CK_AttendancePolicies_Grace", "[GracePeriodMinutes] BETWEEN 0 AND 1440");
            t.HasCheckConstraint("CK_AttendancePolicies_Threshold", "[ConsecutiveLateThreshold] BETWEEN 0 AND 10000 AND ([LateRuleEnabled]=0 OR [ConsecutiveLateThreshold]>0)");
            t.HasCheckConstraint("CK_AttendancePolicies_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]");
            t.HasCheckConstraint("CK_AttendancePolicies_Enums", "[PenaltyTriggerMode] BETWEEN 1 AND 3 AND [PenaltyType] BETWEEN 1 AND 5 AND [ResetMode] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_AttendancePolicies_Value", "([PenaltyValue] IS NULL OR [PenaltyValue]>0) AND ([PenaltyType] NOT IN (1,2) OR [PenaltyValue] IS NULL) AND ([PenaltyType] NOT IN (3,4) OR [PenaltyValue] IS NOT NULL)");
        });
        b.Property(p => p.PolicyCode).HasMaxLength(30).UseCollation("Latin1_General_100_CI_AS").IsRequired(); b.Property(p => p.PolicyName).HasMaxLength(200).IsRequired(); b.Property(p => p.Description).HasMaxLength(2000); b.Property(p => p.PenaltyValue).HasPrecision(12, 3);
        b.HasIndex(p => new { p.CompanyId, p.PolicyCode }).IsUnique(); b.HasIndex(p => new { p.CompanyId, p.IsDefault, p.EffectiveFrom, p.EffectiveTo }); b.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasData(new AttendancePolicy { Id = new Guid("ea060404-ec8a-4c04-9404-000000000001"), CompanyId = Seeds.CompanySeedData.LcapId, PolicyCode = "LCAP-STANDARD", PolicyName = "LCAP Standard Attendance Policy", Description = "Initial configurable Phase 1 policy; HalfDay event classification requires client approval before payroll use. No salary deduction is calculated.", GracePeriodMinutes = 15, LateRuleEnabled = true, ConsecutiveLateThreshold = 3, PenaltyTriggerMode = PenaltyTriggerMode.NextQualifyingLate, PenaltyType = PenaltyType.HalfDay, ResetMode = ResetMode.AfterPenalty, EffectiveFrom = new DateOnly(2026, 9, 8), IsDefault = true, IsActive = true, Revision = 1, CreatedAt = new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.Zero), CreatedBy = "system:seed" });
    }
}
public sealed class AttendanceEvaluationConfiguration : IEntityTypeConfiguration<AttendanceEvaluation>
{
    public void Configure(EntityTypeBuilder<AttendanceEvaluation> b)
    {
        b.ToTable("AttendanceEvaluations", t => t.HasCheckConstraint("CK_AttendanceEvaluations_Counts", "[LateMinutes]>=0 AND [ConsecutiveLateCount]>=0 AND [SequenceAfterEvaluation]>=0 AND [EvaluationVersion]>0"));
        b.Property(e => e.ShiftName).HasMaxLength(200); b.Property(e => e.TimeZoneId).HasMaxLength(100); b.Property(e => e.PolicyCode).HasMaxLength(30); b.Property(e => e.ReasonCode).HasMaxLength(100); b.Property(e => e.Notes).HasMaxLength(2000); b.Property(e => e.PenaltyValue).HasPrecision(12, 3);
        b.HasIndex(e => e.AttendanceRecordId).IsUnique(); b.HasIndex(e => new { e.EmployeeId, e.AttendanceDate }); b.HasIndex(e => new { e.CompanyId, e.AttendanceDate });
        b.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId); b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId); b.HasOne(e => e.AttendanceRecord).WithMany().HasForeignKey(e => e.AttendanceRecordId); b.HasOne<AttendancePolicy>().WithMany().HasForeignKey(e => e.AttendancePolicyId);
    }
}
public sealed class AttendancePenaltyEventConfiguration : IEntityTypeConfiguration<AttendancePenaltyEvent>
{
    public void Configure(EntityTypeBuilder<AttendancePenaltyEvent> b)
    {
        b.ToTable("AttendancePenaltyEvents", t => t.HasCheckConstraint("CK_AttendancePenaltyEvents_Status", "[Status] BETWEEN 1 AND 3 AND [PenaltyType] BETWEEN 1 AND 5"));
        b.Property(e => e.ReasonCode).HasMaxLength(100).IsRequired(); b.Property(e => e.PenaltyValue).HasPrecision(12, 3); b.Property(e => e.CancelledBy).HasMaxLength(200); b.Property(e => e.CancellationReason).HasMaxLength(2000);
        b.HasIndex(e => e.AttendanceEvaluationId).IsUnique(); b.HasIndex(e => e.AttendanceRecordId).IsUnique(); b.HasIndex(e => new { e.EmployeeId, e.PenaltyDate }); b.HasIndex(e => new { e.CompanyId, e.Status });
        b.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId); b.HasOne<Employee>().WithMany().HasForeignKey(e => e.EmployeeId); b.HasOne<AttendanceRecord>().WithMany().HasForeignKey(e => e.AttendanceRecordId); b.HasOne<AttendancePolicy>().WithMany().HasForeignKey(e => e.AttendancePolicyId); b.HasOne(e => e.AttendanceEvaluation).WithMany().HasForeignKey(e => e.AttendanceEvaluationId);
    }
}

