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
                "20260806204327_InitialCreate",
                "20260808120000_AddGameTeamName",
                "20260808214500_ApplyEmptyCardPenaltyToRounds",
                "20260809121000_DeclareZhazhdaScoreFormula",
                "20260809143000_AddGameTeamPlayedAt",
                "20260820140143_AddGameRoundLifecycleVersioning",
                "20260820142036_AddModifierActivationRefundAudit",
                "20260820144630_AddRoundRebuildTechnicalCancellationAudit",
                "20260820151225_AddGameModifierContentLockEmergencyDisable",
                "20260820162234_AddModifierBehaviorV2Snapshots",
                "20260820164525_ExpandModifierResultOutcomesV2",
                "20260820184215_RemoveLegacyModifierCompatibility",
                "20260823100000_EnforceSingleNonterminalGameRound"
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

        Assert.Contains("20260809121000_DeclareZhazhdaScoreFormula", script, StringComparison.Ordinal);
        Assert.Contains("$modifier_metadata$::jsonb", script, StringComparison.Ordinal);
        Assert.Contains(
            "refund-audit rollout requires manual reconciliation",
            script,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "BehaviorV2 rollout blocked: active custom modifier definitions",
            script,
            StringComparison.Ordinal
        );
        Assert.Contains("behavior_v2_snapshot_json", script, StringComparison.Ordinal);
        Assert.Contains(
            "A game has more than one nonterminal round",
            script,
            StringComparison.Ordinal
        );
        Assert.Contains("ux_game_rounds_single_nonterminal_game", script, StringComparison.Ordinal);
    }
}
