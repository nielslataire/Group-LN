using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint15_LooseListingMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LinkedCanonicalUnitId",
                table: "MarketAsset",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFromLooseListing",
                table: "CanonicalUnitAsset",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_LinkedCanonicalUnitId",
                table: "MarketAsset",
                column: "LinkedCanonicalUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketAsset_LinkedCanonicalUnitId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LinkedCanonicalUnitId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "IsFromLooseListing",
                table: "CanonicalUnitAsset");
        }
    }
}
