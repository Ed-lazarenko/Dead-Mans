using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameUserNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_user_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModifierName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QuizPointsDelta = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_user_notifications", x => x.Id);
                    table.CheckConstraint("CK_game_user_notifications_quiz_points_delta_non_negative", "\"QuizPointsDelta\" IS NULL OR \"QuizPointsDelta\" >= 0");
                    table.ForeignKey(
                        name: "FK_game_user_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_user_notifications_UserId_ReadAtUtc_CreatedAtUtc",
                table: "game_user_notifications",
                columns: new[] { "UserId", "ReadAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_user_notifications_UserId_Type_CreatedAtUtc",
                table: "game_user_notifications",
                columns: new[] { "UserId", "Type", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_user_notifications");
        }
    }
}
