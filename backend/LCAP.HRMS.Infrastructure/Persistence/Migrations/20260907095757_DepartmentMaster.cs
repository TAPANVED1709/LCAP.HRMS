using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    DepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.UniqueConstraint("AK_Departments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Departments_DepartmentCode", "LTRIM(RTRIM([DepartmentCode])) <> ''");
                    table.CheckConstraint("CK_Departments_DepartmentName", "LTRIM(RTRIM([DepartmentName])) <> ''");
                    table.CheckConstraint("CK_Departments_NotOwnParent", "[ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_Departments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_CompanyId_ParentDepartmentId",
                        columns: x => new { x.CompanyId, x.ParentDepartmentId },
                        principalSchema: "dbo",
                        principalTable: "Departments",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Departments",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentCode", "DepartmentName", "Description", "IsActive", "ParentDepartmentId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4201"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", "HR", "Human Resources", null, true, null, null, null },
                    { new Guid("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4202"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", "FIN", "Finance", null, true, null, null, null },
                    { new Guid("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4203"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", "SALES", "Sales", null, true, null, null, null },
                    { new Guid("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4204"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", "OPS", "Operations", null, true, null, null, null },
                    { new Guid("0ebde002-3e2e-4ac4-bcca-7a0ea3bd4205"), new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", "MGMT", "Management", null, true, null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId_ParentDepartmentId",
                schema: "dbo",
                table: "Departments",
                columns: new[] { "CompanyId", "ParentDepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                schema: "dbo",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "UX_Departments_CompanyId_DepartmentCode",
                schema: "dbo",
                table: "Departments",
                columns: new[] { "CompanyId", "DepartmentCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Departments",
                schema: "dbo");
        }
    }
}
