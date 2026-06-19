using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint12_PhotoHashAndAiExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectAiExtractionCache",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RawTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtractedProjectName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProjectNameConfidence = table.Column<int>(type: "int", nullable: false),
                    IsMarketingTitle = table.Column<bool>(type: "bit", nullable: false),
                    ExtractedStreet = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ExtractedHouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExtractedPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ExtractedCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExtractedDeveloper = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExtractedNumberOfUnits = table.Column<int>(type: "int", nullable: true),
                    ExtractedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAiExtractionCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPhotoHash",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAssetId = table.Column<long>(type: "bigint", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProjectExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NormalizedImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageOrder = table.Column<int>(type: "int", nullable: false),
                    HashAlgorithm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PerceptualHash = table.Column<long>(type: "bigint", nullable: true),
                    PerceptualHashVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPhotoHash", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPhotoHash_MarketAsset_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiExtractionCache_InputHash",
                table: "ProjectAiExtractionCache",
                column: "InputHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAiExtractionCache_Source_ExternalId",
                table: "ProjectAiExtractionCache",
                columns: new[] { "SourceName", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhotoHash_ContentHash",
                table: "ProjectPhotoHash",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhotoHash_MarketAssetId",
                table: "ProjectPhotoHash",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhotoHash_NormalizedImageUrl",
                table: "ProjectPhotoHash",
                column: "NormalizedImageUrl");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhotoHash_PerceptualHash",
                table: "ProjectPhotoHash",
                column: "PerceptualHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhotoHash_Source_ExternalId",
                table: "ProjectPhotoHash",
                columns: new[] { "SourceName", "ProjectExternalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectAiExtractionCache");

            migrationBuilder.DropTable(
                name: "ProjectPhotoHash");
        }
    }
}
