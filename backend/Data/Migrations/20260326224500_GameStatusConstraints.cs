using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    public partial class GameStatusConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize existing data so that CHECK constraints won't fail on already-inconsistent rows.
            // 1) Unknown statuses => draft
            migrationBuilder.Sql(@"
UPDATE ""games""
SET ""Status"" = 'draft'
WHERE ""Status"" NOT IN ('draft','active','finished');
");

            // 2) If FinishedAtUtc is set, status must be finished.
            migrationBuilder.Sql(@"
UPDATE ""games""
SET ""Status"" = 'finished'
WHERE ""FinishedAtUtc"" IS NOT NULL
  AND ""Status"" <> 'finished';
");

            // 3) If status is finished but FinishedAtUtc is null, downgrade to draft.
            migrationBuilder.Sql(@"
UPDATE ""games""
SET ""Status"" = 'draft'
WHERE ""Status"" = 'finished'
  AND ""FinishedAtUtc"" IS NULL;
");

            // Allowed statuses list
            migrationBuilder.Sql(@"
ALTER TABLE ""games""
ADD CONSTRAINT ""CK_games_status_allowed""
CHECK (""Status"" IN ('draft','active','finished'));
");

            // FinishedAtUtc semantics:
            // draft/active => FinishedAtUtc is NULL
            // finished => FinishedAtUtc is NOT NULL
            migrationBuilder.Sql(@"
ALTER TABLE ""games""
ADD CONSTRAINT ""CK_games_finishedat_semantics""
CHECK (
    (""Status"" IN ('draft','active') AND ""FinishedAtUtc"" IS NULL)
    OR
    (""Status"" = 'finished' AND ""FinishedAtUtc"" IS NOT NULL)
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""games""
DROP CONSTRAINT IF EXISTS ""CK_games_finishedat_semantics"";
");

            migrationBuilder.Sql(@"
ALTER TABLE ""games""
DROP CONSTRAINT IF EXISTS ""CK_games_status_allowed"";
");
        }
    }
}

