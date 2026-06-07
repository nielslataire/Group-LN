using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectGroupColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SaleStatus",
                table: "MarketListingSnapshot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectGroup",
                table: "MarketAsset",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ParentMarketAssetId",
                table: "MarketAsset",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectExternalId",
                table: "MarketAsset",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleStatus",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitExternalId",
                table: "MarketAsset",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_ParentMarketAssetId",
                table: "MarketAsset",
                column: "ParentMarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAsset_ProjectExternalId",
                table: "MarketAsset",
                column: "ProjectExternalId");

            migrationBuilder.AddForeignKey(
                name: "FK_MarketAsset_MarketAsset_ParentMarketAssetId",
                table: "MarketAsset",
                column: "ParentMarketAssetId",
                principalTable: "MarketAsset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarketAsset_MarketAsset_ParentMarketAssetId",
                table: "MarketAsset");

            migrationBuilder.DropIndex(
                name: "IX_MarketAsset_ParentMarketAssetId",
                table: "MarketAsset");

            migrationBuilder.DropIndex(
                name: "IX_MarketAsset_ProjectExternalId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "SaleStatus",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "IsProjectGroup",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "ParentMarketAssetId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "ProjectExternalId",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "SaleStatus",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "UnitExternalId",
                table: "MarketAsset");
        }
    }
}
