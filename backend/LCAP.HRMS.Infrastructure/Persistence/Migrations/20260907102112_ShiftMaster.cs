using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShiftMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shifts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    ShiftName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(7)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(7)", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    MinimumHalfDayMinutes = table.Column<int>(type: "int", nullable: true),
                    MinimumFullDayMinutes = table.Column<int>(type: "int", nullable: true),
                    IsNightShift = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.CheckConstraint("CK_Shifts_FullDay", "[MinimumFullDayMinutes] IS NULL OR [MinimumFullDayMinutes] >= 0");
                    table.CheckConstraint("CK_Shifts_GracePeriod", "[GracePeriodMinutes] >= 0");
                    table.CheckConstraint("CK_Shifts_HalfDay", "[MinimumHalfDayMinutes] IS NULL OR [MinimumHalfDayMinutes] >= 0");
                    table.CheckConstraint("CK_Shifts_NightShift", "[EndTime] > [StartTime] OR [IsNightShift] = 1");
                    table.CheckConstraint("CK_Shifts_ShiftCode", "LTRIM(RTRIM([ShiftCode])) <> ''");
                    table.CheckConstraint("CK_Shifts_ShiftName", "LTRIM(RTRIM([ShiftName])) <> ''");
                    table.CheckConstraint("CK_Shifts_ThresholdOrder", "[MinimumHalfDayMinutes] IS NULL OR [MinimumFullDayMinutes] IS NULL OR [MinimumHalfDayMinutes] <= [MinimumFullDayMinutes]");
                    table.CheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]");
                    table.ForeignKey(
                        name: "FK_Shifts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Shifts",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedBy", "EndTime", "GracePeriodMinutes", "IsActive", "IsNightShift", "MinimumFullDayMinutes", "MinimumHalfDayMinutes", "ShiftCode", "ShiftName", "StartTime", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("a0c85f4b-a175-441a-973e-9f66d6ed0001"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", new TimeOnly(18, 30, 0), 15, true, false, null, null, "GENERAL", "General Shift", new TimeOnly(9, 30, 0), null, null });

            migrationBuilder.CreateIndex(
                name: "UX_Shifts_CompanyId_ShiftCode",
                schema: "dbo",
                table: "Shifts",
                columns: new[] { "CompanyId", "ShiftCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "dbo");
        }
    }
}
