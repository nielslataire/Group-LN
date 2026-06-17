using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint10_CanonicalProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanonicalProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    GeoMunicipalityId = table.Column<int>(type: "int", nullable: true),
                    GeoMunicipalSectionId = table.Column<int>(type: "int", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalProject_GeoMunicipalSection_GeoMunicipalSectionId",
                        column: x => x.GeoMunicipalSectionId,
                        principalTable: "GeoMunicipalSection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CanonicalProject_GeoMunicipality_GeoMunicipalityId",
                        column: x => x.GeoMunicipalityId,
                        principalTable: "GeoMunicipality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalProjectAsset",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalProjectId = table.Column<long>(type: "bigint", nullable: false),
                    MarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    MatchLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    MatchReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalProjectAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalProjectAsset_CanonicalProject_CanonicalProjectId",
                        column: x => x.CanonicalProjectId,
                        principalTable: "CanonicalProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanonicalProjectAsset_MarketAsset_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProject_GeoMunicipalityId",
                table: "CanonicalProject",
                column: "GeoMunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProject_GeoMunicipalSectionId",
                table: "CanonicalProject",
                column: "GeoMunicipalSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProject_IsActive",
                table: "CanonicalProject",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProject_NormalizedName",
                table: "CanonicalProject",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProjectAsset_CanonicalProjectId",
                table: "CanonicalProjectAsset",
                column: "CanonicalProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProjectAsset_IsPrimary",
                table: "CanonicalProjectAsset",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalProjectAsset_MarketAssetId",
                table: "CanonicalProjectAsset",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "UQ_CanonicalProjectAsset_Project_Asset",
                table: "CanonicalProjectAsset",
                columns: new[] { "CanonicalProjectId", "MarketAssetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanonicalProjectAsset");

            migrationBuilder.DropTable(
                name: "CanonicalProject");
        }
    }
}
