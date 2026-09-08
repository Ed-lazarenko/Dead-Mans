using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.Persistence;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Backend.Tests.Integration.Postgres;

public sealed class PostgresPersistenceBoundaryTests : IClassFixture<PostgresTestDatabase>
{
    private static readonly string RuleBehaviorJson = ModifierBehaviorV2Json.Serialize(
        BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior
    );

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
                'fk_game_modifier_activations_game_rounds_round_id',
                'fk_game_modifier_activations_users_cancelled_by_user_id',
                'fk_game_modifier_activations_users_initiated_by_user_id',
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
    public async Task SaveChanges_WhenActivationRefundExceedsFrozenCost_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var definitionId = Guid.NewGuid();
        var seeded = await SeedPlayableRoundGraphAsync(
            db,
            async (draftDb, fixtureNow, draftGameId, _) =>
            {
                var version = await TestModifierVersionFactory.AddAsync(
                    draftDb,
                    new TestModifierSpec(
                        definitionId,
                        "Refund boundary",
                        "Constraint test",
                        GameModifierCategories.Round,
                        5,
                        null,
                        BuiltInModifierBehaviorCatalog.Get(
                            BuiltInModifierBehaviorCatalog.Chirik
                        ).Behavior
                    ),
                    fixtureNow
                );
                draftDb.GameEnabledModifiers.Add(
                    new GameEnabledModifier
                    {
                        GameId = draftGameId,
                        ModifierId = definitionId,
                        ModifierVersionId = version.Id,
                        VersionPinnedAtUtc = fixtureNow,
                        EnabledAtUtc = fixtureNow
                    }
                );
                await draftDb.SaveChangesAsync();
            }
        );
        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = seeded.GameId,
            BoardId = seeded.BoardId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.AwaitingModifiers,
            BaseScore = 100,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellTitleSnapshot = "Test cell",
            CellCostSnapshot = 100,
            CreatedAtUtc = seeded.Now,
            UpdatedAtUtc = seeded.Now
        };
        db.Add(round);
        var activation = new backend.Data.Entities.GameModifierActivation
        {
            Id = Guid.NewGuid(),
            GameId = seeded.GameId,
            RoundId = round.Id,
            ModifierId = definitionId,
            ActivatedByUserId = seeded.UserId,
            InitiatedByUserId = seeded.UserId,
            ActivationCostSnapshot = 5,
            DefinitionRevisionSnapshot = 1,
            ModifierNameSnapshot = "Refund boundary",
            ModifierDescriptionSnapshot = "Constraint test",
            ModifierCategorySnapshot = GameModifierCategories.Round,
            NormalizedTagsSnapshot = ["test"],
            BehaviorV2SnapshotJson = RuleBehaviorJson,
            ActivatedAtUtc = seeded.Now,
            Status = GameModifierActivationStatusValue.Cancelled,
            ArchivedAtUtc = seeded.Now,
            CancelledByUserId = seeded.UserId,
            CancelledAtUtc = seeded.Now,
            RefundAmount = 6
        };

        db.Add(activation);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Contains(
            postgres.ConstraintName,
            new[]
            {
                "ck_game_modifier_activations_refund_range",
                "ck_game_modifier_activations_lifecycle_semantics"
            }
        );
    }

    [Fact]
    public async Task SaveChanges_WhenActiveTeamBelongsToAnotherGame_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var activeGame = CreateGame(GameStatusValue.Draft, now);
        var otherGame = CreateGame(GameStatusValue.Draft, now);
        var activeUser = CreateUser("active-owner");
        var activeSlot = CreateSlot(activeGame.Id, 1, now);
        var activeTeam = CreateTeam(activeGame.Id, activeSlot.Id, now, TeamStatusValue.Confirmed);
        activeTeam.CreatedByUserId = activeUser.Id;
        activeTeam.ConfirmedByUserId = activeUser.Id;
        activeTeam.ConfirmedAtUtc = now;
        var activeMember = new GameTeamMember
        {
            Id = Guid.NewGuid(),
            GameId = activeGame.Id,
            TeamId = activeTeam.Id,
            UserId = activeUser.Id,
            JoinedAtUtc = now
        };
        var otherSlot = CreateSlot(otherGame.Id, 1, now);
        var otherTeam = CreateTeam(otherGame.Id, otherSlot.Id, now, TeamStatusValue.Forming);

        AddDraftBoard(db, activeGame.Id, now);
        db.AddRange(activeGame, activeUser, activeSlot, activeTeam, activeMember);
        await db.SaveChangesAsync();

        activeGame.Status = GameStatusValue.Ready;
        activeGame.ReadyAtUtc = now;
        await db.SaveChangesAsync();

        activeGame.Status = GameStatusValue.Active;
        activeGame.StartedAtUtc = now;
        await db.SaveChangesAsync();

        db.AddRange(otherGame, otherSlot, otherTeam);
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
        var game = CreateGame(GameStatusValue.Draft, now);
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
        var game = CreateGame(GameStatusValue.Draft, now);
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
            BoardId = seeded.BoardId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.Completed,
            PreparedAtUtc = seeded.Now,
            GameplayStartedAtUtc = seeded.Now,
            ReviewedAtUtc = seeded.Now,
            FinishedAtUtc = seeded.Now,
            BaseScore = 100,
            FinalScore = null,
            KillsCount = 1,
            BountyCount = 0,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellTitleSnapshot = "Test cell",
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
    public async Task SaveChanges_WhenGameHasTwoNonterminalRounds_FailsAtDatabaseBoundary()
    {
        await _database.ResetAsync();
        await using var db = _database.CreateDbContext();
        var seeded = await SeedPlayableRoundGraphAsync(db);

        GameRound CreateRound(Guid cellId, int colIndex) => new()
        {
            Id = Guid.NewGuid(),
            GameId = seeded.GameId,
            BoardId = seeded.BoardId,
            BoardCellId = cellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.AwaitingModifiers,
            BaseScore = 100,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = colIndex,
            CellTitleSnapshot = colIndex == 0 ? "Test cell" : "Second test cell",
            CellCostSnapshot = 100,
            CreatedAtUtc = seeded.Now,
            UpdatedAtUtc = seeded.Now
        };

        db.GameRounds.AddRange(
            CreateRound(seeded.CellId, 0),
            CreateRound(seeded.SecondCellId, 1)
        );

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        AssertPostgresConstraint(ex, "ux_game_rounds_single_nonterminal_game");
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
            BoardId = seeded.BoardId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.InProgress,
            BaseScore = 100,
            EmptyCardPenaltyApplied = true,
            KillsCount = 0,
            BountyCount = 0,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellTitleSnapshot = "Test cell",
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
            BoardId = seeded.BoardId,
            BoardCellId = seeded.CellId,
            TeamId = seeded.TeamId,
            Status = GameRoundStatusValue.AwaitingModifiers,
            BaseScore = 100,
            KillsCount = 0,
            BountyCount = 0,
            TeamSlotIndexSnapshot = 1,
            CellRowIndex = 0,
            CellColIndex = 0,
            CellTitleSnapshot = "Test cell",
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
        var game = CreateGame(GameStatusValue.Draft, now);
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
        var game = CreateGame(GameStatusValue.Draft, now);
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

        AddDraftBoard(seedDb, game.Id, now);
        seedDb.AddRange(game, slot, owner, first, second, team, ownerMember);
        await seedDb.SaveChangesAsync();

        game.Status = GameStatusValue.Ready;
        game.ReadyAtUtc = now;
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
                NullLogger<DbGameRegistrationPersistence>.Instance,
                TimeProvider.System
            );

            return await repository.PersistJoinTeamAsync(game.Id, userId, team.Id, maxPlayersPerTeam: 2);
        }
    }

    [Fact]
    public async Task PersistAcceptInvitationAsync_WhenTwoInviteesAcceptLastSlotConcurrently_DoesNotOverfillTeam()
    {
        await _database.ResetAsync();
        await using var seedDb = _database.CreateDbContext();
        var now = DateTime.UtcNow;
        var game = CreateGame(GameStatusValue.Draft, now);
        var slot = CreateSlot(game.Id, 1, now);
        var owner = CreateUser("invite-owner");
        var first = CreateUser("invite-one");
        var second = CreateUser("invite-two");
        var team = CreateTeam(game.Id, slot.Id, now, TeamStatusValue.Forming);
        var ownerMember = new GameTeamMember
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            TeamId = team.Id,
            UserId = owner.Id,
            JoinedAtUtc = now
        };
        var firstInvitation = CreateInvitation(first.Id);
        var secondInvitation = CreateInvitation(second.Id);

        AddDraftBoard(seedDb, game.Id, now);
        seedDb.AddRange(
            game,
            slot,
            owner,
            first,
            second,
            team,
            ownerMember,
            firstInvitation,
            secondInvitation
        );
        await seedDb.SaveChangesAsync();

        game.Status = GameStatusValue.Ready;
        game.ReadyAtUtc = now;
        await seedDb.SaveChangesAsync();

        var firstTask = AcceptInvitationAsync(firstInvitation.Id, first.Id);
        var secondTask = AcceptInvitationAsync(secondInvitation.Id, second.Id);
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, results.Count(result => result.Success));
        Assert.Equal(1, results.Count(result => result.Error == GameRegistrationErrorCode.TeamFull));

        await using var verifyDb = _database.CreateDbContext();
        Assert.Equal(
            2,
            await verifyDb.GameTeamMembers.CountAsync(
                member => member.TeamId == team.Id && member.LeftAtUtc == null
            )
        );
        Assert.Equal(
            1,
            await verifyDb.GameTeamInvitations.CountAsync(
                invitation => invitation.Status == TeamInvitationStatusValue.Accepted
            )
        );

        GameTeamInvitation CreateInvitation(Guid invitedUserId) =>
            new()
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                SlotId = slot.Id,
                TeamId = team.Id,
                InvitedUserId = invitedUserId,
                InvitedByUserId = owner.Id,
                InvitedByKind = InvitedByKindValue.Admin,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = now
            };

        async Task<GameRegistrationResult<RegistrationTeamDto>> AcceptInvitationAsync(
            Guid invitationId,
            Guid userId
        )
        {
            await using var db = _database.CreateDbContext();
            var readStore = new GameRegistrationReadStore(db);
            var repository = new DbGameRegistrationPersistence(
                db,
                readStore,
                NullLogger<DbGameRegistrationPersistence>.Instance,
                TimeProvider.System
            );

            return await repository.PersistAcceptInvitationAsync(
                new AcceptInvitationCommand(
                    invitationId,
                    userId,
                    game.Id,
                    slot.Id,
                    team.Id,
                    MaxPlayersPerTeam: 2
                )
            );
        }
    }

    [Fact]
    public async Task ActivateAndPrepare_WhenConcurrent_SerializeWithoutLatePurchase()
    {
        await _database.ResetAsync();
        Guid roundId;
        var modifierId = Guid.NewGuid();
        Guid userId;
        await using (var seedDb = _database.CreateDbContext())
        {
            var seeded = await SeedPlayableRoundGraphAsync(
                seedDb,
                async (draftDb, fixtureNow, draftGameId, _) =>
                {
                    var modifierVersion = await TestModifierVersionFactory.AddAsync(
                        draftDb,
                        new TestModifierSpec(
                            modifierId,
                            "Concurrent modifier",
                            "Concurrency boundary fixture",
                            GameModifierCategories.Round,
                            1,
                            3,
                            BuiltInModifierBehaviorCatalog.Get(
                                BuiltInModifierBehaviorCatalog.Chirik
                            ).Behavior
                        ),
                        fixtureNow
                    );
                    draftDb.GameEnabledModifiers.Add(
                        new GameEnabledModifier
                        {
                            GameId = draftGameId,
                            ModifierId = modifierId,
                            ModifierVersionId = modifierVersion.Id,
                            VersionPinnedAtUtc = fixtureNow,
                            EnabledAtUtc = fixtureNow
                        }
                    );
                    await draftDb.SaveChangesAsync();
                }
            );
            roundId = Guid.NewGuid();
            userId = seeded.UserId;
            seedDb.GameRounds.Add(
                new GameRound
                {
                    Id = roundId,
                    GameId = seeded.GameId,
                    BoardId = seeded.BoardId,
                    BoardCellId = seeded.CellId,
                    TeamId = seeded.TeamId,
                    Status = GameRoundStatusValue.AwaitingModifiers,
                    Version = 1,
                    BaseScore = 100,
                    TeamSlotIndexSnapshot = 1,
                    CellRowIndex = 0,
                    CellColIndex = 0,
                    CellTitleSnapshot = "Test cell",
                    CellCostSnapshot = 100,
                    CreatedAtUtc = seeded.Now,
                    UpdatedAtUtc = seeded.Now
                }
            );
            seedDb.GameQuizPointLedgerEntries.Add(
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = seeded.GameId,
                    UserId = userId,
                    EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                    PointsDelta = 10,
                    ManualRequestId = Guid.NewGuid(),
                    CreatedByUserId = userId,
                    Reason = "Concurrency fixture credit",
                    AvailablePointsBefore = 0,
                    AvailablePointsAfter = 10,
                    OccurredAtUtc = seeded.Now
                }
            );
            await seedDb.SaveChangesAsync();
        }

        var activateTask = ActivateAsync();
        var prepareTask = PrepareAsync();
        await Task.WhenAll(activateTask, prepareTask);
        var activateResult = await activateTask;
        var prepareResult = await prepareTask;

        await using var verifyDb = _database.CreateDbContext();
        var round = await verifyDb.GameRounds.SingleAsync(x => x.Id == roundId);
        var activationCount = await verifyDb.GameModifierActivations.CountAsync(
            x => x.RoundId == roundId
        );

        if (activateResult.Status == ActivateGameModifierRepositoryStatus.Activated)
        {
            Assert.Equal(TransitionGameRoundOutcome.StaleVersion, prepareResult.Outcome);
            Assert.Equal(GameRoundStatusValue.AwaitingModifiers, round.Status);
            Assert.Equal(1, activationCount);
        }
        else
        {
            Assert.Equal(
                ActivateGameModifierRepositoryStatus.ModifierOrderingClosed,
                activateResult.Status
            );
            Assert.Equal(TransitionGameRoundOutcome.Transitioned, prepareResult.Outcome);
            Assert.Equal(GameRoundStatusValue.Preparing, round.Status);
            Assert.Equal(0, activationCount);
        }

        Assert.Equal(2, round.Version);

        async Task<ActivateGameModifierRepositoryResult> ActivateAsync()
        {
            await using var db = _database.CreateDbContext();
            return await new DbGameModifierRepository(db, TimeProvider.System).ActivateModifierAsync(
                modifierId,
                userId,
                userId
            );
        }

        async Task<TransitionGameRoundResult> PrepareAsync()
        {
            await using var db = _database.CreateDbContext();
            return await new DbGameRoundRepository(db).PrepareAsync(
                roundId,
                new GameRoundVersionCommandInput(1),
                userId
            );
        }
    }

    [Fact]
    public async Task PrepareAndRebuild_WhenConcurrent_PreserveVersionedTransitionOrder()
    {
        await _database.ResetAsync();
        Guid roundId;
        Guid userId;
        await using (var seedDb = _database.CreateDbContext())
        {
            var seeded = await SeedPlayableRoundGraphAsync(seedDb);
            roundId = Guid.NewGuid();
            userId = seeded.UserId;
            seedDb.GameRounds.Add(
                new GameRound
                {
                    Id = roundId,
                    GameId = seeded.GameId,
                    BoardId = seeded.BoardId,
                    BoardCellId = seeded.CellId,
                    TeamId = seeded.TeamId,
                    Status = GameRoundStatusValue.AwaitingModifiers,
                    Version = 1,
                    BaseScore = 100,
                    TeamSlotIndexSnapshot = 1,
                    CellRowIndex = 0,
                    CellColIndex = 0,
                    CellTitleSnapshot = "Test cell",
                    CellCostSnapshot = 100,
                    CreatedAtUtc = seeded.Now,
                    UpdatedAtUtc = seeded.Now
                }
            );
            await seedDb.SaveChangesAsync();
        }

        var prepareTask = TransitionAsync(isRebuild: false);
        var rebuildTask = TransitionAsync(isRebuild: true);
        await Task.WhenAll(prepareTask, rebuildTask);
        var prepareResult = await prepareTask;
        var rebuildResult = await rebuildTask;

        Assert.Equal(TransitionGameRoundOutcome.Transitioned, prepareResult.Outcome);
        Assert.Contains(
            rebuildResult.Outcome,
            new[] { TransitionGameRoundOutcome.Transitioned, TransitionGameRoundOutcome.StaleVersion }
        );

        await using var verifyDb = _database.CreateDbContext();
        var round = await verifyDb.GameRounds.SingleAsync(x => x.Id == roundId);
        var audits = await verifyDb.GameRoundTransitionAudits
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.Sequence)
            .ToArrayAsync();
        if (rebuildResult.Outcome == TransitionGameRoundOutcome.Transitioned)
        {
            Assert.Equal(GameRoundStatusValue.AwaitingModifiers, round.Status);
            Assert.Equal(3, round.Version);
            Assert.Equal(2, audits.Length);
            Assert.Equal(GameRoundTransitionActionValue.Prepare, audits[0].ActionCode);
            Assert.Equal(GameRoundTransitionActionValue.Rebuild, audits[1].ActionCode);
        }
        else
        {
            Assert.Equal(GameRoundStatusValue.Preparing, round.Status);
            Assert.Equal(2, round.Version);
            var prepareAudit = Assert.Single(audits);
            Assert.Equal(GameRoundTransitionActionValue.Prepare, prepareAudit.ActionCode);
        }

        async Task<TransitionGameRoundResult> TransitionAsync(bool isRebuild)
        {
            await using var db = _database.CreateDbContext();
            var repository = new DbGameRoundRepository(db);
            return isRebuild
                ? await repository.RebuildAsync(
                    roundId,
                    new GameRoundVersionCommandInput(2),
                    userId
                )
                : await repository.PrepareAsync(
                    roundId,
                    new GameRoundVersionCommandInput(1),
                    userId
                );
        }
    }

    [Fact]
    public async Task FinishAndActiveTeamSelection_WhenConcurrent_CannotMutateFinishedGame()
    {
        await _database.ResetAsync();
        SeededRoundGraph seeded;
        await using (var seedDb = _database.CreateDbContext())
        {
            seeded = await SeedPlayableRoundGraphAsync(seedDb);
        }

        var finishTask = Task.Run(async () =>
        {
            await using var db = _database.CreateDbContext();
            var persistence = new DbGameLifecyclePersistence(
                db,
                NullLogger<DbGameLifecyclePersistence>.Instance,
                TimeProvider.System
            );
            return await persistence.FinishGameAsync(
                seeded.GameId,
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
                seeded.UserId
            );
        });
        var selectTask = Task.Run(async () =>
        {
            await using var db = _database.CreateDbContext();
            var repository = new DbGameBoardRepository(
                db,
                Options.Create(new StorageOptions()),
                NullLogger<DbGameBoardRepository>.Instance
            );
            return await repository.SetActiveTeamAsync(seeded.TeamId);
        });

        var finish = await finishTask;
        await selectTask;
        Assert.True(finish.Success);

        await using var assertDb = _database.CreateDbContext();
        var game = await assertDb.Games.SingleAsync(x => x.Id == seeded.GameId);
        Assert.Equal(GameStatusValue.Finished, game.Status);
        Assert.Null(game.ActiveTeamId);
        Assert.Equal(
            1,
            await assertDb.GameFinalizations.CountAsync(x => x.GameId == seeded.GameId)
        );
    }

    [Fact]
    public async Task Finish_WhenSnapshotInsertFails_RollsBackEveryLifecycleMutation()
    {
        await _database.ResetAsync();
        SeededRoundGraph seeded;
        await using (var seedDb = _database.CreateDbContext())
        {
            seeded = await SeedPlayableRoundGraphAsync(seedDb);
        }

        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using (var installFailure = connection.CreateCommand())
        {
            installFailure.CommandText =
                """
                CREATE OR REPLACE FUNCTION fail_game_finalization_insert()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'simulated finalization failure';
                END;
                $$;
                CREATE TRIGGER fail_game_finalization_insert
                BEFORE INSERT ON game_finalizations
                FOR EACH ROW EXECUTE FUNCTION fail_game_finalization_insert();
                """;
            await installFailure.ExecuteNonQueryAsync();
        }

        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await using var db = _database.CreateDbContext();
                var persistence = new DbGameLifecyclePersistence(
                    db,
                    NullLogger<DbGameLifecyclePersistence>.Instance,
                    TimeProvider.System
                );
                await persistence.FinishGameAsync(
                    seeded.GameId,
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
                    seeded.UserId
                );
            });
        }
        finally
        {
            await using var removeFailure = connection.CreateCommand();
            removeFailure.CommandText =
                """
                DROP TRIGGER IF EXISTS fail_game_finalization_insert ON game_finalizations;
                DROP FUNCTION IF EXISTS fail_game_finalization_insert();
                """;
            await removeFailure.ExecuteNonQueryAsync();
        }

        await using var assertDb = _database.CreateDbContext();
        var game = await assertDb.Games.SingleAsync(x => x.Id == seeded.GameId);
        Assert.Equal(GameStatusValue.Active, game.Status);
        Assert.Null(game.FinishedAtUtc);
        Assert.Equal(
            1,
            await assertDb.GameBoards
                .Where(x => x.GameId == seeded.GameId)
                .Select(x => x.Version)
                .SingleAsync()
        );
        Assert.Empty(await assertDb.GameFinalizations.ToArrayAsync());
    }

    private static async Task<SeededRoundGraph> SeedPlayableRoundGraphAsync(
        ApplicationDbContext db,
        Func<ApplicationDbContext, DateTime, Guid, Guid, Task>? configureDraftAsync = null
    )
    {
        var now = DateTime.UtcNow;
        var user = CreateUser("resolver");
        var memberUser = CreateUser("team-member");
        var game = CreateGame(GameStatusValue.Draft, now);
        var board = new GameBoard
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Rows = 1,
            Cols = 2,
            RowLabels = new[] { "A" },
            ColLabels = new[] { "1", "2" },
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
            State = BoardCellState.Closed,
            CellType = BoardCellPersistence.DefaultCellType
        };
        var slot = CreateSlot(game.Id, 1, now);
        var team = CreateTeam(game.Id, slot.Id, now, TeamStatusValue.Confirmed);
        team.CreatedByUserId = user.Id;
        team.ConfirmedAtUtc = now;
        team.ConfirmedByUserId = user.Id;
        var member = new GameTeamMember
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            TeamId = team.Id,
            UserId = memberUser.Id,
            JoinedAtUtc = now
        };
        var secondCell = new BoardCell
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            RowIndex = 0,
            ColIndex = 1,
            Title = "Second test cell",
            Cost = 100,
            State = BoardCellState.Closed,
            CellType = BoardCellPersistence.DefaultCellType
        };

        db.AddRange(user, memberUser, game, board, cell, secondCell, slot, team, member);
        await db.SaveChangesAsync();

        if (configureDraftAsync is not null)
        {
            await configureDraftAsync(db, now, game.Id, user.Id);
        }

        game.Status = GameStatusValue.Ready;
        game.ReadyAtUtc = now;
        await db.SaveChangesAsync();

        game.Status = GameStatusValue.Active;
        game.StartedAtUtc = now;
        await db.SaveChangesAsync();

        cell.State = BoardCellState.Open;
        secondCell.State = BoardCellState.Open;
        await db.SaveChangesAsync();

        return new SeededRoundGraph(
            now,
            game.Id,
            board.Id,
            cell.Id,
            secondCell.Id,
            team.Id,
            user.Id
        );
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

    private static void AddDraftBoard(ApplicationDbContext db, Guid gameId, DateTime now)
    {
        var boardId = Guid.NewGuid();
        db.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
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
    }

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
        Guid BoardId,
        Guid CellId,
        Guid SecondCellId,
        Guid TeamId,
        Guid UserId
    );
}
