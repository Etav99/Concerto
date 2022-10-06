using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concerto.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomId);
                    table.ForeignKey(
                        name: "FK_Rooms_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    ChatMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SendTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.ChatMessageId);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    User1Id = table.Column<long>(type: "bigint", nullable: false),
                    User2Id = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => new { x.User1Id, x.User2Id });
                    table.ForeignKey(
                        name: "FK_Contacts_Users_User1Id",
                        column: x => x.User1Id,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contacts_Users_User2Id",
                        column: x => x.User2Id,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationUser",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationUser", x => new { x.ConversationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ConversationUser_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationUser_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomUsers",
                columns: table => new
                {
                    RoomId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomUsers", x => new { x.RoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RoomUsers_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RoomId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    StorageName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Conversations",
                columns: new[] { "ConversationId", "IsPrivate" },
                values: new object[,]
                {
                    { 1L, true },
                    { 2L, true },
                    { 3L, true },
                    { 4L, true },
                    { 5L, true },
                    { 6L, true },
                    { 7L, false },
                    { 8L, false }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "FirstName", "LastName", "SubjectId", "Username" },
                values: new object[,]
                {
                    { 1L, "Jan", "Administracyjny", new Guid("95f418ac-e38f-41ec-a2ad-828bdd3895d0"), "admin" },
                    { 2L, "Piotr", "Testowy", new Guid("954af482-22dd-483f-ac99-975144f85a04"), "user2" },
                    { 3L, "Jacek", "Testowy", new Guid("c786cbc3-9924-410f-bcdb-75a2469107be"), "user3" },
                    { 4L, "John", "Smith", new Guid("f2c0a648-82bb-44a9-908e-8006577cb276"), "user4" }
                });

            migrationBuilder.InsertData(
                table: "ChatMessages",
                columns: new[] { "ChatMessageId", "Content", "ConversationId", "SendTimestamp", "SenderId" },
                values: new object[,]
                {
                    { 1L, "Test message 1", 1L, new DateTime(2022, 10, 2, 23, 9, 8, 351, DateTimeKind.Utc).AddTicks(421), 1L },
                    { 2L, "Test message 2", 1L, new DateTime(2022, 10, 2, 23, 11, 8, 351, DateTimeKind.Utc).AddTicks(424), 1L },
                    { 3L, "Test reply 1", 1L, new DateTime(2022, 10, 2, 23, 12, 8, 351, DateTimeKind.Utc).AddTicks(425), 2L },
                    { 4L, "Test reply 2", 1L, new DateTime(2022, 10, 2, 23, 13, 8, 351, DateTimeKind.Utc).AddTicks(426), 2L },
                    { 5L, "Test message 3", 1L, new DateTime(2022, 10, 2, 23, 13, 8, 351, DateTimeKind.Utc).AddTicks(426), 1L }
                });

            migrationBuilder.InsertData(
                table: "Contacts",
                columns: new[] { "User1Id", "User2Id", "Status" },
                values: new object[,]
                {
                    { 1L, 2L, 1 },
                    { 1L, 3L, 1 },
                    { 1L, 4L, 1 },
                    { 2L, 3L, 1 },
                    { 2L, 4L, 1 },
                    { 3L, 4L, 1 }
                });

            migrationBuilder.InsertData(
                table: "ConversationUser",
                columns: new[] { "ConversationId", "UserId" },
                values: new object[,]
                {
                    { 1L, 1L },
                    { 1L, 2L },
                    { 2L, 1L },
                    { 2L, 3L },
                    { 3L, 1L },
                    { 3L, 4L },
                    { 4L, 2L },
                    { 4L, 3L },
                    { 5L, 2L },
                    { 5L, 4L },
                    { 6L, 3L },
                    { 6L, 4L },
                    { 7L, 1L },
                    { 7L, 2L },
                    { 7L, 3L },
                    { 8L, 1L },
                    { 8L, 4L }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "RoomId", "ConversationId", "Name" },
                values: new object[,]
                {
                    { 1L, 7L, "Room 1" },
                    { 2L, 8L, "Room 2" }
                });

            migrationBuilder.InsertData(
                table: "RoomUsers",
                columns: new[] { "RoomId", "UserId", "Role" },
                values: new object[,]
                {
                    { 1L, 1L, 0 },
                    { 1L, 2L, 0 },
                    { 1L, 3L, 0 },
                    { 2L, 1L, 0 },
                    { 2L, 4L, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_User2Id",
                table: "Contacts",
                column: "User2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationUser_UserId",
                table: "ConversationUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_ConversationId",
                table: "Rooms",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomUsers_UserId",
                table: "RoomUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ConversationId",
                table: "Sessions",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RoomId",
                table: "Sessions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_SessionId",
                table: "UploadedFiles",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubjectId",
                table: "Users",
                column: "SubjectId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "ConversationUser");

            migrationBuilder.DropTable(
                name: "RoomUsers");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}
