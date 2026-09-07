using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LCAP.HRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkLocationMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkLocations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "India"),
                    PinCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    AllowedRadiusMeters = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    IsGeoFenceEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLocations", x => x.Id);
                    table.CheckConstraint("CK_WorkLocations_CoordinatePair", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)");
                    table.CheckConstraint("CK_WorkLocations_Latitude", "[Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_WorkLocations_LocationCode", "LTRIM(RTRIM([LocationCode])) <> ''");
                    table.CheckConstraint("CK_WorkLocations_LocationName", "LTRIM(RTRIM([LocationName])) <> ''");
                    table.CheckConstraint("CK_WorkLocations_Longitude", "[Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_WorkLocations_Radius", "[AllowedRadiusMeters] > 0");
                    table.ForeignKey(
                        name: "FK_WorkLocations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "dbo",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkLocations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "dbo",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "WorkLocations",
                columns: new[] { "Id", "Address", "AllowedRadiusMeters", "BranchId", "City", "CompanyId", "Country", "CreatedAt", "CreatedBy", "IsActive", "IsGeoFenceEnabled", "Latitude", "LocationCode", "LocationName", "Longitude", "PinCode", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("638b4a96-b60f-4cfb-b5a8-5bf4b304896a"), null, 100, new Guid("b3e36a15-4efb-441e-bbd8-f6cf916f6107"), "Patna", new Guid("ec8af472-df65-43d4-9abf-9378e553e455"), "India", new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system:seed", true, true, null, "PATNA-OFFICE", "Patna Office", null, null, "Bihar", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocations_CompanyId",
                schema: "dbo",
                table: "WorkLocations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkLocations_BranchId_LocationCode",
                schema: "dbo",
                table: "WorkLocations",
                columns: new[] { "BranchId", "LocationCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkLocations",
                schema: "dbo");
        }
    }
}
