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

public sealed partial class DbGameModifierRepository : IGameModifierRepository
{
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

}
