using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concerto.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackSelectedByName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedBy",
                table: "Tracks",
                newName: "SelectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_SelectedByUserId",
                table: "Tracks",
                column: "SelectedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Users_SelectedByUserId",
                table: "Tracks",
                column: "SelectedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Users_SelectedByUserId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_SelectedByUserId",
                table: "Tracks");

            migrationBuilder.RenameColumn(
                name: "SelectedByUserId",
                table: "Tracks",
                newName: "SelectedBy");
        }
    }
}
