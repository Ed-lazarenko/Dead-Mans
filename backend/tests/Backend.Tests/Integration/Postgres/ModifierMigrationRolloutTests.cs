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
    public async Task ImmutableRevisionMigration_CreatesStableBaselinesPinsActiveGamesAndEnforcesBoundaries()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString, "20260906000814_AddGameFinalizationSnapshots");
            await ExecuteAsync(
                connectionString,
                """
                INSERT INTO games (
                    id, title, status, created_at_utc, ready_at_utc, started_at_utc,
                    is_deleted, min_players_per_team, max_players_per_team
                ) VALUES
                (
                    '31000000-0000-0000-0000-000000000001', 'Already active', 'active',
                    NOW() - INTERVAL '2 hours', NOW() - INTERVAL '90 minutes',
                    NOW() - INTERVAL '1 hour', FALSE, 1, 2
                ),
                (
                    '31000000-0000-0000-0000-000000000002', 'Still ready', 'ready',
                    NOW() - INTERVAL '2 hours', NOW() - INTERVAL '90 minutes',
                    NULL, FALSE, 1, 2
                );

                INSERT INTO game_enabled_modifiers (game_id, modifier_id, enabled_at_utc)
                VALUES
                ('31000000-0000-0000-0000-000000000001',
                 '10000000-0000-0000-0000-000000000001', NOW() - INTERVAL '90 minutes'),
                ('31000000-0000-0000-0000-000000000002',
                 '10000000-0000-0000-0000-000000000001', NOW() - INTERVAL '90 minutes');
                """
            );

            await MigrateAsync(connectionString);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var baseline = connection.CreateCommand())
            {
                baseline.CommandText =
                    """
                    SELECT
                        COUNT(*) = COUNT(current_version_id),
                        COUNT(*) = (SELECT COUNT(*) FROM modifier_definition_versions),
                        bool_and(v.change_type = 'migration_baseline'),
                        bool_and(v.changed_fields = ARRAY['created']::text[]),
                        bool_and(v.revision >= 1),
                        bool_and(v.id = md5(d.id::text || ':baseline:' || v.revision::text)::uuid)
                    FROM modifier_definitions d
                    JOIN modifier_definition_versions v ON v.id = d.current_version_id;
                    """;
                await using var reader = await baseline.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.True(reader.GetBoolean(0));
                Assert.True(reader.GetBoolean(1));
                Assert.True(reader.GetBoolean(2));
                Assert.True(reader.GetBoolean(3));
                Assert.True(reader.GetBoolean(4));
                Assert.True(reader.GetBoolean(5));
            }

            await using (var pinned = connection.CreateCommand())
            {
                pinned.CommandText =
                    """
                    SELECT game_id, modifier_version_id IS NOT NULL, version_pinned_at_utc IS NOT NULL
                    FROM game_enabled_modifiers
                    ORDER BY game_id;
                    """;
                await using var reader = await pinned.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal(Guid.Parse("31000000-0000-0000-0000-000000000001"), reader.GetGuid(0));
                Assert.True(reader.GetBoolean(1));
                Assert.True(reader.GetBoolean(2));
                Assert.True(await reader.ReadAsync());
                Assert.Equal(Guid.Parse("31000000-0000-0000-0000-000000000002"), reader.GetGuid(0));
                Assert.False(reader.GetBoolean(1));
                Assert.False(reader.GetBoolean(2));
            }

            await using (var immutable = connection.CreateCommand())
            {
                immutable.CommandText =
                    "UPDATE modifier_definition_versions SET name = 'forbidden' WHERE revision = 1";
                var exception = await Assert.ThrowsAsync<PostgresException>(
                    () => immutable.ExecuteNonQueryAsync()
                );
                Assert.Equal("55000", exception.SqlState);
            }

            await using (var foreignBinding = connection.CreateCommand())
            {
                foreignBinding.CommandText =
                    """
                    UPDATE game_enabled_modifiers
                    SET modifier_version_id = (
                        SELECT current_version_id FROM modifier_definitions
                        WHERE id = '10000000-0000-0000-0000-000000000002'
                    )
                    WHERE game_id = '31000000-0000-0000-0000-000000000001';
                    """;
                var exception = await Assert.ThrowsAsync<PostgresException>(
                    () => foreignBinding.ExecuteNonQueryAsync()
                );
                Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
            }

            await using var indexes = connection.CreateCommand();
            indexes.CommandText =
                """
                SELECT indexname FROM pg_indexes
                WHERE schemaname = 'public' AND indexname IN (
                    'ix_modifier_definition_versions_modifier_id_revision',
                    'ix_modifier_definitions_current_version_id',
                    'ix_modifier_definitions_created_at_utc_id',
                    'ix_modifier_definitions_is_archived_created_at_utc_id',
                    'ix_game_enabled_modifiers_modifier_version_id_game_id',
                    'ix_game_modifier_activations_version_game',
                    'ix_modifier_versions_name_trgm',
                    'ix_modifier_versions_category_trgm'
                );
                """;
            var foundIndexes = new HashSet<string>(StringComparer.Ordinal);
            await using (var reader = await indexes.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    foundIndexes.Add(reader.GetString(0));
                }
            }
            Assert.Equal(8, foundIndexes.Count);
        });
    }

    [Fact]
    public async Task ImmutableRevisionMigration_DownRestoresCurrentProjectionOnly()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            await ExecuteAsync(
                connectionString,
                """
                INSERT INTO modifier_definition_versions (
                    id, modifier_id, revision, name, description, category, icon_emoji,
                    activation_command, activation_cost, max_activations_per_round,
                    normalized_tags, behavior_v2_json, created_at_utc,
                    created_by_user_id, created_by_display_name_snapshot, change_note,
                    change_type, changed_fields, cascade_source_modifier_id
                )
                SELECT
                    '41000000-0000-0000-0000-000000000001', modifier_id, revision + 1,
                    'Current before down', description, category, icon_emoji,
                    activation_command, 77, max_activations_per_round,
                    normalized_tags, behavior_v2_json, NOW(), NULL, 'Migration test',
                    'down projection', 'edited', ARRAY['activationCost']::text[], NULL
                FROM modifier_definition_versions
                WHERE modifier_id = '10000000-0000-0000-0000-000000000001'
                  AND revision = 1;

                UPDATE modifier_definitions
                SET current_version_id = '41000000-0000-0000-0000-000000000001'
                WHERE id = '10000000-0000-0000-0000-000000000001';
                """
            );

            await MigrateAsync(connectionString, "20260906000814_AddGameFinalizationSnapshots");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT name, revision, activation_cost,
                       to_regclass('public.modifier_definition_versions') IS NULL
                FROM modifier_definitions
                WHERE id = '10000000-0000-0000-0000-000000000001';
                """;
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Current before down", reader.GetString(0));
            Assert.Equal(2, reader.GetInt32(1));
            Assert.Equal(77, reader.GetInt32(2));
            Assert.True(reader.GetBoolean(3));
        });
    }

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
                SELECT v.name, v.description, v.activation_cost,
                       v.behavior_v2_json ->> 'schemaVersion'
                FROM modifier_definitions d
                JOIN modifier_definition_versions v
                  ON v.id = d.current_version_id AND v.modifier_id = d.id
                WHERE d.id = '10000000-0000-0000-0000-000000000002';
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
    public async Task LatestMigrations_UpdateDefaultZhazhdaCopyAndGeneralizeItsFormula()
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
                SELECT v.description,
                       v.behavior_v2_json ->> 'rule',
                       v.behavior_v2_json #>> '{formulaReference,parameters,incrementPointsPerUnit}',
                       v.behavior_v2_json #>> '{formulaReference,parameters,zeroCountPenaltyPoints}'
                FROM modifier_definitions d
                JOIN modifier_definition_versions v
                  ON v.id = d.current_version_id AND v.modifier_id = d.id
                WHERE d.id = '10000000-0000-0000-0000-000000000002';
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
    public async Task GeneralizeScoringMigration_PreservesCustomizedFormulaParameters()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString, "20260824193939_AddManualQuizPointAdjustments");
            await ExecuteAsync(
                connectionString,
                """
                UPDATE modifier_definitions
                SET behavior_v2_json = jsonb_set(
                    behavior_v2_json,
                    '{formulaReference,parameters,incrementPointsPerKill}',
                    '7'::jsonb
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
                SELECT v.behavior_v2_json #>> '{formulaReference,code}',
                       v.behavior_v2_json #>> '{formulaReference,parameters,incrementPointsPerKill}'
                FROM modifier_definitions d
                JOIN modifier_definition_versions v
                  ON v.id = d.current_version_id AND v.modifier_id = d.id
                WHERE d.id = '10000000-0000-0000-0000-000000000002';
                """;
            await using var reader = await command.ExecuteReaderAsync();

            Assert.True(await reader.ReadAsync());
            Assert.Equal("growing_kill_value", reader.GetString(0));
            Assert.Equal("7", reader.GetString(1));
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
                SELECT v.description, v.behavior_v2_json ->> 'rule'
                FROM modifier_definitions d
                JOIN modifier_definition_versions v
                  ON v.id = d.current_version_id AND v.modifier_id = d.id
                WHERE d.id = '10000000-0000-0000-0000-000000000002';
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
