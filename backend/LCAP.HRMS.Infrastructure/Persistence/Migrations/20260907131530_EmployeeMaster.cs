using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportingManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    AlternateMobileNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PersonalEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    OfficialEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BloodGroup = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DateOfJoining = table.Column<DateOnly>(type: "date", nullable: false),
                    DateOfConfirmation = table.Column<DateOnly>(type: "date", nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    EmployeeStatus = table.Column<int>(type: "int", nullable: false),
                    ProbationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DateOfResignation = table.Column<DateOnly>(type: "date", nullable: true),
                    LastWorkingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExitReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PAN = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AadhaarNumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    UAN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ESICNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    IFSCCode = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    ProfilePhotoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("CK_Employees_Code", "LTRIM(RTRIM([EmployeeCode])) <> ''");
                    table.CheckConstraint("CK_Employees_Confirmation", "[DateOfConfirmation] IS NULL OR [DateOfConfirmation] >= [DateOfJoining]");
                    table.CheckConstraint("CK_Employees_LastWorking", "[LastWorkingDate] IS NULL OR [LastWorkingDate] >= [DateOfJoining]");
                    table.CheckConstraint("CK_Employees_SelfManager", "[ReportingManagerId] IS NULL OR [ReportingManagerId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_Employees_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "dbo",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "dbo",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "dbo",
                        principalTable: "Designations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Employees_ReportingManagerId",
                        column: x => x.ReportingManagerId,
                        principalSchema: "dbo",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "dbo",
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalSchema: "dbo",
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId",
                schema: "dbo",
                table: "Employees",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_IsDeleted",
                schema: "dbo",
                table: "Employees",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                schema: "dbo",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DesignationId",
                schema: "dbo",
                table: "Employees",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeStatus",
                schema: "dbo",
                table: "Employees",
                column: "EmployeeStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ReportingManagerId",
                schema: "dbo",
                table: "Employees",
                column: "ReportingManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ShiftId",
                schema: "dbo",
                table: "Employees",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WorkLocationId",
                schema: "dbo",
                table: "Employees",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "UX_Employees_CompanyId_EmployeeCode",
                schema: "dbo",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees",
                schema: "dbo");
        }
    }
}
