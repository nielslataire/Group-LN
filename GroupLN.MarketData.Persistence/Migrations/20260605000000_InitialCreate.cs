using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace GroupLN.MarketData.Persistence.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058")]
    public partial class InitialCreate : Migration
    {
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
                    CrawlFrequencyHours = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
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

            // Indexen
            migrationBuilder.CreateIndex("IX_CrawlerSource_Name", "CrawlerSource", "Name", unique: true);
            migrationBuilder.CreateIndex("IX_CrawlerRun_SourceId", "CrawlerRun", "SourceId");
            migrationBuilder.CreateIndex("IX_CrawlerRun_StartedAt", "CrawlerRun", "StartedAt");
            migrationBuilder.CreateIndex("IX_CrawlerRun_Status", "CrawlerRun", "Status");
            migrationBuilder.CreateIndex("UQ_MarketProperty_Source_ExternalId", "MarketProperty", new[] { "SourceId", "ExternalId" }, unique: true);
            migrationBuilder.CreateIndex("IX_MarketProperty_PostalCode", "MarketProperty", "PostalCode");
            migrationBuilder.CreateIndex("IX_MarketProperty_City", "MarketProperty", "City");
            migrationBuilder.CreateIndex("IX_MarketProperty_PropertyType", "MarketProperty", "PropertyType");
            migrationBuilder.CreateIndex("IX_MarketProperty_TransactionType", "MarketProperty", "TransactionType");
            migrationBuilder.CreateIndex("IX_MarketProperty_IsActive", "MarketProperty", "IsActive");
            migrationBuilder.CreateIndex("IX_MarketProperty_FirstSeenAt", "MarketProperty", "FirstSeenAt");
            migrationBuilder.CreateIndex("IX_MarketProperty_LastSeenAt", "MarketProperty", "LastSeenAt");
            migrationBuilder.CreateIndex("IX_MarketPropertySnapshot_MarketPropertyId", "MarketPropertySnapshot", "MarketPropertyId");
            migrationBuilder.CreateIndex("IX_MarketPropertySnapshot_SnapshotDate", "MarketPropertySnapshot", "SnapshotDate");
            migrationBuilder.CreateIndex("IX_MarketPropertyPriceHistory_MarketPropertyId", "MarketPropertyPriceHistory", "MarketPropertyId");
            migrationBuilder.CreateIndex("IX_MarketPropertyPriceHistory_DetectedAt", "MarketPropertyPriceHistory", "DetectedAt");

            // Seed data — bronnen
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "CrawlerSource",
                columns: new[] { "Id", "Name", "BaseUrl", "IsActive", "CrawlFrequencyHours", "LastCrawledAt", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    // Alleen Immoweb actief in fase 1 — andere bronnen nog niet geïmplementeerd
                    { 1, "Immoweb",      "https://www.immoweb.be",    true,  12,  null, seedDate, seedDate },
                    { 2, "Zimmo",        "https://www.zimmo.be",       false, 12,  null, seedDate, seedDate },
                    { 3, "Immoscoop",    "https://www.immoscoop.be",   false, 24,  null, seedDate, seedDate },
                    { 4, "Immovlan",     "https://www.immovlan.be",    false, 24,  null, seedDate, seedDate },
                    { 5, "Realo",        "https://www.realo.be",       false, 24,  null, seedDate, seedDate },
                    { 6, "Biddit",       "https://www.biddit.be",      false, 168, null, seedDate, seedDate },
                    { 7, "ImmoNotaire",  "https://www.immonotaire.be", false, 168, null, seedDate, seedDate }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MarketPropertyPriceHistory");
            migrationBuilder.DropTable(name: "MarketPropertySnapshot");
            migrationBuilder.DropTable(name: "MarketProperty");
            migrationBuilder.DropTable(name: "CrawlerRun");
            migrationBuilder.DropTable(name: "CrawlerSource");
        }
    }
}
