using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.Scoring;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GameModifierActivationContract = backend.Application.Contracts.GameModifierActivation;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameModifierRepository : IGameModifierRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _dbContext;

    public DbGameModifierRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    ) => await new ModifierCatalogReadProjection(_dbContext).LoadAsync(cancellationToken);

    public async Task<GetGameModifierStateRepositoryResult> GetStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return new(GetGameModifierStateRepositoryOutcome.GameNotActive);
        }

        var enabledCount = await _dbContext.GameEnabledModifiers.AsNoTracking()
            .CountAsync(x => x.GameId == activeGame.Id, cancellationToken);
        var pinnedCount = await _dbContext.GameEnabledModifiers.AsNoTracking()
            .CountAsync(x => x.GameId == activeGame.Id && x.ModifierVersionId != null, cancellationToken);
        if (enabledCount != pinnedCount)
        {
            return new(GetGameModifierStateRepositoryOutcome.VersionBindingMissing);
        }

        var enabledRows = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == activeGame.Id && x.ModifierVersionId != null)
            .Select(
                x => new
                {
                    Version = x.ModifierVersion!,
                    x.EmergencyDisabledAtUtc
                }
            )
            .OrderBy(x => x.Version.ActivationCost)
            .ThenBy(x => x.Version.Name)
            .ToArrayAsync(cancellationToken);
        var enabledDefinitions = enabledRows.Select(x => x.Version).ToArray();
        var enabledDefinitionIds = enabledDefinitions.Select(x => x.ModifierId).ToArray();
        var enabledVersionIds = enabledDefinitions.Select(x => x.Id).ToArray();

        var conflictRows = await _dbContext.ModifierDefinitionVersionConflicts
            .AsNoTracking()
            .Where(x => enabledVersionIds.Contains(x.ModifierVersionId))
            .ToArrayAsync(cancellationToken);
        var conflictLookup = enabledDefinitionIds.ToDictionary(
            id => id,
            id => conflictRows.Where(x => enabledDefinitions.Any(
                    version => version.ModifierId == id && version.Id == x.ModifierVersionId))
                .Select(x => x.ConflictingModifierId)
                .Where(enabledDefinitionIds.Contains)
                .Distinct()
                .ToArray()
        );

        var activeRows = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == activeGame.Id
                    && x.ArchivedAtUtc == null
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .OrderByDescending(x => x.ActivatedAtUtc)
            .Select(
                x => new
                {
                    x.Id,
                    x.RoundId,
                    RoundVersion = x.Round.Version,
                    x.ModifierId,
                    Name = x.ModifierNameSnapshot,
                    x.ActivatedByUserId,
                    x.ActivationCostSnapshot,
                    x.ActivatedAtUtc
                }
            )
            .ToArrayAsync(cancellationToken);
        var activeModifierIds = activeRows.Select(x => x.ModifierId).ToHashSet();
        var activationCounts = activeRows
            .GroupBy(x => x.ModifierId)
            .ToDictionary(x => x.Key, x => x.Count());

        var orderingRound = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == activeGame.Id
                    && x.Status == GameRoundStatusValue.AwaitingModifiers
            )
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.TeamId })
            .FirstOrDefaultAsync(cancellationToken);
        var orderingOpen = orderingRound is not null;
        var isActiveTeamMember = orderingRound is not null
            && await IsRoundTeamMemberAsync(
                activeGame.Id,
                orderingRound.Id,
                orderingRound.TeamId,
                userId,
                cancellationToken
            );

        var rawEarnedPoints = await GetEarnedQuizPointsAsync(activeGame.Id, userId, cancellationToken);
        var rawSpentPoints = await GetSpentQuizPointsAsync(activeGame.Id, userId, cancellationToken);
        var earnedPoints = SaturatingInt32.From(rawEarnedPoints);
        var spentPoints = SaturatingInt32.From(rawSpentPoints);
        var availablePoints = SaturatingInt32.From(Math.Max(0L, rawEarnedPoints - rawSpentPoints));
        var activatedByUserIds = activeRows.Select(x => x.ActivatedByUserId).Distinct().ToArray();
        var activatedByDisplayNames = await _dbContext.Users
            .AsNoTracking()
            .Where(x => activatedByUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

        var activeModifiers = activeRows
            .Select(
                x => new GameModifierActivationContract(
                    x.Id,
                    x.RoundId,
                    x.RoundVersion,
                    x.ModifierId,
                    x.Name,
                    x.ActivatedByUserId.ToString(),
                    activatedByDisplayNames.GetValueOrDefault(x.ActivatedByUserId)
                        ?? x.ActivatedByUserId.ToString(),
                    x.ActivationCostSnapshot,
                    x.ActivatedAtUtc
                )
            )
            .ToArray();

        var availableModifiers = enabledRows
            .Select(enabledRow =>
            {
                var definition = enabledRow.Version;
                var modifier = MapDefinition(
                    definition,
                    conflictLookup.GetValueOrDefault(definition.ModifierId) ?? Array.Empty<Guid>(),
                    isLockedByActiveGame: true
                );
                var count = activationCounts.GetValueOrDefault(definition.ModifierId);
                var limit = modifier.ActivationLimit.Count;
                var hasLimitReached = limit.HasValue && count >= limit.Value;
                var conflicts = conflictLookup.GetValueOrDefault(definition.ModifierId) ?? Array.Empty<Guid>();
                var hasConflict = conflicts.Any(activeModifierIds.Contains);
                var isActive = activeModifierIds.Contains(definition.ModifierId);
                var blockedReason = ResolveBlockedReason(
                    enabledRow.EmergencyDisabledAtUtc.HasValue,
                    !orderingOpen,
                    isActiveTeamMember,
                    hasLimitReached,
                    hasConflict,
                    availablePoints,
                    definition.ActivationCost
                );

                return new GameModifierAvailability(
                    modifier,
                    isActive,
                    blockedReason is null,
                    blockedReason,
                    count,
                    limit,
                    enabledRow.EmergencyDisabledAtUtc.HasValue,
                    enabledRow.EmergencyDisabledAtUtc
                );
            })
            .ToArray();

        var state = new GameModifierState(
            activeGame.Id,
            availablePoints,
            earnedPoints,
            spentPoints,
            orderingOpen,
            activeModifiers,
            availableModifiers
        );
        return new(GetGameModifierStateRepositoryOutcome.Loaded, state);
    }

    public Task<bool> HasActiveGameAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Games
            .AsNoTracking()
            .AnyAsync(
                x => x.Status == GameStatusValue.Active && !x.IsDeleted,
                cancellationToken
            );
    }

    public Task<Guid?> GetActiveGameIdAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GameModifierAdminPlayersResult> GetAdminPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return EmptyAdminPlayersResult();
        }

        var players = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Login)
            .Select(x => new { x.Id, x.Login, x.DisplayName })
            .ToArrayAsync(cancellationToken);
        if (players.Length == 0)
        {
            return EmptyAdminPlayersResult();
        }

        var playerIds = players.Select(x => x.Id).ToArray();
        var earnedPoints = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == activeGameId.Value
                && playerIds.Contains(x.UserId)
                && (x.EntryType == GameQuizPointEntryTypeValue.QuizReward
                    || x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => (long)item.PointsDelta) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        var spentPoints = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == activeGameId.Value
                && playerIds.Contains(x.UserId)
                && (x.EntryType == GameQuizPointEntryTypeValue.ModifierPurchase
                    || x.EntryType == GameQuizPointEntryTypeValue.ModifierRefund))
            .GroupBy(x => x.UserId)
            .Select(
                x =>
                    new
                    {
                        UserId = x.Key,
                        Points = -x.Sum(item => (long)item.PointsDelta)
                    }
            )
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        var playerBalances = players
            .Select(player =>
            {
                var rawEarned = earnedPoints.GetValueOrDefault(player.Id);
                var rawSpent = spentPoints.GetValueOrDefault(player.Id);
                return new GameModifierAdminPlayer(
                    player.Id,
                    player.Login,
                    player.DisplayName,
                    SaturatingInt32.From(Math.Max(0L, rawEarned - rawSpent)),
                    SaturatingInt32.From(rawEarned),
                    SaturatingInt32.From(rawSpent)
                );
            })
            .ToArray();

        return new GameModifierAdminPlayersResult(
            new GameModifierAdminPlayersSummary(
                playerBalances.Length,
                SaturatingInt32.From(playerBalances.Sum(x => (long)x.AvailableQuizPoints)),
                SaturatingInt32.From(playerBalances.Sum(x => (long)x.EarnedQuizPoints)),
                SaturatingInt32.From(playerBalances.Sum(x => (long)x.SpentQuizPoints))
            ),
            playerBalances
        );
    }

    private static GameModifierAdminPlayersResult EmptyAdminPlayersResult()
    {
        return new GameModifierAdminPlayersResult(
            new GameModifierAdminPlayersSummary(0, 0, 0, 0),
            Array.Empty<GameModifierAdminPlayer>()
        );
    }

    public Task<bool> AdminPlayerExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }

    public Task<bool> ModifierIdExistsAsync(
        Guid modifierId,
        CancellationToken cancellationToken = default
    )
    {
        return _dbContext.ModifierDefinitions
            .AsNoTracking()
            .AnyAsync(x => x.Id == modifierId && !x.IsArchived, cancellationToken);
    }

    public async Task<bool> ModifierIdsExistAsync(
        IReadOnlyList<Guid> modifierIds,
        CancellationToken cancellationToken = default
    )
    {
        if (modifierIds.Count == 0)
        {
            return true;
        }

        var knownCount = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => !x.IsArchived && modifierIds.Contains(x.Id))
            .CountAsync(cancellationToken);
        return knownCount == modifierIds.Distinct().Count();
    }

    public async Task<IReadOnlyList<Guid>> GetEnabledModifierIdsForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.ModifierId)
            .Select(x => x.ModifierId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameModifierActivationContract>> GetActiveModifiersForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == gameId
                    && x.Status == GameModifierActivationStatusValue.Active
            )
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                x => new GameModifierActivationContract(
                    x.Id,
                    x.RoundId,
                    x.Round.Version,
                    x.ModifierId,
                    x.ModifierNameSnapshot,
                    x.ActivatedByUserId.ToString(),
                    x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : x.ActivatedByUserId.ToString(),
                    x.ActivationCostSnapshot,
                    x.ActivatedAtUtc
                )
            )
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ActivateGameModifierRepositoryResult> ActivateModifierAsync(
        Guid modifierId,
        Guid activatedByUserId,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var activeGame = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.GameNotActive
            );
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {activeGame.Id} FOR UPDATE""",
                cancellationToken
            );
        }

        if (!await _dbContext.Games.AsNoTracking().AnyAsync(
                x => x.Id == activeGame.Id && x.Status == GameStatusValue.Active && !x.IsDeleted,
                cancellationToken
            ))
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.GameNotActive
            );
        }

        var orderingRoundId = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == activeGame.Id
                    && x.Status == GameRoundStatusValue.AwaitingModifiers
            )
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!orderingRoundId.HasValue)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ModifierOrderingClosed
            );
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM game_rounds WHERE id = {orderingRoundId.Value} FOR UPDATE""",
                cancellationToken
            );
        }

        var orderingRound = await _dbContext.GameRounds.FirstOrDefaultAsync(
            x =>
                x.Id == orderingRoundId.Value
                && x.Status == GameRoundStatusValue.AwaitingModifiers,
            cancellationToken
        );
        if (orderingRound is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ModifierOrderingClosed
            );
        }

        if (await IsRoundTeamMemberAsync(
                activeGame.Id,
                orderingRound.Id,
                orderingRound.TeamId,
                activatedByUserId,
                cancellationToken
            ))
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ActiveTeamMember
            );
        }

        var enabledModifier = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == activeGame.Id && x.ModifierId == modifierId)
            .Select(x => new { x.EmergencyDisabledAtUtc, x.ModifierVersionId })
            .FirstOrDefaultAsync(cancellationToken);
        if (enabledModifier is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ModifierNotEnabled
            );
        }

        if (enabledModifier.EmergencyDisabledAtUtc.HasValue)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.EmergencyDisabled
            );
        }

        if (!enabledModifier.ModifierVersionId.HasValue)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.VersionBindingMissing);
        }

        var modifierDefinition = await _dbContext.ModifierDefinitionVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == enabledModifier.ModifierVersionId.Value
                    && x.ModifierId == modifierId,
                cancellationToken);
        if (modifierDefinition is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.VersionBindingMissing);
        }

        var conflictingActiveIds = await _dbContext.ModifierDefinitionVersionConflicts
            .AsNoTracking()
            .Where(x => x.ModifierVersionId == modifierDefinition.Id)
            .Select(x => x.ConflictingModifierId)
            .ToArrayAsync(cancellationToken);
        if (conflictingActiveIds.Length > 0)
        {
            var hasConflict = await _dbContext.GameModifierActivations.AnyAsync(
                x =>
                    x.GameId == activeGame.Id
                    && x.Status == GameModifierActivationStatusValue.Active
                    && conflictingActiveIds.Contains(x.ModifierId),
                cancellationToken
            );
            if (hasConflict)
            {
                return new ActivateGameModifierRepositoryResult(
                    ActivateGameModifierRepositoryStatus.ModifierConflictActive
                );
            }
        }

        if (modifierDefinition.MaxActivationsPerRound.HasValue)
        {
            var activationCount = await _dbContext.GameModifierActivations.CountAsync(
                x =>
                    x.GameId == activeGame.Id
                    && x.ModifierId == modifierId
                    && x.Status == GameModifierActivationStatusValue.Active,
                cancellationToken
            );
            if (activationCount >= modifierDefinition.MaxActivationsPerRound.Value)
            {
                return new ActivateGameModifierRepositoryResult(
                    ActivateGameModifierRepositoryStatus.ModifierLimitReached
                );
            }
        }

        var earnedPoints = await GetEarnedQuizPointsAsync(
            activeGame.Id,
            activatedByUserId,
            cancellationToken
        );
        var spentPoints = await GetSpentQuizPointsAsync(
            activeGame.Id,
            activatedByUserId,
            cancellationToken
        );
        if (earnedPoints - spentPoints < modifierDefinition.ActivationCost)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.InsufficientQuizPoints
            );
        }

        var activatedByDisplayName = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == activatedByUserId)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var activationEntityId = Guid.NewGuid();
        var behaviorV2 = ResolveBehaviorV2(modifierDefinition);
        var activation = new Data.Entities.GameModifierActivation
        {
            Id = activationEntityId,
            GameId = activeGame.Id,
            RoundId = orderingRound.Id,
            ModifierId = modifierId,
            ModifierVersionId = modifierDefinition.Id,
            ActivatedByUserId = activatedByUserId,
            InitiatedByUserId = initiatedByUserId,
            ActivationCostSnapshot = modifierDefinition.ActivationCost,
            DefinitionRevisionSnapshot = modifierDefinition.Revision,
            ModifierNameSnapshot = modifierDefinition.Name,
            ModifierDescriptionSnapshot = modifierDefinition.Description,
            ModifierCategorySnapshot = modifierDefinition.Category,
            ModifierIconEmojiSnapshot = modifierDefinition.IconEmoji,
            ActivationCommandSnapshot = modifierDefinition.ActivationCommand,
            NormalizedTagsSnapshot = modifierDefinition.NormalizedTags.ToArray(),
            BehaviorV2SnapshotJson = ModifierBehaviorV2Json.Serialize(behaviorV2),
            ActivatedAtUtc = now,
            Status = GameModifierActivationStatusValue.Active
        };
        _dbContext.GameModifierActivations.Add(activation);
        if (modifierDefinition.ActivationCost > 0)
        {
            var availableBefore = earnedPoints - spentPoints;
            _dbContext.GameQuizPointLedgerEntries.Add(
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = activeGame.Id,
                    UserId = activatedByUserId,
                    EntryType = GameQuizPointEntryTypeValue.ModifierPurchase,
                    PointsDelta = -modifierDefinition.ActivationCost,
                    ModifierActivationId = activationEntityId,
                    CreatedByUserId = initiatedByUserId,
                    AvailablePointsBefore = availableBefore,
                    AvailablePointsAfter = availableBefore - modifierDefinition.ActivationCost,
                    OccurredAtUtc = now
                }
            );
        }

        orderingRound.Version += 1;
        orderingRound.UpdatedAtUtc = now;

        var board = await _dbContext.GameBoards.FirstAsync(
            x => x.GameId == activeGame.Id,
            cancellationToken
        );
        board.Version += 1;

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new ActivateGameModifierRepositoryResult(
            ActivateGameModifierRepositoryStatus.Activated,
            activeGame.Id.ToString(),
            board.Version,
            new GameModifierActivationContract(
                activationEntityId,
                orderingRound.Id,
                orderingRound.Version,
                modifierId,
                modifierDefinition.Name,
                activatedByUserId.ToString(),
                string.IsNullOrWhiteSpace(activatedByDisplayName)
                    ? activatedByUserId.ToString()
                    : activatedByDisplayName,
                modifierDefinition.ActivationCost,
                now
            )
        );
    }

    public async Task<CancelGameModifierActivationRepositoryResult> CancelActivationAsync(
        CancelGameModifierActivationRepositoryInput input,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var activeGame = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.GameNotActive
            );
        }

        var activationRoundId = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.Id == input.ActivationId && x.GameId == activeGame.Id)
            .Select(x => (Guid?)x.RoundId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activationRoundId.HasValue)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.ActivationNotFound
            );
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {activeGame.Id} FOR UPDATE",
                cancellationToken
            );
        }

        var gameStillActive = await _dbContext.Games.AsNoTracking().AnyAsync(
            x => x.Id == activeGame.Id
                && x.Status == GameStatusValue.Active
                && !x.IsDeleted,
            cancellationToken
        );
        if (!gameStillActive)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.GameNotActive
            );
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM game_rounds WHERE id = {activationRoundId.Value} FOR UPDATE""",
                cancellationToken
            );
        }

        var activation = await _dbContext.GameModifierActivations
            .Include(x => x.Round)
            .FirstOrDefaultAsync(
                x => x.Id == input.ActivationId && x.GameId == activeGame.Id,
                cancellationToken
            );
        if (activation is null)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.ActivationNotFound
            );
        }

        if (!input.IsAdmin && activation.ActivatedByUserId != input.CancelledByUserId)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.Forbidden
            );
        }

        var normalizedReason = string.IsNullOrWhiteSpace(input.Reason)
            ? null
            : input.Reason.Trim();
        if (input.IsAdmin && (normalizedReason is null || normalizedReason.Length > 1000))
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.ReasonRequired
            );
        }

        if (activation.Status == GameModifierActivationStatusValue.Cancelled)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.Cancelled,
                activeGame.Id.ToString(),
                ActivationId: activation.Id,
                ActivatedByUserId: activation.ActivatedByUserId,
                ModifierName: activation.ModifierNameSnapshot,
                RefundedQuizPoints: activation.RefundAmount,
                StateChanged: false,
                RoundVersion: activation.Round.Version
            );
        }

        var canCancelInCurrentState = activation.Status == GameModifierActivationStatusValue.Active
            && (input.IsAdmin
                ? activation.Round.Status is GameRoundStatusValue.AwaitingModifiers
                    or GameRoundStatusValue.Preparing
                : activation.Round.Status == GameRoundStatusValue.AwaitingModifiers);
        if (!canCancelInCurrentState)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.InvalidRoundState
            );
        }

        if (activation.Round.Version != input.ExpectedRoundVersion)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.StaleVersion,
                RoundVersion: activation.Round.Version
            );
        }

        var availableBeforeRefund = 0L;
        if (activation.ActivationCostSnapshot > 0)
        {
            var earnedPoints = await GetEarnedQuizPointsAsync(
                activeGame.Id,
                activation.ActivatedByUserId,
                cancellationToken
            );
            var spentPoints = await GetSpentQuizPointsAsync(
                activeGame.Id,
                activation.ActivatedByUserId,
                cancellationToken
            );
            availableBeforeRefund = earnedPoints - spentPoints;
        }

        var now = DateTime.UtcNow;
        activation.Status = GameModifierActivationStatusValue.Cancelled;
        activation.ArchivedAtUtc = now;
        activation.CancelledAtUtc = now;
        activation.CancelledByUserId = input.CancelledByUserId;
        activation.CancellationReason = input.IsAdmin ? normalizedReason : null;
        activation.RefundAmount = activation.ActivationCostSnapshot;
        if (activation.RefundAmount > 0)
        {
            _dbContext.GameQuizPointLedgerEntries.Add(
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = activeGame.Id,
                    UserId = activation.ActivatedByUserId,
                    EntryType = GameQuizPointEntryTypeValue.ModifierRefund,
                    PointsDelta = activation.RefundAmount,
                    ModifierActivationId = activation.Id,
                    CreatedByUserId = input.CancelledByUserId,
                    Reason = normalizedReason,
                    AvailablePointsBefore = availableBeforeRefund,
                    AvailablePointsAfter = availableBeforeRefund + activation.RefundAmount,
                    OccurredAtUtc = now
                }
            );
        }
        activation.Round.Version += 1;
        activation.Round.UpdatedAtUtc = now;

        var board = await _dbContext.GameBoards.FirstAsync(
            x => x.GameId == activeGame.Id,
            cancellationToken
        );
        board.Version += 1;

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new CancelGameModifierActivationRepositoryResult(
            CancelGameModifierActivationRepositoryStatus.Cancelled,
            activeGame.Id.ToString(),
            board.Version,
            activation.Id,
            activation.ActivatedByUserId,
            activation.ModifierNameSnapshot,
            activation.RefundAmount,
            StateChanged: true,
            RoundVersion: activation.Round.Version
        );
    }

    private async Task<long> GetEarnedQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.UserId == userId
                && (x.EntryType == GameQuizPointEntryTypeValue.QuizReward
                    || x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment))
            .SumAsync(x => (long)x.PointsDelta, cancellationToken);
    }

    private async Task<long> GetSpentQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return -await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.UserId == userId
                && (x.EntryType == GameQuizPointEntryTypeValue.ModifierPurchase
                    || x.EntryType == GameQuizPointEntryTypeValue.ModifierRefund))
            .SumAsync(x => (long)x.PointsDelta, cancellationToken);
    }

    private static string? ResolveBlockedReason(
        bool emergencyDisabled,
        bool orderingClosed,
        bool isActiveTeamMember,
        bool limitReached,
        bool hasConflict,
        int availablePoints,
        int activationCost
    )
    {
        if (emergencyDisabled)
        {
            return "emergency_disabled";
        }

        if (orderingClosed)
        {
            return "ordering_closed";
        }

        if (isActiveTeamMember)
        {
            return "active_team_member";
        }

        if (limitReached)
        {
            return "limit_reached";
        }

        if (hasConflict)
        {
            return "conflict_active";
        }

        if (availablePoints < activationCost)
        {
            return "insufficient_points";
        }

        return null;
    }

    private async Task<bool> IsRoundTeamMemberAsync(
        Guid gameId,
        Guid roundId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        if (await _dbContext.GameRoundParticipants
                .AsNoTracking()
                .AnyAsync(
                    participant => participant.RoundId == roundId && participant.UserId == userId,
                    cancellationToken
                ))
        {
            return true;
        }

        return await _dbContext.GameTeamMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.GameId == gameId
                    && member.TeamId == teamId
                    && member.UserId == userId
                    && member.LeftAtUtc == null,
                cancellationToken
            );
    }

    public async Task<CreateGameModifierRepositoryResult> CreateModifierAsync(
        CreateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await AcquireCatalogMutationLockAsync(cancellationToken);
        var resolvedActor = await ResolveActorAsync(actor, cancellationToken);
        if (resolvedActor is null)
        {
            return new(CreateGameModifierRepositoryStatus.InvalidRequest);
        }
        var conflictDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => input.ConflictingModifierIds.Contains(x.Id) && !x.IsArchived)
            .ToArrayAsync(cancellationToken);
        if (conflictDefinitions.Length != input.ConflictingModifierIds.Distinct().Count()
            || conflictDefinitions.Any(x => x.CurrentVersion is null))
        {
            return new(CreateGameModifierRepositoryStatus.InvalidRequest);
        }
        if (await IsAnyContentLockedAsync(input.ConflictingModifierIds, cancellationToken))
        {
            return new(CreateGameModifierRepositoryStatus.CompatibilityLocked);
        }

        var now = DateTime.UtcNow;
        var entity = new ModifierDefinition
        {
            Id = Guid.NewGuid(),
            IsArchived = false,
            CreatedByUserId = resolvedActor.UserId,
            CreatedAtUtc = now
        };

        _dbContext.ModifierDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var conflictNames = conflictDefinitions.ToDictionary(x => x.Id, x => x.CurrentVersion!.Name);
        var version = ModifierVersionProjector.CreateVersion(
            entity, 1, input, resolvedActor, ModifierVersionChangeTypeValue.Created,
            null, now, conflictNames);
        _dbContext.ModifierDefinitionVersions.Add(version);
        ModifierVersionProjector.ApplyCurrentProjection(entity, version);

        var changes = new List<ModifierCatalogChangedItem>
        {
            new(entity.Id, version.Revision, false)
        };
        var cascadingIds = conflictDefinitions.Select(x => x.Id).ToArray();
        var currentVersionIds = conflictDefinitions.Select(x => x.CurrentVersionId!.Value).ToArray();
        var currentConflictRows = await _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => currentVersionIds.Contains(x.ModifierVersionId))
            .Select(x => new
            {
                ModifierId = x.ModifierVersion.ModifierId,
                ConflictsWithModifierId = x.ConflictingModifierId
            })
            .ToArrayAsync(cancellationToken);
        var cascadeConflictIds = currentConflictRows
            .SelectMany(x => new[] { x.ModifierId, x.ConflictsWithModifierId })
            .Append(entity.Id).Distinct().ToArray();
        var cascadeNames = await _dbContext.ModifierDefinitions.AsNoTracking()
            .Where(x => cascadeConflictIds.Contains(x.Id) && x.CurrentVersionId != null)
            .Select(x => new { x.Id, Name = x.CurrentVersion!.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        cascadeNames[entity.Id] = version.Name;
        foreach (var other in conflictDefinitions.OrderBy(x => x.Id))
        {
            var existingIds = currentConflictRows
                .Where(x => x.ModifierId == other.Id || x.ConflictsWithModifierId == other.Id)
                .Select(x => x.ModifierId == other.Id ? x.ConflictsWithModifierId : x.ModifierId)
                .Distinct().Order().ToArray();
            var nextIds = existingIds.Append(entity.Id).Distinct().Order().ToArray();
            var cascade = CreateVersionForCurrentContent(
                other, nextIds, resolvedActor, ModifierVersionChangeTypeValue.CompatibilityCascade,
                entity.Id, null, now, cascadeNames);
            changes.Add(new(other.Id, cascade.Revision, other.IsArchived));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new CreateGameModifierRepositoryResult(
            CreateGameModifierRepositoryStatus.Created,
            MapDefinition(version, input.ConflictingModifierIds),
            changes);
    }

    public async Task<UpdateGameModifierRepositoryResult> UpdateModifierAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await AcquireCatalogMutationLockAsync(cancellationToken);
        var resolvedActor = await ResolveActorAsync(actor, cancellationToken);
        if (resolvedActor is null)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
        }

        var entity = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .FirstOrDefaultAsync(
            x => x.Id == modifierId,
            cancellationToken
        );
        if (entity is null)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
        }

        if (entity.IsArchived)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.Archived);
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM modifier_definitions WHERE id = {modifierId} FOR UPDATE""",
                cancellationToken
            );
        }

        if (entity.CurrentVersion is null)
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.VersionBindingMissing);
        }
        if (entity.CurrentVersion.Revision != input.ExpectedRevision)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.Stale);
        }

        var existingConflictIds = await GetCurrentConflictIdsAsync(modifierId, cancellationToken);
        var requestedIds = input.ConflictingModifierIds.Distinct().Order().ToArray();
        var requestedDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => requestedIds.Contains(x.Id) && !x.IsArchived)
            .ToArrayAsync(cancellationToken);
        if (requestedDefinitions.Length != requestedIds.Length)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
        }
        var archivedExistingDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => existingConflictIds.Contains(x.Id) && x.IsArchived)
            .ToArrayAsync(cancellationToken);
        var desiredIds = requestedIds.Concat(archivedExistingDefinitions.Select(x => x.Id))
            .Distinct().Order().ToArray();
        var incomingContent = ModifierVersionProjector.ContentOf(input) with
        {
            ConflictingModifierIds = desiredIds
        };
        if (requestedDefinitions.Any(x => x.CurrentVersion is null)
            || archivedExistingDefinitions.Any(x => x.CurrentVersion is null))
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.VersionBindingMissing);
        }
        var existingContent = ModifierVersionProjector.ContentOf(entity.CurrentVersion, existingConflictIds);
        if (ModifierVersionProjector.ContentEquals(existingContent, incomingContent))
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.Unchanged,
                 MapDefinition(entity.CurrentVersion, existingConflictIds));
        }

        var desiredDefinitions = requestedDefinitions.Concat(archivedExistingDefinitions).ToArray();

        var changedCompatibilityIds = existingConflictIds
            .Except(desiredIds).Concat(desiredIds.Except(existingConflictIds)).Distinct().ToArray();
        var affectedIds = changedCompatibilityIds.Append(modifierId).Distinct().ToArray();
        var lockedIds = await GetContentLockedIdsAsync(affectedIds, cancellationToken);
        if (lockedIds.Contains(modifierId))
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.ContentLocked);
        }

        if (lockedIds.Count > 0)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.CompatibilityLocked);
        }

        var now = DateTime.UtcNow;
        var desiredNames = desiredDefinitions.ToDictionary(x => x.Id, x => x.CurrentVersion!.Name);
        var targetVersion = ModifierVersionProjector.CreateVersion(
            entity, entity.CurrentVersion.Revision + 1, incomingContent, resolvedActor,
            ModifierVersionChangeTypeValue.Edited, null, now, desiredNames);
        targetVersion.ChangedFields = ModifierVersionProjector.ChangedFields(
            existingContent, incomingContent);
        _dbContext.ModifierDefinitionVersions.Add(targetVersion);
        ModifierVersionProjector.ApplyCurrentProjection(entity, targetVersion);

        var changes = new List<ModifierCatalogChangedItem>
        {
            new(modifierId, targetVersion.Revision, false)
        };
        var affectedDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => changedCompatibilityIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);
        if (affectedDefinitions.Any(x => x.CurrentVersion is null))
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.VersionBindingMissing);
        }
        var affectedVersionIds = affectedDefinitions.Select(x => x.CurrentVersionId!.Value).ToArray();
        var currentConflictRows = await _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => affectedVersionIds.Contains(x.ModifierVersionId))
            .Select(x => new
            {
                ModifierId = x.ModifierVersion.ModifierId,
                ConflictsWithModifierId = x.ConflictingModifierId
            })
            .ToArrayAsync(cancellationToken);
        var cascadeConflictIds = currentConflictRows
            .SelectMany(x => new[] { x.ModifierId, x.ConflictsWithModifierId })
            .Append(modifierId).Distinct().ToArray();
        var cascadeNames = await _dbContext.ModifierDefinitions.AsNoTracking()
            .Where(x => cascadeConflictIds.Contains(x.Id) && x.CurrentVersionId != null)
            .Select(x => new { x.Id, Name = x.CurrentVersion!.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        cascadeNames[modifierId] = targetVersion.Name;
        foreach (var other in affectedDefinitions.OrderBy(x => x.Id))
        {
            var otherCurrentIds = currentConflictRows
                .Where(x => x.ModifierId == other.Id || x.ConflictsWithModifierId == other.Id)
                .Select(x => x.ModifierId == other.Id ? x.ConflictsWithModifierId : x.ModifierId)
                .Distinct().Order().ToArray();
            var nextIds = desiredIds.Contains(other.Id)
                ? otherCurrentIds.Append(modifierId).Distinct().Order().ToArray()
                : otherCurrentIds.Where(x => x != modifierId).Order().ToArray();
            var cascade = CreateVersionForCurrentContent(
                other, nextIds, resolvedActor, ModifierVersionChangeTypeValue.CompatibilityCascade,
                modifierId, null, now, cascadeNames);
            changes.Add(new(other.Id, cascade.Revision, other.IsArchived));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new UpdateGameModifierRepositoryResult(
            UpdateGameModifierRepositoryStatus.Updated,
            MapDefinition(targetVersion, desiredIds),
            changes
        );
    }

    public async Task<ArchiveGameModifierRepositoryStatus> ArchiveModifierAsync(
        Guid modifierId,
        int expectedRevision,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await AcquireCatalogMutationLockAsync(cancellationToken);
        var resolvedActor = await ResolveActorAsync(actor, cancellationToken);
        if (resolvedActor is null)
        {
            return ArchiveGameModifierRepositoryStatus.NotFound;
        }
        var entity = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .FirstOrDefaultAsync(
            x => x.Id == modifierId,
            cancellationToken
        );
        if (entity is null || entity.IsArchived)
        {
            return ArchiveGameModifierRepositoryStatus.NotFound;
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM modifier_definitions WHERE id = {modifierId} FOR UPDATE""",
                cancellationToken
            );
        }

        if (await IsContentLockedAsync(modifierId, cancellationToken))
        {
            return ArchiveGameModifierRepositoryStatus.ContentLocked;
        }
        if (entity.CurrentVersion is null)
        {
            return ArchiveGameModifierRepositoryStatus.VersionBindingMissing;
        }
        if (entity.CurrentVersion.Revision != expectedRevision)
        {
            return ArchiveGameModifierRepositoryStatus.Stale;
        }

        entity.IsArchived = true;
        entity.ArchivedAtUtc = DateTime.UtcNow;
        entity.ArchivedByUserId = resolvedActor.UserId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ArchiveGameModifierRepositoryStatus.Archived;
    }

    public async Task<EmergencyDisableGameModifierRepositoryResult> EmergencyDisableModifierAsync(
        EmergencyDisableGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var activeGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.GameNotActive
            );
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {activeGameId.Value} FOR UPDATE""",
                cancellationToken
            );
        }

        if (!await _dbContext.Games.AsNoTracking().AnyAsync(
                x =>
                    x.Id == activeGameId.Value
                    && x.Status == GameStatusValue.Active
                    && !x.IsDeleted,
                cancellationToken
            ))
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.GameNotActive
            );
        }

        var enabledModifier = await _dbContext.GameEnabledModifiers.FirstOrDefaultAsync(
            x => x.GameId == activeGameId.Value && x.ModifierId == input.ModifierId,
            cancellationToken
        );
        if (enabledModifier is null)
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.ModifierNotEnabled
            );
        }

        if (enabledModifier.EmergencyDisabledAtUtc.HasValue)
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.AlreadyDisabled
            );
        }

        enabledModifier.EmergencyDisabledAtUtc = DateTime.UtcNow;
        enabledModifier.EmergencyDisabledByUserId = input.DisabledByUserId;
        enabledModifier.EmergencyDisableReason = input.Reason;

        var board = await _dbContext.GameBoards.FirstAsync(
            x => x.GameId == activeGameId.Value,
            cancellationToken
        );
        board.Version += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new EmergencyDisableGameModifierRepositoryResult(
            EmergencyDisableGameModifierRepositoryStatus.Disabled,
            activeGameId.Value.ToString(),
            board.Version,
            input.ModifierId
        );
    }

    public async Task<ModifierHistoryPage<ModifierHistorySummary>> GetHistoryAsync(
        ModifierHistoryQuery query,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadHistoryAsync(query, cancellationToken);

    public async Task<ModifierHistoryPage<ModifierVersionSummary>?> GetVersionsAsync(
        Guid modifierId,
        ModifierVersionQuery query,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadVersionsAsync(modifierId, query, cancellationToken);

    public async Task<ModifierVersionDetail?> GetVersionAsync(
        Guid modifierId,
        int revision,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadVersionAsync(modifierId, revision, cancellationToken);

    public async Task<ModifierHistoryPage<ModifierVersionGameSummary>?> GetVersionGamesAsync(
        Guid modifierId,
        int revision,
        ModifierVersionQuery query,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadVersionGamesAsync(modifierId, revision, query, cancellationToken);

    private ModifierDefinitionVersion CreateVersionForCurrentContent(
        ModifierDefinition definition,
        IReadOnlyList<Guid> conflictIds,
        ModifierChangeActor actor,
        string changeType,
        Guid? cascadeSourceModifierId,
        string? note,
        DateTime now,
        Dictionary<Guid, string> allNames)
    {
        var conflictNames = conflictIds.ToDictionary(x => x, x => allNames[x]);
        var current = definition.CurrentVersion
            ?? throw new InvalidOperationException("Current modifier version must be loaded.");
        var content = ModifierVersionProjector.ContentOf(current, conflictIds, note);
        var version = ModifierVersionProjector.CreateVersion(
            definition, current.Revision + 1, content, actor, changeType,
            cascadeSourceModifierId, now, conflictNames);
        _dbContext.ModifierDefinitionVersions.Add(version);
        ModifierVersionProjector.ApplyCurrentProjection(definition, version);
        return version;
    }

    private async Task<ModifierChangeActor?> ResolveActorAsync(
        ModifierChangeActor actor,
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return actor.UserId != Guid.Empty ? actor : null;
        }
        var name = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == actor.UserId && x.IsActive)
            .Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(name) ? null : new ModifierChangeActor(actor.UserId, name);
    }

    private async Task AcquireCatalogMutationLockAsync(CancellationToken cancellationToken)
    {
        await ModifierCatalogTransactionLock.AcquireAsync(_dbContext, cancellationToken);
    }

    private Task<Guid[]> GetCurrentConflictIdsAsync(Guid modifierId, CancellationToken cancellationToken) =>
        _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => x.ModifierVersion.ModifierId == modifierId
                && x.ModifierVersion.Modifier.CurrentVersionId == x.ModifierVersionId)
            .Select(x => x.ConflictingModifierId)
            .OrderBy(x => x).ToArrayAsync(cancellationToken);

    private async Task<HashSet<Guid>> GetContentLockedIdsAsync(
        IReadOnlyList<Guid> modifierIds,
        CancellationToken cancellationToken) =>
        (await _dbContext.GameEnabledModifiers.AsNoTracking()
            .Where(x => modifierIds.Contains(x.ModifierId)
                && x.Game.Status == GameStatusValue.Active && !x.Game.IsDeleted)
            .Select(x => x.ModifierId).Distinct().ToArrayAsync(cancellationToken)).ToHashSet();

    private async Task<bool> IsAnyContentLockedAsync(
        IReadOnlyList<Guid> modifierIds,
        CancellationToken cancellationToken) =>
        (await GetContentLockedIdsAsync(modifierIds, cancellationToken)).Count > 0;

    private Task<bool> IsContentLockedAsync(Guid modifierId, CancellationToken cancellationToken)
    {
        return _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .AnyAsync(
                x => x.ModifierId == modifierId
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted,
                cancellationToken
            );
    }

    private static GameModifierDefinition MapDefinition(
        ModifierDefinitionVersion x,
        IReadOnlyList<Guid> conflictingModifierIds,
        bool isLockedByActiveGame = false)
    {
        return new GameModifierDefinition(
            x.ModifierId, x.Category, x.Name, x.Description, x.ActivationCost,
            new GameModifierActivationLimit(x.MaxActivationsPerRound),
            conflictingModifierIds, x.IconEmoji, x.ActivationCommand,
            isLockedByActiveGame, x.Revision, x.NormalizedTags,
            ResolveBehaviorV2(x));
    }

    private static ModifierBehaviorV2 ResolveBehaviorV2(ModifierDefinitionVersion version)
    {
        return ModifierBehaviorV2Json.Deserialize(version.BehaviorV2Json);
    }

}
