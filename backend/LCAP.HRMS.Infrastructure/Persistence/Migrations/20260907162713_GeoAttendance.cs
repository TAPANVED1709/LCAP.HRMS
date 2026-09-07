using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeoAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CheckInTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CheckOutTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CheckInLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckInLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckInAccuracyMeters = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    CheckOutLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckOutLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckOutAccuracyMeters = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    CheckInDistanceMeters = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    CheckOutDistanceMeters = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    CheckInWithinGeofence = table.Column<bool>(type: "bit", nullable: true),
                    CheckOutWithinGeofence = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckInSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CheckOutSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.CheckConstraint("CK_AttendanceRecords_CheckInAccuracy", "[CheckInAccuracyMeters] IS NULL OR [CheckInAccuracyMeters] > 0");
                    table.CheckConstraint("CK_AttendanceRecords_CheckInDistance", "[CheckInDistanceMeters] IS NULL OR [CheckInDistanceMeters] >= 0");
                    table.CheckConstraint("CK_AttendanceRecords_CheckInLatitude", "[CheckInLatitude] IS NULL OR [CheckInLatitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_AttendanceRecords_CheckInLongitude", "[CheckInLongitude] IS NULL OR [CheckInLongitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_AttendanceRecords_CheckOutAccuracy", "[CheckOutAccuracyMeters] IS NULL OR [CheckOutAccuracyMeters] > 0");
                    table.CheckConstraint("CK_AttendanceRecords_CheckOutDistance", "[CheckOutDistanceMeters] IS NULL OR [CheckOutDistanceMeters] >= 0");
                    table.CheckConstraint("CK_AttendanceRecords_CheckOutLatitude", "[CheckOutLatitude] IS NULL OR [CheckOutLatitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_AttendanceRecords_CheckOutLongitude", "[CheckOutLongitude] IS NULL OR [CheckOutLongitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_AttendanceRecords_Status", "([Status] = 1 AND [CheckOutTime] IS NULL) OR ([Status] = 2 AND [CheckOutTime] IS NOT NULL)");
                    table.CheckConstraint("CK_AttendanceRecords_Times", "[CheckInTime] IS NOT NULL AND ([CheckOutTime] IS NULL OR [CheckOutTime] >= [CheckInTime])");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "dbo",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "dbo",
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalSchema: "dbo",
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CompanyId_AttendanceDate",
                schema: "dbo",
                table: "AttendanceRecords",
                columns: new[] { "CompanyId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ShiftId",
                schema: "dbo",
                table: "AttendanceRecords",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_WorkLocationId_AttendanceDate",
                schema: "dbo",
                table: "AttendanceRecords",
                columns: new[] { "WorkLocationId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "UX_AttendanceRecords_EmployeeId_AttendanceDate",
                schema: "dbo",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AttendanceRecords_EmployeeId_Open",
                schema: "dbo",
                table: "AttendanceRecords",
                column: "EmployeeId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [CheckOutTime] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords",
                schema: "dbo");
        }
    }
}
