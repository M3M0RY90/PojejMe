using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PorabaInZavrzki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DogodkiZaloge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GospodinjstvoId = table.Column<int>(type: "int", nullable: false),
                    ZiviloId = table.Column<int>(type: "int", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Enota = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Vrsta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Razlog = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UporabnikId = table.Column<int>(type: "int", nullable: false),
                    Cas = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogodkiZaloge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DogodkiZaloge_Gospodinjstva_GospodinjstvoId",
                        column: x => x.GospodinjstvoId,
                        principalTable: "Gospodinjstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DogodkiZaloge_Uporabniki_UporabnikId",
                        column: x => x.UporabnikId,
                        principalTable: "Uporabniki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DogodkiZaloge_Zivila_ZiviloId",
                        column: x => x.ZiviloId,
                        principalTable: "Zivila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DogodkiZaloge_GospodinjstvoId",
                table: "DogodkiZaloge",
                column: "GospodinjstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_DogodkiZaloge_UporabnikId",
                table: "DogodkiZaloge",
                column: "UporabnikId");

            migrationBuilder.CreateIndex(
                name: "IX_DogodkiZaloge_ZiviloId",
                table: "DogodkiZaloge",
                column: "ZiviloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogodkiZaloge");
        }
    }
}
