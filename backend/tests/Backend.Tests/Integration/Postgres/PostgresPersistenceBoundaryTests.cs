using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Backend.Tests.Integration.Postgres;

public sealed class PostgresPersistenceBoundaryTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PostgresPersistenceBoundaryTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task MigratedSchema_UsesSnakeCaseHistoryAndRestrictiveAuditForeignKeys()
    {
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.OpenAsync();

        await using (var historyColumns = connection.CreateCommand())
        {
            historyColumns.CommandText =
                """
                SELECT count(*)
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = '__ef_migrations_history'
                  AND column_name IN ('migration_id', 'product_version')
                """;
            Assert.Equal(2L, (long)(await historyColumns.ExecuteScalarAsync() ?? 0L));
        }

        await using (var legacyNames = connection.CreateCommand())
        {
            legacyNames.CommandText =
                """
                SELECT count(*)
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND column_name IN ('card_run_id', 'game_active_modifier_id', 'MigrationId', 'ProductVersion')
                """;
            Assert.Equal(0L, (long)(await legacyNames.ExecuteScalarAsync() ?? 0L));
        }

        await using var auditDeleteRules = connection.CreateCommand();
        auditDeleteRules.CommandText =
            """
            SELECT count(*)
            FROM pg_constraint
            WHERE conname IN (
                'fk_game_rounds_users_resolved_by_user_id',
                'fk_game_round_modifier_results_users_resolved_by_user_id',
                'fk_game_teams_users_confirmed_by_user_id',
                'fk_game_teams_users_rejected_by_user_id',
                'fk_game_teams_users_disbanded_by_user_id',
                'fk_game_teams_users_disband_requested_by_user_id'
            )
            AND confdeltype <> 'r'
            """;
        Assert.Equal(0L, (long)(await auditDeleteRules.ExecuteScalarAsync() ?? 0L));
    }

    [Fact]
    public async Task SaveChanges_WhenActiveTeamBelongsToAnotherGame_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var activeGame = CreateGame(GameStatusValue.Active, now);
        var otherGame = CreateGame(GameStatusValue.Ready, now);
        var otherSlot = CreateSlot(otherGame.Id, 1, now);
        var otherTeam = CreateTeam(otherGame.Id, otherSlot.Id, now, TeamStatusValue.Forming);

        db.AddRange(activeGame, otherGame, otherSlot, otherTeam);
        await db.SaveChangesAsync();

        activeGame.ActiveTeamId = otherTeam.Id;

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "fk_games_active_team_same_game");
    }

    [Fact]
    public async Task SaveChanges_WhenTeamStatusTimestampSemanticsAreBroken_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var game = CreateGame(GameStatusValue.Ready, now);
        var slot = CreateSlot(game.Id, 1, now);
        var team = CreateTeam(game.Id, slot.Id, now, TeamStatusValue.Forming);
        team.ConfirmedAtUtc = now;

        db.AddRange(game, slot, team);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "ck_game_teams_status_timestamp_semantics");
    }

    [Fact]
    public async Task SaveChanges_WhenPendingInvitationAlreadyHasResponseTimestamp_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var game = CreateGame(GameStatusValue.Ready, now);
        var slot = CreateSlot(game.Id, 1, now);
        var inviter = CreateUser("inviter");
        var invitee = CreateUser("invitee");
        var invitation = new GameTeamInvitation
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            SlotId = slot.Id,
            InvitedUserId = invitee.Id,
            InvitedByUserId = inviter.Id,
            InvitedByKind = InvitedByKindValue.Admin,
            Status = TeamInvitationStatusValue.Pending,
            CreatedAtUtc = now,
            RespondedAtUtc = now
        };

        db.AddRange(game, slot, inviter, invitee, invitation);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "ck_game_team_invitations_response_timestamp_semantics");
    }

    [Fact]
    public async Task SaveChanges_WhenCompletedRoundHasNoFinalScore_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var seeded = await SeedPlayableRoundGraphAsync(db);
        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = seeded.GameId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.Completed,
            StartedAtUtc = seeded.Now,
            FinishedAtUtc = seeded.Now,
            BaseScore = 100,
            FinalScore = null,
            KillsCount = 1,
            BountyCount = 0,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellCostSnapshot = 100,
            ResolvedByUserId = seeded.UserId,
            CreatedAtUtc = seeded.Now,
            UpdatedAtUtc = seeded.Now
        };

        db.GameRounds.Add(round);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "ck_game_rounds_resolution_semantics");
    }

    [Fact]
    public async Task SaveChanges_WhenEmptyCardPenaltyFlagIsSetBeforeCompletion_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var seeded = await SeedPlayableRoundGraphAsync(db);
        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = seeded.GameId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.InProgress,
            StartedAtUtc = seeded.Now,
            BaseScore = 100,
            EmptyCardPenaltyApplied = true,
            KillsCount = 0,
            BountyCount = 0,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellCostSnapshot = 100,
            CreatedAtUtc = seeded.Now,
            UpdatedAtUtc = seeded.Now
        };

        db.GameRounds.Add(round);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "ck_game_rounds_empty_card_penalty_semantics");
    }

    [Fact]
    public async Task GetActiveRoundAsync_UsesPostgresTranslatableActiveStatusFilter()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var seeded = await SeedPlayableRoundGraphAsync(db);
        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = seeded.GameId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.AwaitingModifiers,
            StartedAtUtc = seeded.Now,
            BaseScore = 100,
            KillsCount = 0,
            BountyCount = 0,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellCostSnapshot = 100,
            CreatedAtUtc = seeded.Now,
            UpdatedAtUtc = seeded.Now
        };

        db.GameRounds.Add(round);
        await db.SaveChangesAsync();

        var repository = new DbGameRoundRepository(db);
        var activeRound = await repository.GetActiveAsync();

        Assert.NotNull(activeRound);
        Assert.Equal(round.Id, activeRound.RoundId);
        Assert.Equal(GameRoundStatusValue.AwaitingModifiers, activeRound.Status);
    }

    [Fact]
    public async Task SaveChanges_WhenMemberLeavesBeforeJoining_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var user = CreateUser("member-time");
        var game = CreateGame(GameStatusValue.Ready, now);
        var slot = CreateSlot(game.Id, 1, now);
        var team = CreateTeam(game.Id, slot.Id, now, TeamStatusValue.Forming);
        var member = new GameTeamMember
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            TeamId = team.Id,
            UserId = user.Id,
            JoinedAtUtc = now,
            LeftAtUtc = now.AddSeconds(-1)
        };

        db.AddRange(user, game, slot, team, member);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "ck_game_team_members_left_after_join");
    }

    [Fact]
    public async Task PersistJoinTeamAsync_WhenTwoUsersJoinLastSlotConcurrently_DoesNotOverfillTeam()
    {
        await _database.ResetAsync();
        await using var seedDb = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var game = CreateGame(GameStatusValue.Ready, now);
        var slot = CreateSlot(game.Id, 1, now);
        var owner = CreateUser("owner");
        var first = CreateUser("join-one");
        var second = CreateUser("join-two");
        var team = CreateTeam(game.Id, slot.Id, now, TeamStatusValue.Forming, recruitmentOpen: true);
        var ownerMember = new GameTeamMember
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            TeamId = team.Id,
            UserId = owner.Id,
            JoinedAtUtc = now
        };

        seedDb.AddRange(game, slot, owner, first, second, team, ownerMember);
        await seedDb.SaveChangesAsync();

        var firstTask = JoinTeamAsync(first.Id);
        var secondTask = JoinTeamAsync(second.Id);
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, results.Count(result => result.Success));
        Assert.Equal(1, results.Count(result => result.Error == GameRegistrationErrorCode.TeamFull));

        await using var verifyDb = _database.CreateDbContext();
        var activeMemberCount = await verifyDb.GameTeamMembers.CountAsync(
            member => member.TeamId == team.Id && member.LeftAtUtc == null
        );
        Assert.Equal(2, activeMemberCount);

        async Task<GameRegistrationResult<RegistrationTeamDto>> JoinTeamAsync(Guid userId)
        {
            await using var db = _database.CreateDbContext();
            var readStore = new GameRegistrationReadStore(db);
            var repository = new DbGameRegistrationPersistence(
                db,
                readStore,
                NullLogger<DbGameRegistrationPersistence>.Instance
            );

            return await repository.PersistJoinTeamAsync(game.Id, userId, team.Id, maxPlayersPerTeam: 2);
        }
    }

    private static async Task<SeededRoundGraph> SeedPlayableRoundGraphAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var user = CreateUser("resolver");
        var game = CreateGame(GameStatusValue.Active, now);
        var board = new GameBoard
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Rows = 1,
            Cols = 1,
            RowLabels = new[] { "A" },
            ColLabels = new[] { "1" },
            Version = 1,
            CreatedAtUtc = now
        };
        var cell = new BoardCell
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            RowIndex = 0,
            ColIndex = 0,
            Title = "Test cell",
            Cost = 100,
            State = BoardCellState.Open,
            CellType = BoardCellPersistence.DefaultCellType
        };
        var slot = CreateSlot(game.Id, 1, now);
        var team = CreateTeam(game.Id, slot.Id, now, TeamStatusValue.Forming);

        db.AddRange(user, game, board, cell, slot, team);
        await db.SaveChangesAsync();

        return new SeededRoundGraph(now, game.Id, cell.Id, team.Id, user.Id);
    }

    private static User CreateUser(string suffix) =>
        new()
        {
            Id = Guid.NewGuid(),
            TwitchUserId = $"tw_{suffix}_{Guid.NewGuid():N}",
            Login = $"login_{suffix}_{Guid.NewGuid():N}"[..32],
            DisplayName = $"User {suffix}",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static Game CreateGame(string status, DateTime now)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = $"Game {Guid.NewGuid():N}",
            Status = status,
            CreatedAtUtc = now,
            MinPlayersPerTeam = 1,
            MaxPlayersPerTeam = 2
        };

        if (status is GameStatusValue.Ready or GameStatusValue.Active or GameStatusValue.Finished)
        {
            game.ReadyAtUtc = now;
        }

        if (status is GameStatusValue.Active or GameStatusValue.Finished)
        {
            game.StartedAtUtc = now;
        }

        if (status == GameStatusValue.Finished)
        {
            game.FinishedAtUtc = now;
        }

        return game;
    }

    private static GameTeamSlot CreateSlot(Guid gameId, int slotIndex, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SlotIndex = slotIndex,
            SlotType = TeamSlotTypeValue.Public,
            CreatedAtUtc = now
        };

    private static GameTeam CreateTeam(
        Guid gameId,
        Guid slotId,
        DateTime now,
        string status,
        bool recruitmentOpen = false
    )
    {
        var team = new GameTeam
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SlotId = slotId,
            RecruitmentOpen = recruitmentOpen,
            Status = status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return team;
    }

    private static void AssertPostgresConstraint(DbUpdateException exception, string constraintName)
    {
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(constraintName, postgres.ConstraintName);
    }

    private sealed record SeededRoundGraph(
        DateTime Now,
        Guid GameId,
        Guid CellId,
        Guid TeamId,
        Guid UserId
    );
}
