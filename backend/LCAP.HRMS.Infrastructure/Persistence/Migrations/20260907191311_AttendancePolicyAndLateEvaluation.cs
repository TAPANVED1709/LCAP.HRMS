using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AttendancePolicyAndLateEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendancePolicies",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    PolicyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    LateRuleEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConsecutiveLateThreshold = table.Column<int>(type: "int", nullable: false),
                    PenaltyTriggerMode = table.Column<int>(type: "int", nullable: false),
                    PenaltyType = table.Column<int>(type: "int", nullable: false),
                    PenaltyValue = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    ResetMode = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicies", x => x.Id);
                    table.CheckConstraint("CK_AttendancePolicies_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]");
                    table.CheckConstraint("CK_AttendancePolicies_Enums", "[PenaltyTriggerMode] BETWEEN 1 AND 3 AND [PenaltyType] BETWEEN 1 AND 5 AND [ResetMode] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_AttendancePolicies_Grace", "[GracePeriodMinutes] BETWEEN 0 AND 1440");
                    table.CheckConstraint("CK_AttendancePolicies_Threshold", "[ConsecutiveLateThreshold] BETWEEN 0 AND 10000 AND ([LateRuleEnabled]=0 OR [ConsecutiveLateThreshold]>0)");
                    table.CheckConstraint("CK_AttendancePolicies_Value", "([PenaltyValue] IS NULL OR [PenaltyValue]>0) AND ([PenaltyType] NOT IN (1,2) OR [PenaltyValue] IS NULL) AND ([PenaltyType] NOT IN (3,4) OR [PenaltyValue] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AttendancePolicies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceEvaluations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendancePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftStartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualCheckInTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsEvaluated = table.Column<bool>(type: "bit", nullable: false),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveLateCount = table.Column<int>(type: "int", nullable: false),
                    SequenceAfterEvaluation = table.Column<int>(type: "int", nullable: false),
                    ThresholdReached = table.Column<bool>(type: "bit", nullable: false),
                    PenaltyTriggered = table.Column<bool>(type: "bit", nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EvaluationVersion = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PolicyCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PolicyRevision = table.Column<int>(type: "int", nullable: false),
                    LateRuleEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConsecutiveLateThreshold = table.Column<int>(type: "int", nullable: false),
                    PenaltyTriggerMode = table.Column<int>(type: "int", nullable: false),
                    PenaltyType = table.Column<int>(type: "int", nullable: false),
                    PenaltyValue = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    ResetMode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceEvaluations", x => x.Id);
                    table.CheckConstraint("CK_AttendanceEvaluations_Counts", "[LateMinutes]>=0 AND [ConsecutiveLateCount]>=0 AND [SequenceAfterEvaluation]>=0 AND [EvaluationVersion]>0");
                    table.ForeignKey(
                        name: "FK_AttendanceEvaluations_AttendancePolicies_AttendancePolicyId",
                        column: x => x.AttendancePolicyId,
                        principalSchema: "dbo",
                        principalTable: "AttendancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceEvaluations_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalSchema: "dbo",
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceEvaluations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceEvaluations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "dbo",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePenaltyEvents",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendancePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PenaltyDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PenaltyType = table.Column<int>(type: "int", nullable: false),
                    PenaltyValue = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PayrollConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePenaltyEvents", x => x.Id);
                    table.CheckConstraint("CK_AttendancePenaltyEvents_Status", "[Status] BETWEEN 1 AND 3 AND [PenaltyType] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_AttendancePenaltyEvents_AttendanceEvaluations_AttendanceEvaluationId",
                        column: x => x.AttendanceEvaluationId,
                        principalSchema: "dbo",
                        principalTable: "AttendanceEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendancePenaltyEvents_AttendancePolicies_AttendancePolicyId",
                        column: x => x.AttendancePolicyId,
                        principalSchema: "dbo",
                        principalTable: "AttendancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendancePenaltyEvents_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalSchema: "dbo",
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendancePenaltyEvents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendancePenaltyEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "dbo",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "AttendancePolicies",
                columns: new[] { "Id", "CompanyId", "ConsecutiveLateThreshold", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "GracePeriodMinutes", "IsActive", "IsDefault", "LateRuleEnabled", "PenaltyTriggerMode", "PenaltyType", "PenaltyValue", "PolicyCode", "PolicyName", "ResetMode", "Revision", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("ea060404-ec8a-4c04-9404-000000000001"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), 3, new DateTimeOffset(new DateTime(2026, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", "Initial configurable Phase 1 policy; HalfDay event classification requires client approval before payroll use. No salary deduction is calculated.", new DateOnly(2026, 9, 8), null, 15, true, true, true, 1, 1, null, "LCAP-STANDARD", "LCAP Standard Attendance Policy", 1, 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvaluations_AttendancePolicyId",
                schema: "dbo",
                table: "AttendanceEvaluations",
                column: "AttendancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvaluations_AttendanceRecordId",
                schema: "dbo",
                table: "AttendanceEvaluations",
                column: "AttendanceRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvaluations_CompanyId_AttendanceDate",
                schema: "dbo",
                table: "AttendanceEvaluations",
                columns: new[] { "CompanyId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvaluations_EmployeeId_AttendanceDate",
                schema: "dbo",
                table: "AttendanceEvaluations",
                columns: new[] { "EmployeeId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePenaltyEvents_AttendanceEvaluationId",
                schema: "dbo",
                table: "AttendancePenaltyEvents",
                column: "AttendanceEvaluationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePenaltyEvents_AttendancePolicyId",
                schema: "dbo",
                table: "AttendancePenaltyEvents",
                column: "AttendancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePenaltyEvents_AttendanceRecordId",
                schema: "dbo",
                table: "AttendancePenaltyEvents",
                column: "AttendanceRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePenaltyEvents_CompanyId_Status",
                schema: "dbo",
                table: "AttendancePenaltyEvents",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePenaltyEvents_EmployeeId_PenaltyDate",
                schema: "dbo",
                table: "AttendancePenaltyEvents",
                columns: new[] { "EmployeeId", "PenaltyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_CompanyId_IsDefault_EffectiveFrom_EffectiveTo",
                schema: "dbo",
                table: "AttendancePolicies",
                columns: new[] { "CompanyId", "IsDefault", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_CompanyId_PolicyCode",
                schema: "dbo",
                table: "AttendancePolicies",
                columns: new[] { "CompanyId", "PolicyCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendancePenaltyEvents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AttendanceEvaluations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AttendancePolicies",
                schema: "dbo");
        }
    }
}
