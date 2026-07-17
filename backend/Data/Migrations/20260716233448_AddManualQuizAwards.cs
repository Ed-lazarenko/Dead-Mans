using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManualQuizAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_quiz_manual_awards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwardedToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwardedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    AwardedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_quiz_manual_awards", x => x.Id);
                    table.CheckConstraint("CK_game_quiz_manual_awards_points_positive", "\"Points\" > 0");
                    table.ForeignKey(
                        name: "FK_game_quiz_manual_awards_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_quiz_manual_awards_users_AwardedByUserId",
                        column: x => x.AwardedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_quiz_manual_awards_users_AwardedToUserId",
                        column: x => x.AwardedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_quiz_manual_awards_AwardedByUserId_AwardedAtUtc",
                table: "game_quiz_manual_awards",
                columns: new[] { "AwardedByUserId", "AwardedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_quiz_manual_awards_AwardedToUserId_AwardedAtUtc",
                table: "game_quiz_manual_awards",
                columns: new[] { "AwardedToUserId", "AwardedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_game_quiz_manual_awards_GameId_AwardedAtUtc",
                table: "game_quiz_manual_awards",
                columns: new[] { "GameId", "AwardedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_quiz_manual_awards");
        }
    }
}
