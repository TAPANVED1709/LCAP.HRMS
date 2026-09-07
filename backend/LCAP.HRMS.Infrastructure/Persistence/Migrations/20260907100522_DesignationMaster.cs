using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DesignationMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Designations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesignationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    DesignationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true),
                    IsManagerial = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designations", x => x.Id);
                    table.CheckConstraint("CK_Designations_DesignationCode", "LTRIM(RTRIM([DesignationCode])) <> ''");
                    table.CheckConstraint("CK_Designations_DesignationName", "LTRIM(RTRIM([DesignationName])) <> ''");
                    table.ForeignKey(
                        name: "FK_Designations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Designations",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedBy", "Description", "DesignationCode", "DesignationName", "Grade", "IsActive", "Level", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("42d49a28-e6b8-46c7-aac1-11019a39d001"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, "EXEC", "Executive", null, true, null, null, null },
                    { new Guid("42d49a28-e6b8-46c7-aac1-11019a39d002"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, "SREXEC", "Senior Executive", null, true, null, null, null },
                    { new Guid("42d49a28-e6b8-46c7-aac1-11019a39d003"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, "TL", "Team Leader", null, true, null, null, null },
                    { new Guid("42d49a28-e6b8-46c7-aac1-11019a39d004"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, "MGR", "Manager", null, true, null, null, null },
                    { new Guid("42d49a28-e6b8-46c7-aac1-11019a39d005"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, "HRM", "HR Manager", null, true, null, null, null },
                    { new Guid("42d49a28-e6b8-46c7-aac1-11019a39d006"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, "PAYADMIN", "Payroll Admin", null, true, null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "UX_Designations_CompanyId_DesignationCode",
                schema: "dbo",
                table: "Designations",
                columns: new[] { "CompanyId", "DesignationCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Designations",
                schema: "dbo");
        }
    }
}
