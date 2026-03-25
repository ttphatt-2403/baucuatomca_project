using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BauCuaTomCa.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReady : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "RoomPlayers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "RoomPlayers");
        }
    }
}
