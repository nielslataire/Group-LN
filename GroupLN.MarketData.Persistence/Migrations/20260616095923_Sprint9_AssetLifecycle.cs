using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupLN.MarketData.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint9_AssetLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LifecycleConfidence",
                table: "MarketAsset",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleSource",
                table: "MarketAsset",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleStatus",
                table: "MarketAsset",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatusReason",
                table: "MarketAsset",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LifecycleStatusUpdatedAt",
                table: "MarketAsset",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LifecycleConfidence",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LifecycleSource",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LifecycleStatusReason",
                table: "MarketAsset");

            migrationBuilder.DropColumn(
                name: "LifecycleStatusUpdatedAt",
                table: "MarketAsset");
        }
    }
}
