using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint8_GeoAdminVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeoMunicipalSectionId",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GeoMunicipalityId",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationResolutionConfidence",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationResolutionSource",
                table: "MarketAsset",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocationResolvedAt",
                table: "MarketAsset",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GeoMunicipality",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NisCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameDutch = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NameFrench = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NameGerman = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Boundary = table.Column<Geometry>(type: "geometry", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoMunicipality", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoMunicipalSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PseudoNis = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NisCodeMunicipality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NameDutch = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NameFrench = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NameGerman = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Boundary = table.Column<Geometry>(type: "geometry", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoMunicipalSection", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_GeoMunicipalityId",
                table: "MarketAsset",
                column: "GeoMunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_GeoMunicipalSectionId",
                table: "MarketAsset",
                column: "GeoMunicipalSectionId");

            migrationBuilder.CreateIndex(
                name: "UQ_GeoMunicipality_NisCode",
                table: "GeoMunicipality",
                column: "NisCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeoMunicipalSection_NisCodeMunicipality",
                table: "GeoMunicipalSection",
                column: "NisCodeMunicipality");

            migrationBuilder.CreateIndex(
                name: "IX_GeoMunicipalSection_ZipCode",
                table: "GeoMunicipalSection",
                column: "ZipCode");

            migrationBuilder.CreateIndex(
                name: "UQ_GeoMunicipalSection_PseudoNis",
                table: "GeoMunicipalSection",
                column: "PseudoNis",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketAsset_GeoMunicipalSection_GeoMunicipalSectionId",
                table: "MarketAsset",
                column: "GeoMunicipalSectionId",
                principalTable: "GeoMunicipalSection",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketAsset_GeoMunicipality_GeoMunicipalityId",
                table: "MarketAsset",
                column: "GeoMunicipalityId",
                principalTable: "GeoMunicipality",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarketAsset_GeoMunicipalSection_GeoMunicipalSectionId",
                table: "MarketAsset");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketAsset_GeoMunicipality_GeoMunicipalityId",
                table: "MarketAsset");

            migrationBuilder.DropTable(
                name: "GeoMunicipality");

            migrationBuilder.DropTable(
                name: "GeoMunicipalSection");

            migrationBuilder.DropIndex(
                name: "IX_MarketAsset_GeoMunicipalityId",
                table: "MarketAsset");

            migrationBuilder.DropIndex(
                name: "IX_MarketAsset_GeoMunicipalSectionId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "GeoMunicipalSectionId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "GeoMunicipalityId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LocationResolutionConfidence",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LocationResolutionSource",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LocationResolvedAt",
                table: "MarketAsset");
        }
    }
}
