using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint13_SchedulerAndCanonicalUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanonicalUnit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalProjectId = table.Column<long>(type: "bigint", nullable: false),
                    RepresentativeMarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    Area = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    PricePerSqm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    Floor = table.Column<int>(type: "int", nullable: true),
                    HasPriceConflict = table.Column<bool>(type: "bit", nullable: false),
                    HasStatusConflict = table.Column<bool>(type: "bit", nullable: false),
                    HasAreaConflict = table.Column<bool>(type: "bit", nullable: false),
                    ConflictSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalUnit_CanonicalProject_CanonicalProjectId",
                        column: x => x.CanonicalProjectId,
                        principalTable: "CanonicalProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanonicalUnit_MarketAsset_RepresentativeMarketAssetId",
                        column: x => x.RepresentativeMarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrawlerSourceStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastAttemptedCrawlAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessfulCrawlAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailedCrawlAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextCrawlAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    LastResultFound = table.Column<int>(type: "int", nullable: true),
                    LastResultNew = table.Column<int>(type: "int", nullable: true),
                    LastResultUpdated = table.Column<int>(type: "int", nullable: true),
                    LastResultErrors = table.Column<int>(type: "int", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    CurrentPhase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentProgress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlerSourceStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalUnitAsset",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalUnitId = table.Column<long>(type: "bigint", nullable: false),
                    MarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MatchLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    MatchReasons = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRepresentative = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalUnitAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalUnitAsset_CanonicalUnit_CanonicalUnitId",
                        column: x => x.CanonicalUnitId,
                        principalTable: "CanonicalUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanonicalUnitAsset_MarketAsset_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalUnit_CanonicalProjectId",
                table: "CanonicalUnit",
                column: "CanonicalProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalUnit_RepresentativeMarketAssetId",
                table: "CanonicalUnit",
                column: "RepresentativeMarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalUnitAsset_CanonicalUnitId",
                table: "CanonicalUnitAsset",
                column: "CanonicalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalUnitAsset_MarketAssetId",
                table: "CanonicalUnitAsset",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "UQ_CanonicalUnitAsset_Unit_Asset",
                table: "CanonicalUnitAsset",
                columns: new[] { "CanonicalUnitId", "MarketAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_CrawlerSourceStatus_SourceName",
                table: "CrawlerSourceStatus",
                column: "SourceName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanonicalUnitAsset");

            migrationBuilder.DropTable(
                name: "CrawlerSourceStatus");

            migrationBuilder.DropTable(
                name: "CanonicalUnit");
        }
    }
}
