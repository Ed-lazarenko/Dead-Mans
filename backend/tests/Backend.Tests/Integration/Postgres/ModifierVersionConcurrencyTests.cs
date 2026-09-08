using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Integration.Postgres;

public sealed class ModifierVersionConcurrencyTests : IClassFixture<PostgresTestDatabase>
{
    private static readonly Guid ModifierId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly PostgresTestDatabase _database;

    public ModifierVersionConcurrencyTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task StartAndUpdateRace_KeepsTheRevisionPublishedAtReady()
    {
        await _database.ResetAsync();
        var seeded = await SeedReadyGameAsync();
        UpdateGameModifierInput update;
        await using (var readDb = _database.CreateDbContext())
        {
            var definition = await readDb.ModifierDefinitions
                .AsNoTracking()
                .Where(x => x.Id == ModifierId)
                .Select(x => x.CurrentVersion!)
                .SingleAsync();
            var conflictIds = await readDb.ModifierDefinitionVersionConflicts
                .AsNoTracking()
                .Where(x => x.ModifierVersionId == definition.Id)
                .Select(x => x.ConflictingModifierId)
                .ToArrayAsync();
            update = new UpdateGameModifierInput(
                definition.Name + " concurrent edit",
                definition.Description,
                definition.Category,
                definition.ActivationCost + 9,
                new GameModifierActivationLimit(definition.MaxActivationsPerRound),
                conflictIds,
                definition.IconEmoji,
                definition.ActivationCommand,
                definition.NormalizedTags,
                ModifierBehaviorV2Json.Deserialize(definition.BehaviorV2Json),
                definition.Revision,
                "Concurrent start/update verification"
            );
        }

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startTask = Task.Run(async () =>
        {
            await startGate.Task;
            await using var db = _database.CreateDbContext();
            var lifecycle = new DbGameLifecyclePersistence(
                db,
                NullLogger<DbGameLifecyclePersistence>.Instance
            );
            return await lifecycle.StartGameAsync(seeded.GameId);
        });
        var updateTask = Task.Run(async () =>
        {
            await startGate.Task;
            await using var db = _database.CreateDbContext();
            var repository = new DbGameModifierRepository(db);
            return await repository.UpdateModifierAsync(
                ModifierId,
                update,
                new ModifierChangeActor(seeded.UserId, "Concurrency Admin")
            );
        });

        startGate.SetResult();
        var startResult = await startTask;
        var updateResult = await updateTask;

        Assert.True(startResult.Success);
        Assert.Contains(
            updateResult.Status,
            new[]
            {
                UpdateGameModifierRepositoryStatus.Updated,
                UpdateGameModifierRepositoryStatus.ContentLocked
            }
        );

        await using var assertDb = _database.CreateDbContext();
        var pinnedRevision = await assertDb.GameEnabledModifiers
            .Where(x => x.GameId == seeded.GameId && x.ModifierId == ModifierId)
            .Select(x => x.ModifierVersion!.Revision)
            .SingleAsync();
        var currentRevision = await assertDb.ModifierDefinitions
            .Where(x => x.Id == ModifierId)
            .Select(x => x.CurrentVersion!.Revision)
            .SingleAsync();
        Assert.Equal(update.ExpectedRevision, pinnedRevision);
        Assert.Equal(
            updateResult.Status == UpdateGameModifierRepositoryStatus.Updated
                ? update.ExpectedRevision + 1
                : update.ExpectedRevision,
            currentRevision
        );
    }

    [Fact]
    public async Task StartAndArchiveRace_KeepsPublishedContentAndSerializesActiveLock()
    {
        await _database.ResetAsync();
        var seeded = await SeedReadyGameAsync();
        await using var revisionDb = _database.CreateDbContext();
        var expectedRevision = await revisionDb.ModifierDefinitions.AsNoTracking()
            .Where(x => x.Id == ModifierId)
            .Select(x => x.CurrentVersion!.Revision)
            .SingleAsync();

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startTask = Task.Run(async () =>
        {
            await gate.Task;
            await using var db = _database.CreateDbContext();
            return await new DbGameLifecyclePersistence(
                db, NullLogger<DbGameLifecyclePersistence>.Instance).StartGameAsync(seeded.GameId);
        });
        var archiveTask = Task.Run(async () =>
        {
            await gate.Task;
            await using var db = _database.CreateDbContext();
            return await new DbGameModifierRepository(db).ArchiveModifierAsync(
                ModifierId,
                expectedRevision,
                new ModifierChangeActor(seeded.UserId, "Concurrency Admin"));
        });

        gate.SetResult();
        await Task.WhenAll(startTask, archiveTask);
        var start = await startTask;
        var archive = await archiveTask;

        await using var assertDb = _database.CreateDbContext();
        var root = await assertDb.ModifierDefinitions.AsNoTracking()
            .SingleAsync(x => x.Id == ModifierId);
        var game = await assertDb.Games.AsNoTracking().SingleAsync(x => x.Id == seeded.GameId);
        var missingPins = await assertDb.GameEnabledModifiers.AsNoTracking()
            .CountAsync(x => x.GameId == seeded.GameId && x.ModifierVersionId == null);

        Assert.True(start.Success);
        Assert.Equal(GameStatusValue.Active, game.Status);
        Assert.Equal(0, missingPins);
        if (archive == ArchiveGameModifierRepositoryStatus.ContentLocked)
        {
            Assert.False(root.IsArchived);
        }
        else
        {
            Assert.Equal(ArchiveGameModifierRepositoryStatus.Archived, archive);
            Assert.True(root.IsArchived);
        }
    }

