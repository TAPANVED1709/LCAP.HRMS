using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RegisteredAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "India"),
                    PinCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PAN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GSTIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PFRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ESIRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PayrollCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "INR"),
                    PayrollDay = table.Column<int>(type: "int", nullable: false),
                    SalaryPaymentDay = table.Column<int>(type: "int", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.CheckConstraint("CK_Companies_CompanyCode", "LTRIM(RTRIM([CompanyCode])) <> ''");
                    table.CheckConstraint("CK_Companies_CompanyName", "LTRIM(RTRIM([CompanyName])) <> ''");
                    table.CheckConstraint("CK_Companies_PayrollDay", "[PayrollDay] BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_Companies_SalaryPaymentDay", "[SalaryPaymentDay] BETWEEN 1 AND 31");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Companies",
                columns: new[] { "Id", "City", "CompanyCode", "CompanyName", "Country", "CreatedAt", "CreatedBy", "ESIRegistrationNumber", "GSTIN", "IsActive", "LegalName", "LogoUrl", "PAN", "PFRegistrationNumber", "PayrollCurrency", "PayrollDay", "PinCode", "RegisteredAddress", "SalaryPaymentDay", "State", "TAN", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), null, "LCAP", "LCAP", "India", new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", null, null, true, null, null, null, null, "INR", 1, null, null, 1, "Bihar", null, null, null });

            migrationBuilder.CreateIndex(
                name: "UX_Companies_CompanyCode",
                schema: "dbo",
                table: "Companies",
                column: "CompanyCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Companies",
                schema: "dbo");
        }
    }
}
