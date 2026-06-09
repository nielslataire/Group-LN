using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7_Deduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MatchScore",
                table: "MarketAssetMatchCandidate",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<long>(
                name: "CandidateMarketAssetId",
                table: "MarketAssetMatchCandidate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComparedFieldsJson",
                table: "MarketAssetMatchCandidate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchLevel",
                table: "MarketAssetMatchCandidate",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchType",
                table: "MarketAssetMatchCandidate",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_CandidateAssetId",
                table: "MarketAssetMatchCandidate",
                column: "CandidateMarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_MatchLevel",
                table: "MarketAssetMatchCandidate",
                column: "MatchLevel");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_MatchType",
                table: "MarketAssetMatchCandidate",
                column: "MatchType");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetMatchCandidate_Pair",
                table: "MarketAssetMatchCandidate",
                columns: new[] { "ExistingMarketAssetId", "CandidateMarketAssetId" },
                filter: "[CandidateMarketAssetId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketAssetMatchCandidate_CandidateAssetId",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropIndex(
                name: "IX_MarketAssetMatchCandidate_MatchLevel",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropIndex(
                name: "IX_MarketAssetMatchCandidate_MatchType",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropIndex(
                name: "IX_MarketAssetMatchCandidate_Pair",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropColumn(
                name: "CandidateMarketAssetId",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropColumn(
                name: "ComparedFieldsJson",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropColumn(
                name: "MatchLevel",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.DropColumn(
                name: "MatchType",
                table: "MarketAssetMatchCandidate");

            migrationBuilder.AlterColumn<decimal>(
                name: "MatchScore",
                table: "MarketAssetMatchCandidate",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);
        }
    }
}
