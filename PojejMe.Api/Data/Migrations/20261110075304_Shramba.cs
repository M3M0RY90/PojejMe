using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Shramba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gospodinjstva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gospodinjstva", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Uporabniki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Ime = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GospodinjstvoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uporabniki", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uporabniki_Gospodinjstva_GospodinjstvoId",
                        column: x => x.GospodinjstvoId,
                        principalTable: "Gospodinjstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ZalogeShrambe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GospodinjstvoId = table.Column<int>(type: "int", nullable: false),
                    ZiviloId = table.Column<int>(type: "int", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Enota = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Lokacija = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RokUporabnosti = table.Column<DateOnly>(type: "date", nullable: true),
                    DodalId = table.Column<int>(type: "int", nullable: false),
                    DodanoOb = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZalogeShrambe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZalogeShrambe_Gospodinjstva_GospodinjstvoId",
                        column: x => x.GospodinjstvoId,
                        principalTable: "Gospodinjstva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZalogeShrambe_Uporabniki_DodalId",
                        column: x => x.DodalId,
                        principalTable: "Uporabniki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZalogeShrambe_Zivila_ZiviloId",
                        column: x => x.ZiviloId,
                        principalTable: "Zivila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Gospodinjstva",
                columns: new[] { "Id", "Ime" },
                values: new object[,]
                {
                    { 1, "Testno gospodinjstvo A" },
                    { 2, "Testno gospodinjstvo B" }
                });

            migrationBuilder.InsertData(
                table: "Uporabniki",
                columns: new[] { "Id", "GospodinjstvoId", "Ime" },
                values: new object[,]
                {
                    { 1, 1, "Ana" },
                    { 2, 1, "Bor" },
                    { 3, 2, "Cene" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Uporabniki_GospodinjstvoId",
                table: "Uporabniki",
                column: "GospodinjstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_ZalogeShrambe_DodalId",
                table: "ZalogeShrambe",
                column: "DodalId");

            migrationBuilder.CreateIndex(
                name: "IX_ZalogeShrambe_GospodinjstvoId",
                table: "ZalogeShrambe",
                column: "GospodinjstvoId");

            migrationBuilder.CreateIndex(
                name: "IX_ZalogeShrambe_ZiviloId",
                table: "ZalogeShrambe",
                column: "ZiviloId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZalogeShrambe");

            migrationBuilder.DropTable(
                name: "Uporabniki");

            migrationBuilder.DropTable(
                name: "Gospodinjstva");
        }
    }
}
