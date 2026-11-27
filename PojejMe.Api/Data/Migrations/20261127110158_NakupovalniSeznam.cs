using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NakupovalniSeznam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostavkeNakupovalnegaSeznama",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GospodinjstvoId = table.Column<int>(type: "int", nullable: false),
                    ZiviloId = table.Column<int>(type: "int", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Enota = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DodanoOb = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IzReceptaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostavkeNakupovalnegaSeznama", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostavkeNakupovalnegaSeznama_Gospodinjstva_GospodinjstvoId",
                        column: x => x.GospodinjstvoId,
                        principalTable: "Gospodinjstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostavkeNakupovalnegaSeznama_Zivila_ZiviloId",
                        column: x => x.ZiviloId,
                        principalTable: "Zivila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PostavkeNakupovalnegaSeznama_GospodinjstvoId",
                table: "PostavkeNakupovalnegaSeznama",
                column: "GospodinjstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_PostavkeNakupovalnegaSeznama_ZiviloId",
                table: "PostavkeNakupovalnegaSeznama",
                column: "ZiviloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostavkeNakupovalnegaSeznama");
        }
    }
}
