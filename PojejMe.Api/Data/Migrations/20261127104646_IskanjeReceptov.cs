using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IskanjeReceptov : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CasPripraveMin",
                table: "Recepti",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceptOmejitev",
                columns: table => new
                {
                    PrehranskeOmejitveId = table.Column<int>(type: "int", nullable: false),
                    ReceptId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptOmejitev", x => new { x.PrehranskeOmejitveId, x.ReceptId });
                    table.ForeignKey(
                        name: "FK_ReceptOmejitev_PrehranskeOmejitve_PrehranskeOmejitveId",
                        column: x => x.PrehranskeOmejitveId,
                        principalTable: "PrehranskeOmejitve",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceptOmejitev_Recepti_ReceptId",
                        column: x => x.ReceptId,
                        principalTable: "Recepti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptOmejitev_ReceptId",
                table: "ReceptOmejitev",
                column: "ReceptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceptOmejitev");

            migrationBuilder.DropColumn(
                name: "CasPripraveMin",
                table: "Recepti");
        }
    }
}
