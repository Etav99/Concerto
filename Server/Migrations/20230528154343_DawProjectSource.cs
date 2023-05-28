using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concerto.Server.Migrations
{
    /// <inheritdoc />
    public partial class DawProjectSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AudioSourceGuid",
                table: "DawProjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioSourceHash",
                table: "DawProjects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioSourceGuid",
                table: "DawProjects");

            migrationBuilder.DropColumn(
                name: "AudioSourceHash",
                table: "DawProjects");
        }
    }
}
