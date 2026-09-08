using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Backend.Tests.Integration.Postgres;

public sealed class ProductionBaselineMigrationTests
{
    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=deadmans;Password=deadmans_dev_password;SSL Mode=Disable";

    [Fact]
    public async Task CleanBaseline_SeedsOnlyAccessRolesAndCreatesRequiredExtensions()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);

            await using var db = CreateDbContext(connectionString);
            Assert.Equal(3, await db.Roles.CountAsync());
            Assert.Empty(await db.Users.ToArrayAsync());
            Assert.Empty(await db.Games.ToArrayAsync());
            Assert.Empty(await db.ModifierDefinitions.ToArrayAsync());
            Assert.Empty(await db.QuestionDefinitions.ToArrayAsync());

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT array_agg(extname ORDER BY extname)
                FROM pg_extension
                WHERE extname IN ('citext', 'pg_trgm');
                """;
            var extensions = Assert.IsType<string[]>(await command.ExecuteScalarAsync());
            Assert.Equal(["citext", "pg_trgm"], extensions);

            await using (var downDb = CreateDbContext(connectionString))
            {
                await downDb.GetService<IMigrator>().MigrateAsync(Migration.InitialDatabase);
            }

            command.CommandText = "SELECT to_regclass('public.games') IS NULL";
            Assert.True(Assert.IsType<bool>(await command.ExecuteScalarAsync()));

            await MigrateAsync(connectionString);
            await using var restoredDb = CreateDbContext(connectionString);
            Assert.Equal(3, await restoredDb.Roles.CountAsync());
        });
    }

    [Fact]
    public async Task GameConcurrency_AllowsOneDraftBesideExactlyOneReadyOrActiveGame()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var active = CreateGame(GameStatusValue.Draft, now);
            var draft = CreateGame(GameStatusValue.Draft, now);
            var activeUser = CreateUser(now);

            await using (var db = CreateDbContext(connectionString))
            {
                db.Games.Add(active);
                db.Users.Add(activeUser);
                AddValidSetup(db, active, now, activeUser);
                await db.SaveChangesAsync();

                active.Status = GameStatusValue.Ready;
                active.ReadyAtUtc = now;
                await db.SaveChangesAsync();

                active.Status = GameStatusValue.Active;
                active.StartedAtUtc = now;
                await db.SaveChangesAsync();

                db.Games.Add(draft);
                AddValidSetup(db, draft, now);
                await db.SaveChangesAsync();
            }

            await using (var duplicateDraft = CreateDbContext(connectionString))
            {
                duplicateDraft.Games.Add(CreateGame(GameStatusValue.Draft, now));
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => duplicateDraft.SaveChangesAsync()
                );
                AssertUniqueViolation(exception, "ux_games_single_draft");
            }

            await using (var readyBesideActive = CreateDbContext(connectionString))
            {
                readyBesideActive.Games.Add(CreateGame(GameStatusValue.Ready, now));
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => readyBesideActive.SaveChangesAsync()
                );
                AssertUniqueViolation(exception, "ux_games_single_current");
            }

            await using (var archiveActive = CreateDbContext(connectionString))
            {
                var persistedActive = await archiveActive.Games.SingleAsync(x => x.Id == active.Id);
                persistedActive.IsDeleted = true;
                persistedActive.DeletedAtUtc = now.AddMinutes(1);
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => archiveActive.SaveChangesAsync()
                );
                AssertCheckViolation(exception, "ck_games_archive_finished_only");
            }

            await using (var skipLifecycle = CreateDbContext(connectionString))
            {
                var persistedDraft = await skipLifecycle.Games.SingleAsync(x => x.Id == draft.Id);
                persistedDraft.Status = GameStatusValue.Active;
                persistedDraft.ReadyAtUtc = now;
                persistedDraft.StartedAtUtc = now;
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => skipLifecycle.SaveChangesAsync()
                );
                AssertCheckViolation(exception, "ck_games_lifecycle_transition");
            }

            await using (var finishCurrent = CreateDbContext(connectionString))
            {
                var lifecycle = new DbGameLifecyclePersistence(
                    finishCurrent,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<DbGameLifecyclePersistence>.Instance
                );
                var result = await lifecycle.FinishGameAsync(
                    active.Id,
                    new FinishGameInput(
                        1,
                        Guid.NewGuid(),
                        new HashSet<string>
                        {
                            GameFinishWarningCodes.UnplayedTeams,
                            GameFinishWarningCodes.NoCompletedRounds
                        },
                        null
                    ),
                    activeUser.Id
                );
                Assert.True(result.Success);
            }

            await using (var publishNext = CreateDbContext(connectionString))
            {
                var persistedDraft = await publishNext.Games.SingleAsync(x => x.Id == draft.Id);
                persistedDraft.Status = GameStatusValue.Ready;
                persistedDraft.ReadyAtUtc = now.AddMinutes(1);
                await publishNext.SaveChangesAsync();
            }

            await using (var readyWithNextDraft = CreateDbContext(connectionString))
            {
                readyWithNextDraft.Games.Add(CreateGame(GameStatusValue.Draft, now.AddMinutes(2)));
                await readyWithNextDraft.SaveChangesAsync();
            }

            await using (var activeBesideReady = CreateDbContext(connectionString))
            {
                activeBesideReady.Games.Add(CreateGame(GameStatusValue.Active, now.AddMinutes(2)));
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => activeBesideReady.SaveChangesAsync()
                );
                AssertUniqueViolation(exception, "ux_games_single_current");
            }
        });
    }

    [Fact]
    public async Task Publication_RequiresBoardAndTeamSlot()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var game = CreateGame(GameStatusValue.Draft, now);

            await using var db = CreateDbContext(connectionString);
            db.Games.Add(game);
            await db.SaveChangesAsync();

            game.Status = GameStatusValue.Ready;
            game.ReadyAtUtc = now;
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => db.SaveChangesAsync()
            );
            AssertCheckViolation(exception, "ck_games_publication_setup_complete");
        });
    }

    [Fact]
    public async Task Activation_RequiresASettledConfirmedRoster()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var game = CreateGame(GameStatusValue.Draft, now);

            await using var db = CreateDbContext(connectionString);
            db.Games.Add(game);
            AddValidSetup(db, game, now);
            await db.SaveChangesAsync();

            game.Status = GameStatusValue.Ready;
            game.ReadyAtUtc = now;
            await db.SaveChangesAsync();

            game.Status = GameStatusValue.Active;
            game.StartedAtUtc = now;
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => db.SaveChangesAsync()
            );
            AssertCheckViolation(exception, "ck_games_active_roster_settled");
        });
    }

    [Fact]
    public async Task PendingTeamInvitation_MustFollowItsTeamsCurrentSlot()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var game = CreateGame(GameStatusValue.Draft, now);
            var owner = CreateUser(now);
            var invitee = CreateUser(now);

            await using var db = CreateDbContext(connectionString);
            db.AddRange(game, owner, invitee);
            AddValidSetup(db, game, now);
            await db.SaveChangesAsync();

            var originalSlotId = await db.GameTeamSlots
                .Where(slot => slot.GameId == game.Id)
                .Select(slot => slot.Id)
                .SingleAsync();
            var otherSlotId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            db.GameTeamSlots.Add(
                new GameTeamSlot
                {
                    Id = otherSlotId,
                    GameId = game.Id,
                    SlotIndex = 2,
                    SlotType = TeamSlotTypeValue.Public,
                    CreatedAtUtc = now
                }
            );
            db.GameTeams.Add(
                new GameTeam
                {
                    Id = teamId,
                    GameId = game.Id,
                    SlotId = originalSlotId,
                    Status = TeamStatusValue.Forming,
                    CreatedByUserId = owner.Id,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
            var invitation = new GameTeamInvitation
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                SlotId = originalSlotId,
                TeamId = teamId,
                InvitedUserId = invitee.Id,
                InvitedByUserId = owner.Id,
                InvitedByKind = InvitedByKindValue.Member,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = now
            };
            db.GameTeamInvitations.Add(invitation);
            await db.SaveChangesAsync();

            invitation.SlotId = otherSlotId;
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => db.SaveChangesAsync()
            );
            AssertCheckViolation(
                exception,
                "ck_game_team_invitations_pending_team_slot"
            );
        });
    }

    [Fact]
    public async Task Finish_RequiresAnAtomicFinalizationSnapshot()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var game = CreateGame(GameStatusValue.Draft, now);
            var user = CreateUser(now);

            await using (var seedDb = CreateDbContext(connectionString))
            {
                seedDb.AddRange(game, user);
                AddValidSetup(seedDb, game, now, user);
                await seedDb.SaveChangesAsync();

                game.Status = GameStatusValue.Ready;
                game.ReadyAtUtc = now;
                await seedDb.SaveChangesAsync();

                game.Status = GameStatusValue.Active;
                game.StartedAtUtc = now;
                await seedDb.SaveChangesAsync();
            }

            await using var invalidFinishDb = CreateDbContext(connectionString);
            var persisted = await invalidFinishDb.Games.SingleAsync(x => x.Id == game.Id);
            persisted.Status = GameStatusValue.Finished;
            persisted.FinishedAtUtc = now.AddSeconds(1);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => invalidFinishDb.SaveChangesAsync()
            );
            AssertCheckViolation(exception, "ck_games_finalization_required");
        });
    }

    [Fact]
    public async Task PublishedGame_FreezesConfigurationAndCannotBeHardDeleted()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var ready = CreateGame(GameStatusValue.Draft, now);

            await using (var seedDb = CreateDbContext(connectionString))
            {
                seedDb.Games.Add(ready);
                AddValidSetup(seedDb, ready, now);
                await seedDb.SaveChangesAsync();

                ready.Status = GameStatusValue.Ready;
                ready.ReadyAtUtc = now;
                await seedDb.SaveChangesAsync();
            }

            await using (var mutateDb = CreateDbContext(connectionString))
            {
                var board = await mutateDb.GameBoards.SingleAsync(x => x.GameId == ready.Id);
                board.RowLabels = ["Renamed"];
                var exception = await Assert.ThrowsAnyAsync<Exception>(
                    () => mutateDb.SaveChangesAsync()
                );
                AssertSqlState(exception, "55000");
            }

            await using (var deleteReadyDb = CreateDbContext(connectionString))
            {
                var persisted = await deleteReadyDb.Games.SingleAsync(x => x.Id == ready.Id);
                deleteReadyDb.Games.Remove(persisted);
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => deleteReadyDb.SaveChangesAsync()
                );
                AssertCheckViolation(exception, "ck_games_hard_delete_draft_only");
            }

            await using (var deleteDraftDb = CreateDbContext(connectionString))
            {
                var draft = CreateGame(GameStatusValue.Draft, now.AddSeconds(1));
                deleteDraftDb.Games.Add(draft);
                await deleteDraftDb.SaveChangesAsync();
                deleteDraftDb.Games.Remove(draft);
                await deleteDraftDb.SaveChangesAsync();
                Assert.False(await deleteDraftDb.Games.AnyAsync(x => x.Id == draft.Id));
            }
        });
    }

    [Fact]
    public async Task QuestionCatalog_RequiresExactlyOnePrimaryAcceptedAnswer()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var category = new QuestionCategory
            {
                Id = Guid.NewGuid(),
                Name = "История",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            var question = CreateQuestion(category.Id, now);
            question.AcceptedAnswers.Add(
                new QuestionAcceptedAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    AnswerText = "Варшава",
                    NormalizedAnswer = "варшава",
                    IsPrimary = true,
                    SortOrder = 0,
                    CreatedAtUtc = now
                }
            );
            await using (var validDb = CreateDbContext(connectionString))
            {
                validDb.QuestionCategories.Add(category);
                validDb.QuestionDefinitions.Add(question);
                await validDb.SaveChangesAsync();
            }

            await using var invalidDb = CreateDbContext(connectionString);
            var onlyAnswer = await invalidDb.QuestionAcceptedAnswers.SingleAsync(
                answer => answer.QuestionId == question.Id
            );
            invalidDb.QuestionAcceptedAnswers.Remove(onlyAnswer);
            var exception = await Assert.ThrowsAnyAsync<Exception>(
                () => invalidDb.SaveChangesAsync()
            );
            AssertCheckViolation(
                exception,
                "ck_question_accepted_answers_complete_set"
            );
        });
    }

    [Fact]
    public async Task PointLedger_EnforcesRunningBalanceAndImmutability()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var game = CreateGame(GameStatusValue.Draft, now);
            var user = CreateUser(now);
            var firstEntryId = Guid.NewGuid();

            await using (var seedDb = CreateDbContext(connectionString))
            {
                seedDb.AddRange(game, user);
                AddValidSetup(seedDb, game, now, user);
                await seedDb.SaveChangesAsync();

                game.Status = GameStatusValue.Ready;
                game.ReadyAtUtc = now;
                await seedDb.SaveChangesAsync();

                game.Status = GameStatusValue.Active;
                game.StartedAtUtc = now;
                await seedDb.SaveChangesAsync();

                seedDb.GameQuizPointLedgerEntries.Add(
                    new GameQuizPointLedgerEntry
                    {
                        Id = firstEntryId,
                        GameId = game.Id,
                        UserId = user.Id,
                        EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                        PointsDelta = 10,
                        ManualRequestId = Guid.NewGuid(),
                        CreatedByUserId = user.Id,
                        Reason = "Initial test credit",
                        AvailablePointsBefore = 0,
                        AvailablePointsAfter = 10,
                        OccurredAtUtc = now
                    }
                );
                await seedDb.SaveChangesAsync();
            }

            await using (var invalidDb = CreateDbContext(connectionString))
            {
                invalidDb.GameQuizPointLedgerEntries.Add(
                    new GameQuizPointLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        GameId = game.Id,
                        UserId = user.Id,
                        EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                        PointsDelta = 5,
                        ManualRequestId = Guid.NewGuid(),
                        CreatedByUserId = user.Id,
                        Reason = "Invalid running balance",
                        AvailablePointsBefore = 0,
                        AvailablePointsAfter = 5,
                        OccurredAtUtc = now.AddSeconds(1)
                    }
                );
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => invalidDb.SaveChangesAsync()
                );
                AssertCheckViolation(exception, "ck_quiz_point_ledger_running_balance");
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var update = connection.CreateCommand();
            update.CommandText =
                "UPDATE game_quiz_point_ledger_entries SET points_delta = 9 WHERE id = @id";
            update.Parameters.AddWithValue("id", firstEntryId);
            var immutable = await Assert.ThrowsAsync<PostgresException>(
                () => update.ExecuteNonQueryAsync()
            );
            Assert.Equal("55000", immutable.SqlState);
        });
    }

    [Fact]
    public async Task BoardCells_MustFormACompleteGridInsideDeclaredDimensions()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await MigrateAsync(connectionString);
            var now = DateTime.UtcNow;
            var gameId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                BEGIN;
                INSERT INTO games (
                    id, title, status, created_at_utc, is_deleted,
                    min_players_per_team, max_players_per_team, quiz_answer_duration_seconds
                ) VALUES (@game_id, 'Bounds test', 'draft', @now, FALSE, 1, 2, 60);
                INSERT INTO game_boards (
                    id, game_id, version, rows, cols, row_labels, col_labels, created_at_utc
                ) VALUES (@board_id, @game_id, 1, 1, 1, ARRAY['A'], ARRAY['1'], @now);
                INSERT INTO game_board_cells (
                    id, board_id, row_index, col_index, state, cell_type, cost
                ) VALUES (gen_random_uuid(), @board_id, 1, 0, 'closed', 'default', 0);
                COMMIT;
                """;
            command.Parameters.AddWithValue("game_id", gameId);
            command.Parameters.AddWithValue("board_id", boardId);
            command.Parameters.AddWithValue("now", now);
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => command.ExecuteNonQueryAsync()
            );
            AssertCheckViolation(exception, "ck_game_board_cells_within_board_bounds");

            await using (var rollback = connection.CreateCommand())
            {
                rollback.CommandText = "ROLLBACK";
                await rollback.ExecuteNonQueryAsync();
            }

            command.Parameters.Clear();
            command.CommandText =
                """
                BEGIN;
                INSERT INTO games (
                    id, title, status, created_at_utc, is_deleted,
                    min_players_per_team, max_players_per_team, quiz_answer_duration_seconds
                ) VALUES (@game_id, 'Completeness test', 'draft', @now, FALSE, 1, 2, 60);
                INSERT INTO game_boards (
                    id, game_id, version, rows, cols, row_labels, col_labels, created_at_utc
                ) VALUES (@board_id, @game_id, 1, 1, 2, ARRAY['A'], ARRAY['1','2'], @now);
                INSERT INTO game_board_cells (
                    id, board_id, row_index, col_index, state, cell_type, cost
                ) VALUES (gen_random_uuid(), @board_id, 0, 0, 'closed', 'default', 0);
                COMMIT;
                """;
            command.Parameters.AddWithValue("game_id", Guid.NewGuid());
            command.Parameters.AddWithValue("board_id", Guid.NewGuid());
            command.Parameters.AddWithValue("now", now);
            exception = await Assert.ThrowsAsync<PostgresException>(
                () => command.ExecuteNonQueryAsync()
            );
            AssertCheckViolation(exception, "ck_game_board_cells_complete_grid");
        });
    }

    private static Game CreateGame(string status, DateTime now)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = $"Game {Guid.NewGuid():N}",
            Status = status,
            CreatedAtUtc = now,
            QuizAnswerDurationSeconds = 60,
            MinPlayersPerTeam = 1,
            MaxPlayersPerTeam = 2
        };
        if (status is GameStatusValue.Ready or GameStatusValue.Active)
        {
            game.ReadyAtUtc = now;
        }
        if (status == GameStatusValue.Active)
        {
            game.StartedAtUtc = now;
        }
        return game;
    }

    private static User CreateUser(DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        TwitchUserId = Guid.NewGuid().ToString("N"),
        Login = $"viewer_{Guid.NewGuid():N}"[..32],
        DisplayName = "Quiz viewer",
        IsActive = true,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static void AddValidSetup(
        ApplicationDbContext db,
        Game game,
        DateTime now,
        User? confirmedMember = null
    )
    {
        var boardId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        db.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = game.Id,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                Version = 1,
                CreatedAtUtc = now
            }
        );
        db.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                State = BoardCellState.Closed,
                CellType = BoardCellPersistence.DefaultCellType,
                Cost = 0
            }
        );
        db.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = game.Id,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = now
            }
        );

        if (confirmedMember is null)
        {
            return;
        }

        var teamId = Guid.NewGuid();
        db.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = game.Id,
                SlotId = slotId,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = confirmedMember.Id,
                ConfirmedByUserId = confirmedMember.Id,
                ConfirmedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        db.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                TeamId = teamId,
                UserId = confirmedMember.Id,
                JoinedAtUtc = now
            }
        );
    }

    private static QuestionDefinition CreateQuestion(Guid categoryId, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ExternalCode = $"q-{Guid.NewGuid():N}",
        CategoryId = categoryId,
        Text = "Столица Польши?",
        Reward = 10,
        Revision = 1,
        IsEnabled = true,
        Priority = 0,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static void AssertUniqueViolation(DbUpdateException exception, string constraint)
    {
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal(constraint, postgres.ConstraintName);
    }

    private static void AssertCheckViolation(Exception exception, string constraint)
    {
        var postgres = exception switch
        {
            PostgresException direct => direct,
            DbUpdateException { InnerException: PostgresException inner } => inner,
            _ => throw new Xunit.Sdk.XunitException(
                $"Expected PostgreSQL check violation, received {exception.GetType().Name}."
            )
        };
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal(constraint, postgres.ConstraintName);
    }

    private static void AssertSqlState(Exception exception, string sqlState)
    {
        var current = exception;
        while (current is not PostgresException && current.InnerException is not null)
        {
            current = current.InnerException;
        }
        var postgres = current as PostgresException
            ?? throw new Xunit.Sdk.XunitException(
                $"Expected PostgreSQL error, received {exception.GetType().Name}."
            );
        Assert.Equal(sqlState, postgres.SqlState);
    }

    private static async Task MigrateAsync(string connectionString)
    {
        await using var dbContext = CreateDbContext(connectionString);
        await dbContext.GetService<IMigrator>().MigrateAsync();
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

    private static async Task WithDatabaseAsync(Func<string, Task> test)
    {
        var adminConnectionString = Environment.GetEnvironmentVariable("DEADMANS_TEST_POSTGRES")
            ?? DefaultAdminConnectionString;
        var databaseName = $"deadmans_baseline_tests_{Guid.NewGuid():N}";
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
                    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity "
                    + "WHERE datname = @database_name AND pid <> pg_backend_pid()";
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
