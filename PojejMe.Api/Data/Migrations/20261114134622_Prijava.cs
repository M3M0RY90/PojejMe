using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PojejMe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Prijava : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Uporabniki",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GesloHash",
                table: "Uporabniki",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PonastavitevHash",
                table: "Uporabniki",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PonastavitevPoteceOb",
                table: "Uporabniki",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZetonVerzija",
                table: "Uporabniki",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Uporabniki",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "GesloHash", "PonastavitevHash", "PonastavitevPoteceOb", "ZetonVerzija" },
                values: new object[] { null, null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "Uporabniki",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "GesloHash", "PonastavitevHash", "PonastavitevPoteceOb", "ZetonVerzija" },
                values: new object[] { null, null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "Uporabniki",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "GesloHash", "PonastavitevHash", "PonastavitevPoteceOb", "ZetonVerzija" },
                values: new object[] { null, null, null, null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Uporabniki_Email",
                table: "Uporabniki",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Uporabniki_Email",
                table: "Uporabniki");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Uporabniki");

            migrationBuilder.DropColumn(
                name: "GesloHash",
                table: "Uporabniki");

            migrationBuilder.DropColumn(
                name: "PonastavitevHash",
                table: "Uporabniki");

            migrationBuilder.DropColumn(
                name: "PonastavitevPoteceOb",
                table: "Uporabniki");

            migrationBuilder.DropColumn(
                name: "ZetonVerzija",
                table: "Uporabniki");
        }
    }
}
