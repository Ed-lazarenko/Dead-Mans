using System;
using System.Security.Cryptography;
using System.Text;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326195000_SeedTestGameWithLoadoutMedia")]
    public partial class SeedTestGameWithLoadoutMedia : Migration
    {
        private const string Bucket = "deadman";
        private const string Group = "elements";

        // Must stay in sync with the storage seeder script.
        private static readonly Guid TestGameId = Guid.Parse("c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a");

        private static DateTime FixedCreatedAtUtc =>
            new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc);

        private static Guid DeterministicGuid(string seed)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return new Guid(bytes);
        }

        private static string SqlLiteral(string value)
        {
            if (value == null)
            {
                return "NULL";
            }

            // Escape single-quotes for SQL string literal.
            var escaped = value.Replace("'", "''");
            return $"'{escaped}'";
        }

        private static string SqlGuid(Guid value)
        {
            return $"'{value}'::uuid";
        }

        private static string SqlTimestamptz(DateTime valueUtc)
        {
            // valueUtc is always in UTC (FixedCreatedAtUtc).
            var iso = valueUtc.ToString("O"); // e.g. 2026-03-26T00:00:00.0000000Z
            return $"'{iso}'::timestamptz";
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = FixedCreatedAtUtc;
            var createdAt = now;
            var rowLabels = new[] { "100", "125", "150", "175", "200" };
            var colLabels = new[]
            {
                "Бомбардир",
                "Пиромант",
                "Токсик",
                "Вампир",
                "Аватар",
                "Всё могу x2"
            };

            var boardId = DeterministicGuid($"deadmans-test-board-{TestGameId}");

            migrationBuilder.Sql(
                $@"
INSERT INTO ""games"" (""Id"", ""Title"", ""Description"", ""Status"", ""CreatedAtUtc"", ""StartedAtUtc"", ""FinishedAtUtc"")
VALUES (
  {SqlGuid(TestGameId)},
  {SqlLiteral("Test Game (Loadout Media Seed)")},
  NULL,
  {SqlLiteral("active")},
  {SqlTimestamptz(createdAt)},
  NULL,
  NULL
);"
            );

            migrationBuilder.Sql(
                $@"
INSERT INTO ""game_boards"" (""Id"", ""GameId"", ""Version"", ""Rows"", ""Cols"", ""RowLabels"", ""ColLabels"", ""CreatedAtUtc"")
VALUES (
  {SqlGuid(boardId)},
  {SqlGuid(TestGameId)},
  1,
  {rowLabels.Length},
  {colLabels.Length},
  {SqlLiteral(@"[""100"",""125"",""150"",""175"",""200""]")}::jsonb,
  {SqlLiteral(@"[""Бомбардир"",""Пиромант"",""Токсик"",""Вампир"",""Аватар"",""Всё могу x2""]")}::jsonb,
  {SqlTimestamptz(createdAt)}
);"
            );

            for (var row = 0; row < rowLabels.Length; row += 1)
            {
                for (var col = 0; col < colLabels.Length; col += 1)
                {
                    var filename = $"{col + 1}-{row + 1}.png";
                    var objectKey = $"games/{TestGameId}/{Group}/{filename}";

                    var cellId = DeterministicGuid($"deadmans-test-cell-{TestGameId}-{filename}");
                    var mediaAssetId = DeterministicGuid($"deadmans-test-media-{TestGameId}-{filename}");
                    var linkId = DeterministicGuid($"deadmans-test-link-{TestGameId}-{filename}");

                    migrationBuilder.Sql(
                        $@"
INSERT INTO ""board_cells"" (""Id"", ""BoardId"", ""RowIndex"", ""ColIndex"", ""State"", ""CellType"", ""Title"", ""Cost"", ""Description"")
VALUES (
  {SqlGuid(cellId)},
  {SqlGuid(boardId)},
  {row},
  {col},
  {SqlLiteral("closed")},
  {SqlLiteral("loadout")},
  {SqlLiteral(string.Empty)},
  {rowLabels[row]},
  NULL
);"
                    );

                    migrationBuilder.Sql(
                        $@"
INSERT INTO ""media_assets"" (""Id"", ""Bucket"", ""ObjectKey"", ""MimeType"", ""SizeBytes"", ""Scope"", ""Status"", ""CreatedAtUtc"")
VALUES (
  {SqlGuid(mediaAssetId)},
  {SqlLiteral(Bucket)},
  {SqlLiteral(objectKey)},
  {SqlLiteral("image/png")},
  0,
  {SqlLiteral("private")},
  {SqlLiteral("active")},
  {SqlTimestamptz(createdAt)}
);"
                    );

                    migrationBuilder.Sql(
                        $@"
INSERT INTO ""board_cell_media"" (""Id"", ""CellId"", ""MediaAssetId"", ""Role"", ""SortOrder"")
VALUES (
  {SqlGuid(linkId)},
  {SqlGuid(cellId)},
  {SqlGuid(mediaAssetId)},
  {SqlLiteral("content")},
  0
);"
                    );
                }
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete the inserted rows by object key prefix.
            migrationBuilder.Sql($@"
DELETE FROM media_assets
WHERE ""Bucket"" = '{Bucket}'
  AND ""ObjectKey"" LIKE 'games/{TestGameId}/{Group}/%';
");

            migrationBuilder.Sql($@"
DELETE FROM games
WHERE ""Id"" = '{TestGameId}';
");
        }
    }
}

