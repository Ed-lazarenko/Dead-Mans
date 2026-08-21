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
    )
    {
        var definitions = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.ActivationCost)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

        var definitionIds = definitions.Select(x => x.Id).ToArray();
        var lockedDefinitionIds = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(
                x => definitionIds.Contains(x.ModifierId)
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted
            )
            .Select(x => x.ModifierId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var lockedDefinitionIdSet = lockedDefinitionIds.ToHashSet();
        var conflictRows = await _dbContext.ModifierConflicts
            .AsNoTracking()
            .Where(
                x =>
                    definitionIds.Contains(x.ModifierId)
                    || definitionIds.Contains(x.ConflictsWithModifierId)
            )
            .ToArrayAsync(cancellationToken);
        var conflictLookup = definitionIds.ToDictionary(
            id => id,
            id => conflictRows
                .Where(x => x.ModifierId == id || x.ConflictsWithModifierId == id)
                .Select(x => x.ModifierId == id ? x.ConflictsWithModifierId : x.ModifierId)
                .Where(definitionIds.Contains)
                .Distinct()
                .OrderBy(id => id)
                .ToArray()
        );

        return definitions
            .Select(
                x => MapDefinition(
                    x,
                    conflictLookup.GetValueOrDefault(x.Id) ?? Array.Empty<Guid>(),
                    lockedDefinitionIdSet.Contains(x.Id)
                )
            )
            .ToArray();
    }

    public async Task<GameModifierState?> GetStateAsync(
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
            return null;
        }

        var enabledRows = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == activeGame.Id && !x.ModifierDefinition.IsArchived)
            .Select(
                x => new
                {
                    Definition = x.ModifierDefinition,
                    x.EmergencyDisabledAtUtc
                }
            )
            .OrderBy(x => x.Definition.ActivationCost)
            .ThenBy(x => x.Definition.Name)
            .ToArrayAsync(cancellationToken);
        var enabledDefinitions = enabledRows.Select(x => x.Definition).ToArray();
        var enabledDefinitionIds = enabledDefinitions.Select(x => x.Id).ToArray();

        var conflictRows = await _dbContext.ModifierConflicts
            .AsNoTracking()
            .Where(
                x =>
                    enabledDefinitionIds.Contains(x.ModifierId)
                    || enabledDefinitionIds.Contains(x.ConflictsWithModifierId)
            )
            .ToArrayAsync(cancellationToken);
        var conflictLookup = enabledDefinitionIds.ToDictionary(
            id => id,
            id => conflictRows
                .Where(x => x.ModifierId == id || x.ConflictsWithModifierId == id)
                .Select(x => x.ModifierId == id ? x.ConflictsWithModifierId : x.ModifierId)
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
                    x.ModifierDefinition.Name,
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
            .OrderByDescending(x => x.StartedAtUtc)
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
                var definition = enabledRow.Definition;
                var modifier = MapDefinition(
                    definition,
                    conflictLookup.GetValueOrDefault(definition.Id) ?? Array.Empty<Guid>(),
                    isLockedByActiveGame: true
                );
                var count = activationCounts.GetValueOrDefault(definition.Id);
                var limit = modifier.ActivationLimit.Count;
                var hasLimitReached = limit.HasValue && count >= limit.Value;
                var conflicts = conflictLookup.GetValueOrDefault(definition.Id) ?? Array.Empty<Guid>();
                var hasConflict = conflicts.Any(activeModifierIds.Contains);
                var isActive = activeModifierIds.Contains(definition.Id);
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

        return new GameModifierState(
            activeGame.Id,
            availablePoints,
            earnedPoints,
            spentPoints,
            orderingOpen,
            activeModifiers,
            availableModifiers
        );
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
        var earnedFromQuestions = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == activeGameId.Value
                    && x.AnsweredAtUtc.HasValue
                    && x.AwardedPoints.HasValue
                    && (x.AnsweredForUserId.HasValue || x.AnsweredByUserId.HasValue)
            )
            .GroupBy(x => x.AnsweredForUserId ?? x.AnsweredByUserId!.Value)
            .Select(
                x => new
                {
                    UserId = x.Key,
                    Points = x.Sum(item => (long)(item.AwardedPoints ?? 0))
                }
            )
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        var earnedFromManualAwards = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == activeGameId.Value && playerIds.Contains(x.AwardedToUserId))
            .GroupBy(x => x.AwardedToUserId)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => (long)item.Points) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        var spentPoints = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.GameId == activeGameId.Value && playerIds.Contains(x.ActivatedByUserId))
            .GroupBy(x => x.ActivatedByUserId)
            .Select(
                x =>
                    new
                    {
                        UserId = x.Key,
                        Points = x.Sum(
                            item => (long)item.ActivationCostSnapshot - item.RefundAmount
                        )
                    }
            )
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        var playerBalances = players
            .Select(player =>
            {
                var rawEarned =
                    earnedFromQuestions.GetValueOrDefault(player.Id)
                    + earnedFromManualAwards.GetValueOrDefault(player.Id);
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
                    x.ModifierDefinition.Name,
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

        var orderingRoundId = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == activeGame.Id
                    && x.Status == GameRoundStatusValue.AwaitingModifiers
            )
            .OrderByDescending(x => x.StartedAtUtc)
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

        var modifierDefinition = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => x.Id == modifierId && !x.IsArchived)
            .FirstOrDefaultAsync(cancellationToken);
        if (modifierDefinition is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.NotFound
            );
        }

        var enabledModifier = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == activeGame.Id && x.ModifierId == modifierId)
            .Select(x => new { x.EmergencyDisabledAtUtc })
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

        var conflictingActiveIds = await _dbContext.ModifierConflicts
            .AsNoTracking()
            .Where(
                x =>
                    x.ModifierId == modifierId
                    || x.ConflictsWithModifierId == modifierId
            )
            .Select(
                x =>
                    x.ModifierId == modifierId
                        ? x.ConflictsWithModifierId
                        : x.ModifierId
            )
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
        _dbContext.GameModifierActivations.Add(
            new Data.Entities.GameModifierActivation
            {
                Id = activationEntityId,
                GameId = activeGame.Id,
                RoundId = orderingRound.Id,
                ModifierId = modifierId,
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
            }
        );

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
                $"""SELECT 1 FROM game_rounds WHERE id = {activationRoundId.Value} FOR UPDATE""",
                cancellationToken
            );
        }

        var activation = await _dbContext.GameModifierActivations
            .Include(x => x.ModifierDefinition)
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
                ModifierName: activation.ModifierDefinition.Name,
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

        var now = DateTime.UtcNow;
        activation.Status = GameModifierActivationStatusValue.Cancelled;
        activation.ArchivedAtUtc = now;
        activation.CancelledAtUtc = now;
        activation.CancelledByUserId = input.CancelledByUserId;
        activation.CancellationReason = input.IsAdmin ? normalizedReason : null;
        activation.RefundAmount = activation.ActivationCostSnapshot;
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
            activation.ModifierDefinition.Name,
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
        var answeredPoints = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == gameId
                    && x.AwardedPoints.HasValue
                    && (
                        x.AnsweredForUserId == userId
                        || (x.AnsweredForUserId == null && x.AnsweredByUserId == userId)
                    )
            )
            .SumAsync(x => (long)(x.AwardedPoints ?? 0), cancellationToken);
        var manualPoints = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == gameId && x.AwardedToUserId == userId)
            .SumAsync(x => (long)x.Points, cancellationToken);
        return answeredPoints + manualPoints;
    }

    private async Task<long> GetSpentQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.GameId == gameId && x.ActivatedByUserId == userId)
            .SumAsync(
                x => (long)x.ActivationCostSnapshot - x.RefundAmount,
                cancellationToken
            );
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

    public async Task<GameModifierDefinition?> CreateModifierAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var now = DateTime.UtcNow;
        var entity = new ModifierDefinition
        {
            Id = Guid.NewGuid(),
            Revision = 1,
            Name = input.Name,
            Description = input.Description,
            Category = input.Category,
            IconEmoji = input.IconEmoji,
            ActivationCommand = input.ActivationCommand,
            ActivationCost = input.ActivationCost,
            MaxActivationsPerRound = ToPerGameLimit(input.ActivationLimit),
            NormalizedTags = (input.NormalizedTags ?? []).ToArray(),
            BehaviorV2Json = ModifierBehaviorV2Json.Serialize(input.BehaviorV2),
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.ModifierDefinitions.Add(entity);
        AddConflictRows(entity.Id, input.ConflictingModifierIds);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return MapDefinition(entity, input.ConflictingModifierIds);
    }

    public async Task<UpdateGameModifierRepositoryResult> UpdateModifierAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var entity = await _dbContext.ModifierDefinitions.FirstOrDefaultAsync(
            x => x.Id == modifierId && !x.IsArchived,
            cancellationToken
        );
        if (entity is null)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
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
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.ContentLocked);
        }

        entity.Name = input.Name;
        entity.Revision += 1;
        entity.Description = input.Description;
        entity.Category = input.Category;
        entity.ActivationCost = input.ActivationCost;
        entity.MaxActivationsPerRound = ToPerGameLimit(input.ActivationLimit);
        entity.NormalizedTags = (input.NormalizedTags ?? entity.NormalizedTags).ToArray();
        entity.BehaviorV2Json = ModifierBehaviorV2Json.Serialize(input.BehaviorV2);
        entity.IconEmoji = input.IconEmoji;
        entity.ActivationCommand = input.ActivationCommand;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var existingConflicts = await _dbContext.ModifierConflicts
            .Where(x => x.ModifierId == modifierId || x.ConflictsWithModifierId == modifierId)
            .ToArrayAsync(cancellationToken);
        _dbContext.ModifierConflicts.RemoveRange(existingConflicts);
        AddConflictRows(modifierId, input.ConflictingModifierIds);

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new UpdateGameModifierRepositoryResult(
            UpdateGameModifierRepositoryStatus.Updated,
            MapDefinition(entity, input.ConflictingModifierIds)
        );
    }

    public async Task<ArchiveGameModifierRepositoryStatus> ArchiveModifierAsync(
        Guid modifierId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var entity = await _dbContext.ModifierDefinitions.FirstOrDefaultAsync(
            x => x.Id == modifierId && !x.IsArchived,
            cancellationToken
        );
        if (entity is null)
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

        entity.IsArchived = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
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
        ModifierDefinition x,
        IReadOnlyList<Guid> conflictingModifierIds,
        bool isLockedByActiveGame = false
    )
    {
        return new GameModifierDefinition(
            x.Id,
            x.Category,
            x.Name,
            x.Description,
            x.ActivationCost,
            new GameModifierActivationLimit(x.MaxActivationsPerRound),
            conflictingModifierIds,
            x.IconEmoji,
            x.ActivationCommand,
            isLockedByActiveGame,
            x.Revision,
            x.NormalizedTags,
            ResolveBehaviorV2(x)
        );
    }

    private static ModifierBehaviorV2 ResolveBehaviorV2(ModifierDefinition definition)
    {
        return ModifierBehaviorV2Json.Deserialize(definition.BehaviorV2Json);
    }

    private void AddConflictRows(Guid modifierId, IReadOnlyList<Guid> conflictingModifierIds)
    {
        var rows = conflictingModifierIds
            .Where(id => id != Guid.Empty && id != modifierId)
            .Select(id => NormalizeConflictPair(modifierId, id))
            .Distinct()
            .Select(pair => new ModifierConflict
            {
                ModifierId = pair.Left,
                ConflictsWithModifierId = pair.Right
            });

        _dbContext.ModifierConflicts.AddRange(rows);
    }

    private static (Guid Left, Guid Right) NormalizeConflictPair(Guid left, Guid right)
    {
        return left.CompareTo(right) <= 0 ? (left, right) : (right, left);
    }

    private static int? ToPerGameLimit(GameModifierActivationLimit activationLimit)
    {
        return activationLimit.Count;
    }

}
