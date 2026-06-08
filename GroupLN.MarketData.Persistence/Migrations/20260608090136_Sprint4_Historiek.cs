using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4_Historiek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MissingCrawlCount",
                table: "MarketListing",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSoldAt",
                table: "MarketAsset",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "MarketAsset",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectGroupSnapshot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnitsTotal = table.Column<int>(type: "int", nullable: false),
                    UnitsSold = table.Column<int>(type: "int", nullable: false),
                    UnitsAvailable = table.Column<int>(type: "int", nullable: false),
                    UnitsReserved = table.Column<int>(type: "int", nullable: false),
                    UnitsOption = table.Column<int>(type: "int", nullable: false),
                    UnitsUnknown = table.Column<int>(type: "int", nullable: false),
                    SoldPercentage = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectGroupSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectGroupSnapshot_MarketAsset_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGroupSnapshot_MarketAssetId",
                table: "ProjectGroupSnapshot",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGroupSnapshot_SnapshotDate",
                table: "ProjectGroupSnapshot",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectGroupSnapshot");

            migrationBuilder.DropColumn(
                name: "MissingCrawlCount",
                table: "MarketListing");

            migrationBuilder.DropColumn(
                name: "FirstSoldAt",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "MarketAsset");
        }
    }
}
