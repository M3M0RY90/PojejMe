using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Recepti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recepti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Opis = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SteviloPorcij = table.Column<int>(type: "int", nullable: false),
                    SlikaPot = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvtorId = table.Column<int>(type: "int", nullable: false),
                    UstvarjenOb = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PosodobljenOb = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recepti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recepti_Uporabniki_AvtorId",
                        column: x => x.AvtorId,
                        principalTable: "Uporabniki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "KorakiPriprave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReceptId = table.Column<int>(type: "int", nullable: false),
                    Zaporedje = table.Column<int>(type: "int", nullable: false),
                    Opis = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KorakiPriprave", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KorakiPriprave_Recepti_ReceptId",
                        column: x => x.ReceptId,
                        principalTable: "Recepti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SestavineReceptov",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReceptId = table.Column<int>(type: "int", nullable: false),
                    ZiviloId = table.Column<int>(type: "int", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Enota = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SestavineReceptov", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SestavineReceptov_Recepti_ReceptId",
                        column: x => x.ReceptId,
                        principalTable: "Recepti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SestavineReceptov_Zivila_ZiviloId",
                        column: x => x.ZiviloId,
                        principalTable: "Zivila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_KorakiPriprave_ReceptId",
                table: "KorakiPriprave",
                column: "ReceptId");

            migrationBuilder.CreateIndex(
                name: "IX_Recepti_AvtorId",
                table: "Recepti",
                column: "AvtorId");

            migrationBuilder.CreateIndex(
                name: "IX_SestavineReceptov_ReceptId",
                table: "SestavineReceptov",
                column: "ReceptId");

            migrationBuilder.CreateIndex(
                name: "IX_SestavineReceptov_ZiviloId",
                table: "SestavineReceptov",
                column: "ZiviloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KorakiPriprave");

            migrationBuilder.DropTable(
                name: "SestavineReceptov");

            migrationBuilder.DropTable(
                name: "Recepti");
        }
    }
}
