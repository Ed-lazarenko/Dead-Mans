using System;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260809143000_AddGameTeamPlayedAt")]
    public partial class AddGameTeamPlayedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "played_at_utc",
                table: "game_teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE game_teams
                SET played_at_utc = updated_at_utc
                WHERE is_played = true
                    AND played_at_utc IS NULL;
                """
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_teams_played_timestamp_semantics",
                table: "game_teams",
                sql: "(is_played = true AND played_at_utc IS NOT NULL) OR (is_played = false AND played_at_utc IS NULL)"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_game_teams_played_timestamp_semantics",
                table: "game_teams"
            );

            migrationBuilder.DropColumn(
                name: "played_at_utc",
                table: "game_teams"
            );
        }
    }
}
