using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Backend.Tests.Unit.Data.Migrations;

public sealed class ApplicationMigrationChainTests
{
    [Fact]
    public void GetMigrations_IncludesEveryApplicationMigrationInChronologicalOrder()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=deadmans_migration_chain_test;Username=test;Password=test"
            )
            .Options;
        using var dbContext = new ApplicationDbContext(options);

        var migrations = dbContext.Database.GetMigrations().ToArray();

        Assert.Equal(
            [
                "20260908003848_ProductionBaseline"
            ],
            migrations
        );
    }

    [Fact]
    public void GenerateScript_ForCompleteMigrationChain_SucceedsWithoutDatabaseConnection()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=deadmans_migration_script_test;Username=test;Password=test"
            )
            .Options;
        using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.GetService<IMigrator>();

        var script = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        Assert.Contains("20260908003848_ProductionBaseline", script, StringComparison.Ordinal);
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS citext", script, StringComparison.Ordinal);
        Assert.Contains("behavior_v2_snapshot_json", script, StringComparison.Ordinal);
        Assert.Contains("game_quiz_point_ledger_entries", script, StringComparison.Ordinal);
        Assert.Contains("ux_game_rounds_single_nonterminal_game", script, StringComparison.Ordinal);
        Assert.Contains("ux_games_single_current", script, StringComparison.Ordinal);
        Assert.Contains("deadmans_assert_game_finalization", script, StringComparison.Ordinal);
        Assert.Contains("ck_games_active_roster_settled", script, StringComparison.Ordinal);
        Assert.Contains("game_finalizations", script, StringComparison.Ordinal);
        Assert.Contains("game_team_final_results", script, StringComparison.Ordinal);
        Assert.Contains("ix_game_finalizations_request_id", script, StringComparison.Ordinal);
        Assert.DoesNotContain("game_quiz_manual_awards", script, StringComparison.Ordinal);
    }
}
