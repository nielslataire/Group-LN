using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedPropertyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnergyFeatures",
                table: "MarketListingSnapshot",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "MarketListingSnapshot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GardenArea",
                table: "MarketListingSnapshot",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "MarketListingSnapshot",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShowerCount",
                table: "MarketListingSnapshot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TerraceArea",
                table: "MarketListingSnapshot",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToiletCount",
                table: "MarketListingSnapshot",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeveloperName",
                table: "MarketAsset",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeveloperPhone",
                table: "MarketAsset",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeveloperWebsite",
                table: "MarketAsset",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnergyFeatures",
                table: "MarketAsset",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GarageCount",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GardenArea",
                table: "MarketAsset",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "MarketAsset",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShowerCount",
                table: "MarketAsset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TerraceArea",
                table: "MarketAsset",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToiletCount",
                table: "MarketAsset",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnergyFeatures",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "GardenArea",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "ShowerCount",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "TerraceArea",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "ToiletCount",
                table: "MarketListingSnapshot");

            migrationBuilder.DropColumn(
                name: "DeveloperName",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "DeveloperPhone",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "DeveloperWebsite",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "EnergyFeatures",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "GarageCount",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "GardenArea",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "ShowerCount",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "TerraceArea",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "ToiletCount",
                table: "MarketAsset");
        }
    }
}