    [Fact]
    public async Task FinishedGameRetainsPinnedRevisionWhileNextGameUsesNewRevision()
    {
        await _database.ResetAsync();
        var first = await SeedReadyGameAsync();
        ModifierDefinitionVersion before;
        Guid[] conflictIds;
        await using (var readDb = _database.CreateDbContext())
        {
            before = await readDb.ModifierDefinitions.AsNoTracking()
                .Where(x => x.Id == ModifierId)
                .Select(x => x.CurrentVersion!)
                .SingleAsync();
            conflictIds = await readDb.ModifierDefinitionVersionConflicts.AsNoTracking()
                .Where(x => x.ModifierVersionId == before.Id)
                .Select(x => x.ConflictingModifierId)
                .ToArrayAsync();
        }

        await using (var startDb = _database.CreateDbContext())
        {
            var lifecycle = new DbGameLifecyclePersistence(
                startDb, NullLogger<DbGameLifecyclePersistence>.Instance);
            Assert.True((await lifecycle.StartGameAsync(first.GameId)).Success);
        }
        var firstActivation = await ActivatePinnedModifierAsync(first);
        await CancelRoundAndFinishGameAsync(first, firstActivation.RoundId);

        var editedName = before.Name + " immutable v-next";
        var editedBehavior = BuiltInModifierBehaviorCatalog
            .Get(BuiltInModifierBehaviorCatalog.Zhazhda)
            .Behavior;
        await using (var updateDb = _database.CreateDbContext())
        {
            var repository = new DbGameModifierRepository(updateDb);
            var update = await repository.UpdateModifierAsync(
                ModifierId,
                new UpdateGameModifierInput(
                    editedName, before.Description, GameModifierCategories.Result, before.ActivationCost + 5,
                    new GameModifierActivationLimit(1), conflictIds,
                    before.IconEmoji, before.ActivationCommand, before.NormalizedTags,
                    editedBehavior, before.Revision,
                    "Game 2 must use the new revision"),
                new ModifierChangeActor(first.UserId, "Concurrency Admin"));
            Assert.Equal(UpdateGameModifierRepositoryStatus.Updated, update.Status);
        }

        var second = await SeedReadyGameAsync();
        await using (var startDb = _database.CreateDbContext())
        {
            var lifecycle = new DbGameLifecyclePersistence(
                startDb, NullLogger<DbGameLifecyclePersistence>.Instance);
            Assert.True((await lifecycle.StartGameAsync(second.GameId)).Success);
        }
        await using (var lockDb = _database.CreateDbContext())
        {
            var current = await lockDb.ModifierDefinitions.AsNoTracking()
                .Where(x => x.Id == ModifierId)
                .Select(x => x.CurrentVersion!)
                .SingleAsync();
            var locked = await new DbGameModifierRepository(lockDb).UpdateModifierAsync(
                ModifierId,
                new UpdateGameModifierInput(
                    current.Name + " forbidden while active",
                    current.Description,
                    current.Category,
                    current.ActivationCost,
                    new GameModifierActivationLimit(current.MaxActivationsPerRound),
                    conflictIds,
                    current.IconEmoji,
                    current.ActivationCommand,
                    current.NormalizedTags,
                    ModifierBehaviorV2Json.Deserialize(current.BehaviorV2Json),
                    current.Revision,
                    "Must be rejected while game 2 is active"),
                new ModifierChangeActor(second.UserId, "Concurrency Admin"));
            Assert.Equal(UpdateGameModifierRepositoryStatus.ContentLocked, locked.Status);
        }
        var secondActivation = await ActivatePinnedModifierAsync(second);
        await CancelRoundAndFinishGameAsync(second, secondActivation.RoundId);
        ArchiveGameModifierRepositoryStatus archiveStatus;
        await using (var archiveDb = _database.CreateDbContext())
        {
            var revision = await archiveDb.ModifierDefinitions.AsNoTracking()
                .Where(x => x.Id == ModifierId)
                .Select(x => x.CurrentVersion!.Revision)
                .SingleAsync();
            archiveStatus = await new DbGameModifierRepository(archiveDb).ArchiveModifierAsync(
                ModifierId,
                revision,
                new ModifierChangeActor(second.UserId, "Concurrency Admin"));
        }
        Assert.Equal(ArchiveGameModifierRepositoryStatus.Archived, archiveStatus);

        await using var assertDb = _database.CreateDbContext();
        var pins = await assertDb.GameEnabledModifiers.AsNoTracking()
            .Where(x => x.GameId == first.GameId || x.GameId == second.GameId)
            .Select(x => new
            {
                x.GameId,
                x.ModifierId,
                Revision = x.ModifierVersion!.Revision,
                Name = x.ModifierVersion.Name,
                x.VersionPinnedAtUtc
            })
            .ToArrayAsync();
        Assert.Equal(2, pins.Count(x => x.GameId == first.GameId));
        Assert.Equal(2, pins.Count(x => x.GameId == second.GameId));
        Assert.All(pins, x => Assert.True(x.VersionPinnedAtUtc.HasValue));
        var game1Modifier = Assert.Single(pins, x =>
            x.GameId == first.GameId && x.ModifierId == ModifierId);
        var game2Modifier = Assert.Single(pins, x =>
            x.GameId == second.GameId && x.ModifierId == ModifierId);
        Assert.Equal(before.Revision, game1Modifier.Revision);
        Assert.Equal(before.Name, game1Modifier.Name);
        Assert.Equal(before.Revision + 1, game2Modifier.Revision);
        Assert.Equal(editedName, game2Modifier.Name);
        Assert.Equal(before.Id, firstActivation.ModifierVersionId);
        Assert.Equal(before.ActivationCost, firstActivation.ActivationCostSnapshot);
        Assert.Equal(ModifierBehaviorKind.Rule,
            ModifierBehaviorV2Json.Deserialize(firstActivation.BehaviorV2SnapshotJson).Kind);
        Assert.Equal(game2Modifier.Revision, secondActivation.DefinitionRevisionSnapshot);
        Assert.Equal(before.ActivationCost + 5, secondActivation.ActivationCostSnapshot);
        var secondBehavior = ModifierBehaviorV2Json.Deserialize(secondActivation.BehaviorV2SnapshotJson);
        Assert.Equal(ModifierBehaviorKind.Scoring, secondBehavior.Kind);
        Assert.Equal(ModifierFormulaCodes.KillValueIncreasePerUnit, secondBehavior.FormulaReference?.Code);
        Assert.True(await assertDb.ModifierDefinitions.AsNoTracking()
            .Where(x => x.Id == ModifierId)
            .Select(x => x.IsArchived)
            .SingleAsync());
        Assert.DoesNotContain(await new DbGameModifierRepository(assertDb).GetCatalogAsync(),
            modifier => modifier.Id == ModifierId);
        Assert.Equal(2, await assertDb.GameEnabledModifiers.AsNoTracking()
            .CountAsync(x => (x.GameId == first.GameId || x.GameId == second.GameId)
                && x.ModifierId == ModifierId));
    }

