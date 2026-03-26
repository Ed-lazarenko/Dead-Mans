using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGamesAndBoardsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "game_boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Rows = table.Column<int>(type: "integer", nullable: false),
                    Cols = table.Column<int>(type: "integer", nullable: false),
                    RowLabels = table.Column<string[]>(type: "jsonb", nullable: false),
                    ColLabels = table.Column<string[]>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_boards_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_cells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowIndex = table.Column<int>(type: "integer", nullable: false),
                    ColIndex = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CellType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_cells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_board_cells_game_boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "game_boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_board_cells_BoardId",
                table: "board_cells",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_board_cells_BoardId_RowIndex_ColIndex",
                table: "board_cells",
                columns: new[] { "BoardId", "RowIndex", "ColIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_board_cells_State",
                table: "board_cells",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_game_boards_GameId",
                table: "game_boards",
                column: "GameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_games_CreatedAtUtc",
                table: "games",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_games_Status",
                table: "games",
                column: "Status");

            migrationBuilder.Sql(@"
ALTER TABLE ""games""
ADD CONSTRAINT ""CK_games_status_allowed""
CHECK (""Status"" IN ('draft','active','finished'));
");

            migrationBuilder.Sql(@"
ALTER TABLE ""games""
ADD CONSTRAINT ""CK_games_finishedat_semantics""
CHECK (
    (""Status"" IN ('draft','active') AND ""FinishedAtUtc"" IS NULL)
    OR
    (""Status"" = 'finished' AND ""FinishedAtUtc"" IS NOT NULL)
);
");

            migrationBuilder.Sql(@"
ALTER TABLE ""game_boards""
ADD CONSTRAINT ""CK_game_boards_dimensions_positive""
CHECK (""Rows"" > 0 AND ""Cols"" > 0);
");

            migrationBuilder.Sql(@"
ALTER TABLE ""game_boards""
ADD CONSTRAINT ""CK_game_boards_labels_match_dimensions""
CHECK (
    jsonb_array_length(""RowLabels"") = ""Rows""
    AND jsonb_array_length(""ColLabels"") = ""Cols""
);
");

            migrationBuilder.Sql(@"
ALTER TABLE ""board_cells""
ADD CONSTRAINT ""CK_board_cells_state_allowed""
CHECK (""State"" IN ('open','closed'));
");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_cells");

            migrationBuilder.DropTable(
                name: "game_boards");

            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
