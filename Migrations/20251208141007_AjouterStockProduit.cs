using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetTestDotNet.Migrations
{
    /// <inheritdoc />
    public partial class AjouterStockProduit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Produits",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Produits");
        }
    }
}