    private async Task<GameFixture> SeedReadyGameAsync()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        await using var db = _database.CreateDbContext();
        if (!await db.ModifierDefinitions.AnyAsync(x => x.Id == ModifierId))
        {
            await TestModifierVersionFactory.AddAsync(
                db,
                [
                    new TestModifierSpec(
                        ModifierId,
                        "Concurrency modifier",
                        "Pinned revision race fixture",
                        GameModifierCategories.Round,
                        1,
                        3,
                        BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior
                    ),
                    new TestModifierSpec(
                        ModifierDefinitionSeedIds.Feyerverk,
                        "Concurrency companion",
                        "Complete enabled-set pin fixture",
                        GameModifierCategories.Round,
                        1,
                        null,
                        BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Feyerverk).Behavior
                    )
                ],
                now
            );
        }
        db.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"concurrency-{userId:N}",
                Login = $"race-{userId:N}"[..32],
                DisplayName = "Concurrency Admin",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        db.Users.Add(
            new User
            {
                Id = buyerId,
                TwitchUserId = $"concurrency-buyer-{buyerId:N}",
                Login = $"buyer-{buyerId:N}"[..32],
                DisplayName = "Modifier Buyer",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Modifier concurrency game",
                Status = GameStatusValue.Draft,
                CreatedAtUtc = now,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 2
            }
        );
        db.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = now
            }
        );
        db.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                Name = "Concurrency Team",
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = userId,
                ConfirmedAtUtc = now,
                ConfirmedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        db.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = userId,
                JoinedAtUtc = now
            }
        );
        db.GameBoards.Add(new GameBoard
        {
            Id = boardId,
            GameId = gameId,
            Rows = 1,
            Cols = 1,
            RowLabels = ["A"],
            ColLabels = ["1"],
            Version = 1,
            CreatedAtUtc = now
        });
        db.BoardCells.Add(new BoardCell
        {
            Id = cellId,
            BoardId = boardId,
            RowIndex = 0,
            ColIndex = 0,
            Title = "Smoke cell",
            Cost = 100,
            State = BoardCellState.Closed,
            CellType = BoardCellPersistence.DefaultCellType
        });
        db.GameEnabledModifiers.AddRange(
            new GameEnabledModifier
            {
                GameId = gameId,
                ModifierId = ModifierId,
                EnabledAtUtc = now
            },
            new GameEnabledModifier
            {
                GameId = gameId,
                ModifierId = ModifierDefinitionSeedIds.Feyerverk,
                EnabledAtUtc = now
            }
        );
        await db.SaveChangesAsync();
        var lifecycle = new DbGameLifecyclePersistence(
            db,
            NullLogger<DbGameLifecyclePersistence>.Instance
        );
        var publication = await lifecycle.OpenRegistrationAsync(gameId);
        Assert.True(publication.Success);
        return new GameFixture(gameId, userId, buyerId, teamId, boardId, cellId);
    }

    private async Task<backend.Data.Entities.GameModifierActivation> ActivatePinnedModifierAsync(
        GameFixture fixture)
    {
        var now = DateTime.UtcNow;
        await using (var seedDb = _database.CreateDbContext())
        {
            var cell = await seedDb.BoardCells.SingleAsync(x => x.Id == fixture.CellId);
            cell.State = BoardCellState.Open;
            seedDb.GameRounds.Add(new GameRound
            {
                Id = Guid.NewGuid(),
                GameId = fixture.GameId,
                BoardId = fixture.BoardId,
                BoardCellId = fixture.CellId,
                TeamId = fixture.TeamId,
                Status = GameRoundStatusValue.AwaitingModifiers,
                Version = 1,
                BaseScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Smoke cell",
                CellCostSnapshot = 100,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            seedDb.GameQuizPointLedgerEntries.Add(new GameQuizPointLedgerEntry
            {
                Id = Guid.NewGuid(),
                GameId = fixture.GameId,
                UserId = fixture.BuyerId,
                EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                PointsDelta = 100,
                ManualRequestId = Guid.NewGuid(),
                CreatedByUserId = fixture.UserId,
                Reason = "Modifier concurrency fixture credit",
                AvailablePointsBefore = 0,
                AvailablePointsAfter = 100,
                OccurredAtUtc = now
            });
            await seedDb.SaveChangesAsync();
        }

        await using (var activateDb = _database.CreateDbContext())
        {
            var result = await new DbGameModifierRepository(activateDb).ActivateModifierAsync(
                ModifierId,
                fixture.BuyerId,
                fixture.UserId);
            Assert.Equal(ActivateGameModifierRepositoryStatus.Activated, result.Status);
        }

        await using var assertDb = _database.CreateDbContext();
        return await assertDb.GameModifierActivations.AsNoTracking()
            .SingleAsync(x => x.GameId == fixture.GameId && x.ModifierId == ModifierId);
    }

    private async Task CancelRoundAndFinishGameAsync(GameFixture fixture, Guid roundId)
    {
        await using (var cancelDb = _database.CreateDbContext())
        {
            var version = await cancelDb.GameRounds
                .Where(x => x.Id == roundId)
                .Select(x => x.Version)
                .SingleAsync();
            var cancelled = await new DbGameRoundRepository(cancelDb).TechnicalCancelAsync(
                roundId,
                new TechnicalCancelGameRoundInput(
                    version,
                    GameRoundTechnicalCancellationReasonValue.ApplicationError,
                    null,
                    "Modifier revision concurrency fixture cleanup"
                ),
                fixture.UserId
            );
            Assert.Equal(TransitionGameRoundOutcome.Transitioned, cancelled.Outcome);
        }

        await using var finishDb = _database.CreateDbContext();
        var boardVersion = await finishDb.GameBoards
            .Where(x => x.GameId == fixture.GameId)
            .Select(x => x.Version)
            .SingleAsync();
        var finished = await new DbGameLifecyclePersistence(
            finishDb,
            NullLogger<DbGameLifecyclePersistence>.Instance
        ).FinishGameAsync(
            fixture.GameId,
            new FinishGameInput(
                boardVersion,
                Guid.NewGuid(),
                new HashSet<string>
                {
                    GameFinishWarningCodes.UnplayedTeams,
                    GameFinishWarningCodes.NoCompletedRounds
                },
                null
            ),
            fixture.UserId
        );
        Assert.True(finished.Success);
    }

    private sealed record GameFixture(
        Guid GameId,
        Guid UserId,
        Guid BuyerId,
        Guid TeamId,
        Guid BoardId,
        Guid CellId);
}
