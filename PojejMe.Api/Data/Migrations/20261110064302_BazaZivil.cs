using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BazaZivil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alergeni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alergeni", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SkupineZivil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkupineZivil", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Zivila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SkupinaId = table.Column<int>(type: "int", nullable: false),
                    EnergijaKcal = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Beljakovine = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    OgljikoviHidrati = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Sladkorji = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Mascobe = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    NasiceneMascobe = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Vlaknine = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Sol = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zivila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zivila_SkupineZivil_SkupinaId",
                        column: x => x.SkupinaId,
                        principalTable: "SkupineZivil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ZiviloAlergen",
                columns: table => new
                {
                    AlergeniId = table.Column<int>(type: "int", nullable: false),
                    ZivilaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZiviloAlergen", x => new { x.AlergeniId, x.ZivilaId });
                    table.ForeignKey(
                        name: "FK_ZiviloAlergen_Alergeni_AlergeniId",
                        column: x => x.AlergeniId,
                        principalTable: "Alergeni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZiviloAlergen_Zivila_ZivilaId",
                        column: x => x.ZivilaId,
                        principalTable: "Zivila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Alergeni",
                columns: new[] { "Id", "Ime" },
                values: new object[,]
                {
                    { 1, "Gluten" },
                    { 2, "Raki" },
                    { 3, "Jajca" },
                    { 4, "Ribe" },
                    { 5, "Arašidi" },
                    { 6, "Soja" },
                    { 7, "Mleko" },
                    { 8, "Oreški" },
                    { 9, "Listna zelena" },
                    { 10, "Gorčično seme" },
                    { 11, "Sezamovo seme" },
                    { 12, "Žveplov dioksid in sulfiti" },
                    { 13, "Volčji bob" },
                    { 14, "Mehkužci" }
                });

            migrationBuilder.InsertData(
                table: "SkupineZivil",
                columns: new[] { "Id", "Ime" },
                values: new object[,]
                {
                    { 1, "Sadje" },
                    { 2, "Zelenjava" },
                    { 3, "Žita in žitni izdelki" },
                    { 4, "Stročnice" },
                    { 5, "Mleko in mlečni izdelki" },
                    { 6, "Meso in mesni izdelki" },
                    { 7, "Ribe in morski sadeži" },
                    { 8, "Jajca" },
                    { 9, "Oreščki in semena" },
                    { 10, "Olja in maščobe" },
                    { 11, "Sladkor in sladkarije" },
                    { 12, "Pijače" },
                    { 13, "Začimbe in omake" },
                    { 14, "Drugo" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Zivila_Ime",
                table: "Zivila",
                column: "Ime",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zivila_SkupinaId",
                table: "Zivila",
                column: "SkupinaId");

            migrationBuilder.CreateIndex(
                name: "IX_ZiviloAlergen_ZivilaId",
                table: "ZiviloAlergen",
                column: "ZivilaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZiviloAlergen");

            migrationBuilder.DropTable(
                name: "Alergeni");

            migrationBuilder.DropTable(
                name: "Zivila");

            migrationBuilder.DropTable(
                name: "SkupineZivil");
        }
    }
}
