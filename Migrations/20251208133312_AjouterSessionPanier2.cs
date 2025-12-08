using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetTestDotNet.Migrations
{
    /// <inheritdoc />
    public partial class AjouterSessionPanier2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpiration",
                table: "Paniers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PrixUnitaire",
                table: "Paniers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "Paniers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Paniers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateExpiration",
                table: "Paniers");

            migrationBuilder.DropColumn(
                name: "PrixUnitaire",
                table: "Paniers");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Paniers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Paniers");
        }
    }
}
