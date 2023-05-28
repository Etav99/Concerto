using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concerto.Server.Migrations
{
    /// <inheritdoc />
    public partial class DawInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DawProjects",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DawProjects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<float>(type: "real", nullable: false),
                    Volume = table.Column<float>(type: "real", nullable: false),
                    SelectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AudioSourceGuid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => new { x.ProjectId, x.Id });
                    table.ForeignKey(
                        name: "FK_Tracks_DawProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "DawProjects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "DawProjects");
        }
    }
}
