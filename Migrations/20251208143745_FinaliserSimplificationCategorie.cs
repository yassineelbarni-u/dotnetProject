using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetTestDotNet.Migrations
{
    /// <inheritdoc />
    public partial class FinaliserSimplificationCategorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produits_Categories_CategorieId",
                table: "Produits");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Produits_CategorieId",
                table: "Produits");

            migrationBuilder.DropColumn(
                name: "CategorieId",
                table: "Produits");

            migrationBuilder.AddColumn<string>(
                name: "Categorie",
                table: "Produits",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categorie",
                table: "Produits");

            migrationBuilder.AddColumn<int>(
                name: "CategorieId",
                table: "Produits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Produits_CategorieId",
                table: "Produits",
                column: "CategorieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Produits_Categories_CategorieId",
                table: "Produits",
                column: "CategorieId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
