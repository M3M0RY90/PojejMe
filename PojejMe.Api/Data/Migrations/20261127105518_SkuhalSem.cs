using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SkuhalSem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PripraveReceptov",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReceptId = table.Column<int>(type: "int", nullable: false),
                    SteviloPorcij = table.Column<int>(type: "int", nullable: false),
                    SkuhanoOb = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Kljuc = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PripraveReceptov", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PripraveReceptov_Recepti_ReceptId",
                        column: x => x.ReceptId,
                        principalTable: "Recepti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PripraveReceptov_ReceptId_Kljuc",
                table: "PripraveReceptov",
                columns: new[] { "ReceptId", "Kljuc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PripraveReceptov");
        }
    }
}
