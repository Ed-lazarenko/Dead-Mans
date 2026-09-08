using System.Data.Common;
using backend.Application.Contracts;
using backend.Data;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Backend.Tests.Integration.Postgres;

public sealed class ModifierVersionQueryPerformanceTests : IClassFixture<PostgresTestDatabase>
{
    private static readonly Guid ModifierId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly PostgresTestDatabase _database;

    public ModifierVersionQueryPerformanceTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task VersionTimeline_QueryCountIsBoundedAndPlanUsesRevisionIndex()
    {
        await _database.ResetAsync();
        await using (var seedContext = _database.CreateDbContext())
        {
            await TestModifierVersionFactory.AddAsync(
                seedContext,
                new TestModifierSpec(
                    ModifierId,
                    "Performance modifier",
                    "Performance query fixture",
                    "round",
                    1,
                    1,
                    BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior
                )
            );
        }

        await using (var connection = new NpgsqlConnection(_database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var seed = connection.CreateCommand();
            seed.CommandText =
                """
                BEGIN;
                INSERT INTO modifier_definition_versions (
                    id, modifier_id, revision, name, description, category, icon_emoji,
                    activation_command, activation_cost, max_activations_per_round,
                    normalized_tags, behavior_v2_json, created_at_utc,
                    created_by_user_id, created_by_display_name_snapshot, change_note,
                    change_type, changed_fields, cascade_source_modifier_id
                )
                SELECT
                    md5(v.modifier_id::text || ':performance:' || generated.revision::text)::uuid,
                    v.modifier_id, generated.revision, v.name || ' ' || generated.revision::text,
                    v.description, v.category, v.icon_emoji, v.activation_command,
                    v.activation_cost, v.max_activations_per_round, v.normalized_tags,
                    v.behavior_v2_json,
                    v.created_at_utc + generated.revision * INTERVAL '1 second',
                    NULL, 'Performance seed', NULL, 'edited', ARRAY['name']::text[], NULL
                FROM modifier_definition_versions v
                CROSS JOIN generate_series(2, 1001) AS generated(revision)
                WHERE v.modifier_id = @modifier_id AND v.revision = 1
                ON CONFLICT (modifier_id, revision) DO NOTHING;
                UPDATE modifier_definitions
                SET current_version_id = md5(id::text || ':performance:1001')::uuid
                WHERE id = @modifier_id;
                ANALYZE modifier_definition_versions;
                COMMIT;
                """;
            seed.Parameters.AddWithValue("modifier_id", ModifierId);
            await seed.ExecuteNonQueryAsync();
        }

        var counter = new CommandCountingInterceptor();
        await using var db = CreateCountingContext(counter);
        var repository = new DbGameModifierRepository(db);

        var page = await repository.GetVersionsAsync(
            ModifierId,
            new(null, 100),
            CancellationToken.None
        );

        Assert.NotNull(page);
        Assert.Equal(100, page.Items.Count);
        Assert.NotNull(page.NextCursor);
        Assert.InRange(counter.ExecutedCommands, 1, 4);

        counter.Reset();
        var history = await repository.GetHistoryAsync(
            new ModifierHistoryQuery(null, "all", null, 20),
            CancellationToken.None
        );
        Assert.NotEmpty(history.Items);
        Assert.InRange(counter.ExecutedCommands, 1, 2);

        counter.Reset();
        var detail = await repository.GetVersionAsync(
            ModifierId,
            1001,
            CancellationToken.None
        );
        Assert.NotNull(detail);
        Assert.InRange(counter.ExecutedCommands, 1, 2);

        counter.Reset();
        var games = await repository.GetVersionGamesAsync(
            ModifierId,
            1001,
            new(null, 20),
            CancellationToken.None
        );
        Assert.NotNull(games);
        Assert.InRange(counter.ExecutedCommands, 1, 2);

        await using var planConnection = new NpgsqlConnection(_database.ConnectionString);
        await planConnection.OpenAsync();
        await using var planCommand = planConnection.CreateCommand();
        planCommand.CommandText =
            """
            SET enable_seqscan = off;
            EXPLAIN (COSTS OFF)
            SELECT id, modifier_id, revision, name, created_at_utc
            FROM modifier_definition_versions
            WHERE modifier_id = @modifier_id
            ORDER BY revision DESC, id DESC
            LIMIT 100;
            """;
        planCommand.Parameters.AddWithValue("modifier_id", ModifierId);
        var planLines = new List<string>();
        await using (var reader = await planCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                planLines.Add(reader.GetString(0));
            }
        }
        Assert.Contains(
            planLines,
            line => line.Contains(
                "ix_modifier_definition_versions_modifier_id_revision",
                StringComparison.Ordinal
            )
        );

        await using var searchPlanCommand = planConnection.CreateCommand();
        searchPlanCommand.CommandText =
            """
            SET enable_seqscan = off;
            EXPLAIN (COSTS OFF)
            SELECT id
            FROM modifier_definition_versions
            WHERE name ILIKE '%performance%'
            LIMIT 20;
            """;
        var searchPlanLines = new List<string>();
        await using (var reader = await searchPlanCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                searchPlanLines.Add(reader.GetString(0));
            }
        }
        Assert.Contains(searchPlanLines, line => line.Contains(
            "ix_modifier_versions_name_trgm",
            StringComparison.Ordinal));

        await using var historyPlanCommand = planConnection.CreateCommand();
        historyPlanCommand.CommandText =
            """
            SET enable_seqscan = off;
            SET enable_bitmapscan = off;
            SET enable_sort = off;
            EXPLAIN (COSTS OFF)
            SELECT id, created_at_utc
            FROM modifier_definitions
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 20;
            """;
        var historyPlanLines = new List<string>();
        await using (var reader = await historyPlanCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                historyPlanLines.Add(reader.GetString(0));
            }
        }
        Assert.Contains(historyPlanLines, line => line.Contains(
            "ix_modifier_definitions_created_at_utc_id",
            StringComparison.Ordinal));
    }

    private ApplicationDbContext CreateCountingContext(CommandCountingInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                _database.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history")
            )
            .ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>()
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        public int ExecutedCommands { get; private set; }

        public void Reset() => ExecutedCommands = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result
        )
        {
            ExecutedCommands++;
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            ExecutedCommands++;
            return ValueTask.FromResult(result);
        }
    }
}
