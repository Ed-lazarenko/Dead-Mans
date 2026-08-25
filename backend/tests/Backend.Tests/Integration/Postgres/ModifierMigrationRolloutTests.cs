using backend.Data;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Backend.Tests.Integration.Postgres;

public sealed class ModifierMigrationRolloutTests
{
    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=deadmans;Password=deadmans_dev_password;SSL Mode=Disable";

    [Fact]
    public async Task ExistingDatabaseBeforeZhazhdaMigration_PreservesAdminEditedCatalogValues()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString, "20260808214500_ApplyEmptyCardPenaltyToRounds");
            await ExecuteAsync(
                connectionString,
                """
                UPDATE modifier_definitions
                SET name = 'Жажда: локальная редакция',
                    description = 'Администратор изменил описание.',
                    activation_cost = 17
                WHERE id = '10000000-0000-0000-0000-000000000002';
                """
            );

            await MigrateAsync(connectionString);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT name, description, activation_cost,
                       behavior_v2_json ->> 'schemaVersion'
                FROM modifier_definitions
                WHERE id = '10000000-0000-0000-0000-000000000002';
                """;
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Жажда: локальная редакция", reader.GetString(0));
            Assert.Equal("Администратор изменил описание.", reader.GetString(1));
            Assert.Equal(17, reader.GetInt32(2));
            Assert.Equal("2", reader.GetString(3));
        });
    }

    [Fact]
    public async Task ClarifyZhazhdaPlayerDescription_UpdatesDefaultCopyWithoutChangingFormula()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString, "20260823100000_EnforceSingleNonterminalGameRound");
            await MigrateAsync(connectionString);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT description,
                       behavior_v2_json ->> 'rule',
                       behavior_v2_json #>> '{formulaReference,parameters,incrementPointsPerKill}',
                       behavior_v2_json #>> '{formulaReference,parameters,zeroKillPenaltyPoints}'
                FROM modifier_definitions
                WHERE id = '10000000-0000-0000-0000-000000000002';
                """;
            await using var reader = await command.ExecuteReaderAsync();

            Assert.True(await reader.ReadAsync());
            Assert.Contains("115 × 3 = 345", reader.GetString(0), StringComparison.Ordinal);
            Assert.Contains("Новая стоимость умножается", reader.GetString(1), StringComparison.Ordinal);
            Assert.Equal("5", reader.GetString(2));
            Assert.Equal("25", reader.GetString(3));
        });
    }

    [Fact]
    public async Task ClarifyZhazhdaPlayerDescription_PreservesAdminEditedCopy()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString, "20260823100000_EnforceSingleNonterminalGameRound");
            await ExecuteAsync(
                connectionString,
                """
                UPDATE modifier_definitions
                SET description = 'Администраторское описание Жажды.',
                    behavior_v2_json = jsonb_set(
                        behavior_v2_json,
                        '{rule}',
                        to_jsonb('Администраторское правило Жажды.'::text),
                        FALSE
                    )
                WHERE id = '10000000-0000-0000-0000-000000000002';
                """
            );

            await MigrateAsync(connectionString);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT description, behavior_v2_json ->> 'rule'
                FROM modifier_definitions
                WHERE id = '10000000-0000-0000-0000-000000000002';
                """;
            await using var reader = await command.ExecuteReaderAsync();

            Assert.True(await reader.ReadAsync());
            Assert.Equal("Администраторское описание Жажды.", reader.GetString(0));
            Assert.Equal("Администраторское правило Жажды.", reader.GetString(1));
        });
    }

    [Theory]
    [InlineData("active_custom", "active custom modifier definitions")]
    [InlineData("limit_mismatch", "activation limit differs")]
    [InlineData("custom_expression", "custom expression requires explicit formula mapping")]
    public async Task BehaviorV2Migration_WhenInventoryIsAmbiguous_FailsClosed(
        string mutation,
        string expectedMessage
    )
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(
                connectionString,
                "20260820151225_AddGameModifierContentLockEmergencyDisable"
            );
            await ExecuteAsync(connectionString, InventoryMutationSql(mutation));

            var exception = await Assert.ThrowsAnyAsync<Exception>(() => MigrateAsync(connectionString));
            Assert.Contains(expectedMessage, exception.ToString(), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task RefundAuditMigration_WhenActivationHasNoDeterministicRound_FailsClosed()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(
                connectionString,
                "20260820140143_AddGameRoundLifecycleVersioning"
            );
            await ExecuteAsync(
                connectionString,
                """
                INSERT INTO users (
                    id, twitch_user_id, login, display_name, is_active, created_at_utc, updated_at_utc
                ) VALUES (
                    '20000000-0000-0000-0000-000000000001', 'migration-user',
                    'migration-user', 'Migration User', TRUE, NOW(), NOW()
                );

                INSERT INTO games (
                    id, title, status, created_at_utc, ready_at_utc, started_at_utc,
                    is_deleted, min_players_per_team, max_players_per_team
                ) VALUES (
                    '30000000-0000-0000-0000-000000000001', 'Migration game', 'active',
                    NOW(), NOW(), NOW(), FALSE, 1, 2
                );

                INSERT INTO game_modifier_activations (
                    id, game_id, modifier_id, activated_by_user_id,
                    activation_cost_snapshot, activated_at_utc
                ) VALUES (
                    '40000000-0000-0000-0000-000000000001',
                    '30000000-0000-0000-0000-000000000001',
                    '10000000-0000-0000-0000-000000000001',
                    '20000000-0000-0000-0000-000000000001',
                    3, NOW()
                );
                """
            );

            var exception = await Assert.ThrowsAnyAsync<Exception>(() => MigrateAsync(connectionString));
            Assert.Contains(
                "cannot be mapped unambiguously to a round",
                exception.ToString(),
                StringComparison.OrdinalIgnoreCase
            );
        });
    }

    private static string InventoryMutationSql(string mutation) => mutation switch
    {
        "active_custom" =>
            """
            UPDATE modifier_definitions
            SET id = '50000000-0000-0000-0000-000000000001'
            WHERE id = '10000000-0000-0000-0000-000000000001';
            """,
        "limit_mismatch" =>
            """
            UPDATE modifier_definitions
            SET metadata_json = jsonb_set(
                COALESCE(metadata_json, '{}'::jsonb),
                '{activationLimit}',
                '{"count":999}'::jsonb,
                TRUE
            )
            WHERE id = '10000000-0000-0000-0000-000000000001';
            """,
        "custom_expression" =>
            """
            UPDATE modifier_definitions
            SET metadata_json = jsonb_set(
                metadata_json,
                '{effect,scoreImpact,scoreFormula}',
                '{"mode":"custom_expression"}'::jsonb,
                TRUE
            )
            WHERE id = '10000000-0000-0000-0000-000000000002';
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null)
    };

    private static async Task MigrateAsync(string connectionString, string? targetMigration = null)
    {
        await using var dbContext = CreateDbContext(connectionString);
        var migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync(targetMigration);
    }

    private static ApplicationDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history")
            )
            .ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>()
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task WithDatabaseAsync(Func<string, Task> test)
    {
        var adminConnectionString = Environment.GetEnvironmentVariable("DEADMANS_TEST_POSTGRES")
            ?? DefaultAdminConnectionString;
        var databaseName = $"deadmans_migration_tests_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        };

        await using var admin = new NpgsqlConnection(adminConnectionString);
        await admin.OpenAsync();
        await using (var create = admin.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            await test(builder.ConnectionString);
        }
        finally
        {
            await using (var terminate = admin.CreateCommand())
            {
                terminate.CommandText =
                    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database_name AND pid <> pg_backend_pid()";
                terminate.Parameters.AddWithValue("database_name", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)}";
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
