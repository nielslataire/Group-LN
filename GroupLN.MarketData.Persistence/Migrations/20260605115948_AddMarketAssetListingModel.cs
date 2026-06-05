using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketAssetListingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠ DATA-MIGRATIE WAARSCHUWING ⚠
            // Deze migratie verwijdert de oude MarketProperty, MarketPropertySnapshot en MarketPropertyPriceHistory tabellen.
            // Als er al productiedata in die tabellen bestaat, moet u EERST een data-migratie uitvoeren:
            //   1. Maak een MarketAsset aan voor elk uniek pand (per locatie/type combinatie)
            //   2. Maak een MarketListing aan voor elke MarketProperty rij, met de juiste MarketAssetId
            //   3. Kopieer snapshots van MarketPropertySnapshot → MarketListingSnapshot
            //   4. Kopieer prijshistoriek van MarketPropertyPriceHistory → MarketListingPriceHistory
            // In de huidige development-fase is er nog geen data, dus droppen is veilig.

            migrationBuilder.DropTable(
                name: "MarketPropertyPriceHistory");

            migrationBuilder.DropTable(
                name: "MarketPropertySnapshot");

            migrationBuilder.DropTable(
                name: "MarketProperty");

            migrationBuilder.CreateTable(
                name: "MarketAsset",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    LivingArea = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LandArea = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    ConstructionYear = table.Column<int>(type: "int", nullable: true),
                    EPCScore = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    EPCLabel = table.Column<int>(type: "int", nullable: true),
                    NewBuild = table.Column<bool>(type: "bit", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAsset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketAssetMatchCandidate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExistingMarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MatchReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAssetMatchCandidate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketAssetMatchCandidate_MarketAsset_ExistingMarketAssetId",
                        column: x => x.ExistingMarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketListing",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AskingPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListing_CrawlerSource_SourceId",
                        column: x => x.SourceId,
                        principalTable: "CrawlerSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketListing_MarketAsset_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketListingPriceHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketListingId = table.Column<long>(type: "bigint", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    PreviousPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    PriceChangeAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    PriceChangePercentage = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListingPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListingPriceHistory_MarketListing_MarketListingId",
                        column: x => x.MarketListingId,
                        principalTable: "MarketListing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketListingSnapshot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketListingId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_MarketListingSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListingSnapshot_MarketListing_MarketListingId",
                        column: x => x.MarketListingId,
                        principalTable: "MarketListing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_City",
                table: "MarketAsset",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_Coordinates",
                table: "MarketAsset",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_FirstSeenAt",
                table: "MarketAsset",
                column: "FirstSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_IsActive",
                table: "MarketAsset",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_LastSeenAt",
                table: "MarketAsset",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_PostalCode",
                table: "MarketAsset",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_PropertyType",
                table: "MarketAsset",
                column: "PropertyType");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_TransactionType",
                table: "MarketAsset",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "UQ_MarketAsset_AssetKey",
                table: "MarketAsset",
                column: "AssetKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_ExistingMarketAssetId",
                table: "MarketAssetMatchCandidate",
                column: "ExistingMarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_IsConfirmed",
                table: "MarketAssetMatchCandidate",
                column: "IsConfirmed");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_IsRejected",
                table: "MarketAssetMatchCandidate",
                column: "IsRejected");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_MatchScore",
                table: "MarketAssetMatchCandidate",
                column: "MatchScore");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_Source_ExternalId",
                table: "MarketAssetMatchCandidate",
                columns: new[] { "SourceId", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_FirstSeenAt",
                table: "MarketListing",
                column: "FirstSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_IsActive",
                table: "MarketListing",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_LastSeenAt",
                table: "MarketListing",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_MarketAssetId",
                table: "MarketListing",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_SourceId",
                table: "MarketListing",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_Url",
                table: "MarketListing",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "UQ_MarketListing_Source_ExternalId",
                table: "MarketListing",
                columns: new[] { "SourceId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketListingPriceHistory_DetectedAt",
                table: "MarketListingPriceHistory",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListingPriceHistory_MarketListingId",
                table: "MarketListingPriceHistory",
                column: "MarketListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListingSnapshot_MarketListingId",
                table: "MarketListingSnapshot",
                column: "MarketListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListingSnapshot_SnapshotDate",
                table: "MarketListingSnapshot",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketAssetMatchCandidate");

            migrationBuilder.DropTable(
                name: "MarketListingPriceHistory");

            migrationBuilder.DropTable(
                name: "MarketListingSnapshot");

            migrationBuilder.DropTable(
                name: "MarketListing");

            migrationBuilder.DropTable(
                name: "MarketAsset");

            migrationBuilder.CreateTable(
                name: "MarketProperty",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false, defaultValue: "BE"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PropertySubType = table.Column<int>(type: "int", nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
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
                    AskingPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    AskingPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    ConstructionYear = table.Column<int>(type: "int", nullable: true),
                    DescriptionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EPCLabel = table.Column<int>(type: "int", nullable: true),
                    EPCScore = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    GarageCount = table.Column<int>(type: "int", nullable: true),
                    IsNewBuild = table.Column<bool>(type: "bit", nullable: false),
                    LandArea = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LivingArea = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
    }
}
