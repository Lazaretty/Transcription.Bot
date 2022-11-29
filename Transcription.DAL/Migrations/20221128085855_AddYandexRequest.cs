using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Transcription.DAL.Migrations
{
    public partial class AddYandexRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ChatStates_UserId",
                table: "Users");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "ChatStateId",
                table: "ChatStates",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "YandexRequests",
                columns: table => new
                {
                    YandexRequestId = table.Column<string>(type: "text", nullable: false),
                    UserChatId = table.Column<long>(type: "bigint", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YandexRequests", x => x.YandexRequestId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatStates_ChatStateId",
                table: "ChatStates",
                column: "ChatStateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatStates_Users_ChatStateId",
                table: "ChatStates",
                column: "ChatStateId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatStates_Users_ChatStateId",
                table: "ChatStates");

            migrationBuilder.DropTable(
                name: "YandexRequests");

            migrationBuilder.DropIndex(
                name: "IX_ChatStates_ChatStateId",
                table: "ChatStates");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "ChatStateId",
                table: "ChatStates",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ChatStates_UserId",
                table: "Users",
                column: "UserId",
                principalTable: "ChatStates",
                principalColumn: "ChatStateId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
