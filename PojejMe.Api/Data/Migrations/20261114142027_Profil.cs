using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Profil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrehranskeOmejitve",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrehranskeOmejitve", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UporabnikAlergen",
                columns: table => new
                {
                    AlergeniId = table.Column<int>(type: "int", nullable: false),
                    UporabnikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UporabnikAlergen", x => new { x.AlergeniId, x.UporabnikId });
                    table.ForeignKey(
                        name: "FK_UporabnikAlergen_Alergeni_AlergeniId",
                        column: x => x.AlergeniId,
                        principalTable: "Alergeni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UporabnikAlergen_Uporabniki_UporabnikId",
                        column: x => x.UporabnikId,
                        principalTable: "Uporabniki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UporabnikOmejitev",
                columns: table => new
                {
                    PrehranskeOmejitveId = table.Column<int>(type: "int", nullable: false),
                    UporabnikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UporabnikOmejitev", x => new { x.PrehranskeOmejitveId, x.UporabnikId });
                    table.ForeignKey(
                        name: "FK_UporabnikOmejitev_PrehranskeOmejitve_PrehranskeOmejitveId",
                        column: x => x.PrehranskeOmejitveId,
                        principalTable: "PrehranskeOmejitve",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UporabnikOmejitev_Uporabniki_UporabnikId",
                        column: x => x.UporabnikId,
                        principalTable: "Uporabniki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "PrehranskeOmejitve",
                columns: new[] { "Id", "Ime" },
                values: new object[,]
                {
                    { 1, "Vegetarijansko" },
                    { 2, "Vegansko" },
                    { 3, "Brez svinjine" },
                    { 4, "Halal" },
                    { 5, "Košer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UporabnikAlergen_UporabnikId",
                table: "UporabnikAlergen",
                column: "UporabnikId");

            migrationBuilder.CreateIndex(
                name: "IX_UporabnikOmejitev_UporabnikId",
                table: "UporabnikOmejitev",
                column: "UporabnikId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UporabnikAlergen");

            migrationBuilder.DropTable(
                name: "UporabnikOmejitev");

            migrationBuilder.DropTable(
                name: "PrehranskeOmejitve");
        }
    }
}
