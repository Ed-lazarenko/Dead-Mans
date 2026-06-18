using System;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617120000_AddGameQuestionSelections")]
    public partial class AddGameQuestionSelections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_question_selections",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnabledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_game_question_selections",
                        x => new { x.GameId, x.QuestionId }
                    );
                    table.ForeignKey(
                        name: "FK_game_question_selections_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_question_selections_question_definitions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "question_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_question_selections_GameId",
                table: "game_question_selections",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_game_question_selections_QuestionId",
                table: "game_question_selections",
                column: "QuestionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "game_question_selections");
        }
    }
}
