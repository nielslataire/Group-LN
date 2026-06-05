using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrawlerSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CrawlFrequencyHours = table.Column<int>(type: "int", nullable: false),
                    LastCrawledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlerSource", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrawlerRun",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ListingsFound = table.Column<int>(type: "int", nullable: false),
                    ListingsCreated = table.Column<int>(type: "int", nullable: false),
                    ListingsUpdated = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<int>(type: "int", nullable: false),
                    LogMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlerRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlerRun_CrawlerSource_SourceId",
                        column: x => x.SourceId,
                        principalTable: "CrawlerSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketProperty",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    PropertySubType = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false, defaultValue: "BE"),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketProperty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketProperty_CrawlerSource_SourceId",
                        column: x => x.SourceId,
                        principalTable: "CrawlerSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketPropertyPriceHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketPropertyId = table.Column<long>(type: "bigint", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    PreviousPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    PriceChangeAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    PriceChangePercentage = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPropertyPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPropertyPriceHistory_MarketProperty_MarketPropertyId",
                        column: x => x.MarketPropertyId,
                        principalTable: "MarketProperty",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketPropertySnapshot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketPropertyId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    LivingArea = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LandArea = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    GarageCount = table.Column<int>(type: "int", nullable: true),
                    ConstructionYear = table.Column<int>(type: "int", nullable: true),
                    EPCScore = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    EPCLabel = table.Column<int>(type: "int", nullable: true),
                    IsNewBuild = table.Column<bool>(type: "bit", nullable: false),
                    DescriptionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPropertySnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPropertySnapshot_MarketProperty_MarketPropertyId",
                        column: x => x.MarketPropertyId,
                        principalTable: "MarketProperty",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CrawlerSource",
                columns: new[] { "Id", "BaseUrl", "CrawlFrequencyHours", "CreatedAt", "IsActive", "LastCrawledAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "https://www.immoweb.be", 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Immoweb", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "https://www.zimmo.be", 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Zimmo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "https://www.immoscoop.be", 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Immoscoop", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "https://www.immovlan.be", 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Immovlan", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "https://www.realo.be", 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Realo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "https://www.biddit.be", 168, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Biddit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "https://www.immonotaire.be", 168, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "ImmoNotaire", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlerRun_SourceId",
                table: "CrawlerRun",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlerRun_StartedAt",
                table: "CrawlerRun",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlerRun_Status",
                table: "CrawlerRun",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlerSource_Name",
                table: "CrawlerSource",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_City",
                table: "MarketProperty",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_FirstSeenAt",
                table: "MarketProperty",
                column: "FirstSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_IsActive",
                table: "MarketProperty",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_LastSeenAt",
                table: "MarketProperty",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_PostalCode",
                table: "MarketProperty",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_PropertyType",
                table: "MarketProperty",
                column: "PropertyType");

            migrationBuilder.CreateIndex(
                name: "IX_MarketProperty_TransactionType",
                table: "MarketProperty",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "UQ_MarketProperty_Source_ExternalId",
                table: "MarketProperty",
                columns: new[] { "SourceId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketPropertyPriceHistory_DetectedAt",
                table: "MarketPropertyPriceHistory",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPropertyPriceHistory_MarketPropertyId",
                table: "MarketPropertyPriceHistory",
                column: "MarketPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPropertySnapshot_MarketPropertyId",
                table: "MarketPropertySnapshot",
                column: "MarketPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPropertySnapshot_SnapshotDate",
                table: "MarketPropertySnapshot",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawlerRun");

            migrationBuilder.DropTable(
                name: "MarketPropertyPriceHistory");

            migrationBuilder.DropTable(
                name: "MarketPropertySnapshot");

            migrationBuilder.DropTable(
                name: "MarketProperty");

            migrationBuilder.DropTable(
                name: "CrawlerSource");
        }
    }
}
