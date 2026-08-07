using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
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
            .Select(x => MapDefinition(x, conflictLookup.GetValueOrDefault(x.Id) ?? Array.Empty<Guid>()))
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

        var enabledDefinitions = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == activeGame.Id && !x.ModifierDefinition.IsArchived)
            .Select(x => x.ModifierDefinition)
            .OrderBy(x => x.ActivationCost)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
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
            .Where(x => x.GameId == activeGame.Id && x.ArchivedAtUtc == null)
            .OrderByDescending(x => x.ActivatedAtUtc)
            .Select(
                x => new
                {
                    x.Id,
                    x.ModifierId,
                    x.ModifierDefinition.Name,
                    x.ActivatedByUserId,
                    x.ActivationCostSnapshot,
                    CurrentActivationCost = x.ModifierDefinition.ActivationCost,
                    x.ActivatedAtUtc
                }
            )
            .ToArrayAsync(cancellationToken);
        var activeModifierIds = activeRows.Select(x => x.ModifierId).ToHashSet();
        var activationCounts = activeRows
            .GroupBy(x => x.ModifierId)
            .ToDictionary(x => x.Key, x => x.Count());

        var orderingOpen = await _dbContext.GameRounds.AnyAsync(
            x =>
                x.GameId == activeGame.Id
                && x.Status == GameRoundStatusValue.AwaitingModifiers,
            cancellationToken
        );

        var earnedPoints = await GetEarnedQuizPointsAsync(activeGame.Id, userId, cancellationToken);
        var spentPoints = await GetSpentQuizPointsAsync(activeGame.Id, userId, cancellationToken);
        var availablePoints = Math.Max(0, earnedPoints - spentPoints);
        var activatedByUserIds = activeRows.Select(x => x.ActivatedByUserId).Distinct().ToArray();
        var activatedByDisplayNames = await _dbContext.Users
            .AsNoTracking()
            .Where(x => activatedByUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

        var activeModifiers = activeRows
            .Select(
                x => new GameModifierActivationContract(
                    x.Id,
                    x.ModifierId,
                    x.Name,
                    x.ActivatedByUserId.ToString(),
                    activatedByDisplayNames.GetValueOrDefault(x.ActivatedByUserId)
                        ?? x.ActivatedByUserId.ToString(),
                    ResolveActivationCostSnapshot(x.ActivationCostSnapshot, x.CurrentActivationCost),
                    x.ActivatedAtUtc
                )
            )
            .ToArray();

        var availableModifiers = enabledDefinitions
            .Select(definition =>
            {
                var modifier = MapDefinition(
                    definition,
                    conflictLookup.GetValueOrDefault(definition.Id) ?? Array.Empty<Guid>()
                );
                var count = activationCounts.GetValueOrDefault(definition.Id);
                var limit = modifier.ActivationLimit.Count;
                var hasLimitReached = limit.HasValue && count >= limit.Value;
                var conflicts = conflictLookup.GetValueOrDefault(definition.Id) ?? Array.Empty<Guid>();
                var hasConflict = conflicts.Any(activeModifierIds.Contains);
                var isActive = activeModifierIds.Contains(definition.Id);
                var blockedReason = ResolveBlockedReason(
                    !orderingOpen,
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
                    limit
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

    public async Task<IReadOnlyList<GameModifierAdminPlayer>> GetAdminPlayersAsync(
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
            return Array.Empty<GameModifierAdminPlayer>();
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
            return Array.Empty<GameModifierAdminPlayer>();
        }

        var playerIds = players.Select(x => x.Id).ToArray();
        var earnedFromQuestions = await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == activeGameId.Value
                    && x.AnsweredAtUtc.HasValue
                    && x.AwardedPoints.HasValue
                    && (x.AnsweredForUserId.HasValue || x.AnsweredByUserId.HasValue)
            )
            .GroupBy(x => x.AnsweredForUserId ?? x.AnsweredByUserId!.Value)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => item.AwardedPoints ?? 0) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        var earnedFromManualAwards = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == activeGameId.Value && playerIds.Contains(x.AwardedToUserId))
            .GroupBy(x => x.AwardedToUserId)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => item.Points) })
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
                            item => item.ActivationCostSnapshot > 0
                                ? item.ActivationCostSnapshot
                                : item.ModifierDefinition.ActivationCost
                        )
                    }
            )
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        return players
            .Select(player =>
            {
                var earned =
                    earnedFromQuestions.GetValueOrDefault(player.Id)
                    + earnedFromManualAwards.GetValueOrDefault(player.Id);
                var spent = spentPoints.GetValueOrDefault(player.Id);
                return new GameModifierAdminPlayer(
                    player.Id,
                    player.Login,
                    player.DisplayName,
                    Math.Max(0, earned - spent),
                    earned,
                    spent
                );
            })
            .ToArray();
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
            .Where(x => x.GameId == gameId && x.ArchivedAtUtc == null)
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                x => new GameModifierActivationContract(
                    x.Id,
                    x.ModifierId,
                    x.ModifierDefinition.Name,
                    x.ActivatedByUserId.ToString(),
                    x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : x.ActivatedByUserId.ToString(),
                    x.ActivationCostSnapshot > 0
                        ? x.ActivationCostSnapshot
                        : x.ModifierDefinition.ActivationCost,
                    x.ActivatedAtUtc
                )
            )
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ActivateGameModifierRepositoryResult> ActivateModifierAsync(
        Guid modifierId,
        Guid activatedByUserId,
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

        var orderingOpen = await _dbContext.GameRounds.AnyAsync(
            x =>
                x.GameId == activeGame.Id
                && x.Status == GameRoundStatusValue.AwaitingModifiers,
            cancellationToken
        );
        if (!orderingOpen)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ModifierOrderingClosed
            );
        }

        var modifierDefinition = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => x.Id == modifierId && !x.IsArchived)
            .Select(x => new { x.Id, x.Name, x.DefaultLimitPerGame, x.ActivationCost })
            .FirstOrDefaultAsync(cancellationToken);
        if (modifierDefinition is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.NotFound
            );
        }

        var isEnabled = await _dbContext.GameEnabledModifiers.AnyAsync(
            x => x.GameId == activeGame.Id && x.ModifierId == modifierId,
            cancellationToken
        );
        if (!isEnabled)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ModifierNotEnabled
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
                    && x.ArchivedAtUtc == null
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

        if (modifierDefinition.DefaultLimitPerGame.HasValue)
        {
            var activationCount = await _dbContext.GameModifierActivations.CountAsync(
                x =>
                    x.GameId == activeGame.Id
                    && x.ModifierId == modifierId
                    && x.ArchivedAtUtc == null,
                cancellationToken
            );
            if (activationCount >= modifierDefinition.DefaultLimitPerGame.Value)
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
        _dbContext.GameModifierActivations.Add(
            new Data.Entities.GameModifierActivation
            {
                Id = activationEntityId,
                GameId = activeGame.Id,
                ModifierId = modifierId,
                ActivatedByUserId = activatedByUserId,
                ActivationCostSnapshot = modifierDefinition.ActivationCost,
                ActivatedAtUtc = now
            }
        );

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
        Guid activationId,
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

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {activeGame.Id} FOR UPDATE""",
                cancellationToken
            );
        }

        var activation = await _dbContext.GameModifierActivations
            .Include(x => x.ModifierDefinition)
            .FirstOrDefaultAsync(
                x => x.Id == activationId && x.GameId == activeGame.Id && x.ArchivedAtUtc == null,
                cancellationToken
            );
        if (activation is null)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.ActivationNotFound
            );
        }

        var alreadyAppliedInRound = await _dbContext.GameRoundModifierResults.AnyAsync(
            x => x.GameModifierActivationId == activationId,
            cancellationToken
        );
        if (alreadyAppliedInRound)
        {
            return new CancelGameModifierActivationRepositoryResult(
                CancelGameModifierActivationRepositoryStatus.AlreadyAppliedInRound
            );
        }

        _dbContext.GameModifierActivations.Remove(activation);

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
            activationId,
            activation.ActivatedByUserId,
            activation.ModifierDefinition.Name,
            activation.ActivationCostSnapshot > 0
                ? activation.ActivationCostSnapshot
                : activation.ModifierDefinition.ActivationCost
        );
    }

    private async Task<int> GetEarnedQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var answeredPoints = await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == gameId
                    && x.AnsweredForUserId == userId
                    && x.AwardedPoints.HasValue
            )
            .SumAsync(x => x.AwardedPoints ?? 0, cancellationToken);
        var manualPoints = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == gameId && x.AwardedToUserId == userId)
            .SumAsync(x => x.Points, cancellationToken);
        return answeredPoints + manualPoints;
    }

    private async Task<int> GetSpentQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.GameId == gameId && x.ActivatedByUserId == userId)
            .SumAsync(
                x => x.ActivationCostSnapshot > 0
                    ? x.ActivationCostSnapshot
                    : x.ModifierDefinition.ActivationCost,
                cancellationToken
            );
    }

    private static string? ResolveBlockedReason(
        bool orderingClosed,
        bool limitReached,
        bool hasConflict,
        int availablePoints,
        int activationCost
    )
    {
        if (orderingClosed)
        {
            return "ordering_closed";
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

    private static int ResolveActivationCostSnapshot(int snapshot, int currentCost)
    {
        return snapshot > 0 ? snapshot : currentCost;
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
            Name = input.Name,
            Description = input.Description,
            ScoringType = input.ScoringType,
            Category = input.Category,
            RequiresHostControl = input.RequiresHostControl,
            IconEmoji = input.IconEmoji,
            ActivationCommand = input.ActivationCommand,
            ActivationCost = input.ActivationCost,
            DefaultLimitPerGame = ToPerGameLimit(input.ActivationLimit),
            MetadataJson = SerializeMetadata(input.Effect, input.ActivationLimit),
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

    public async Task<GameModifierDefinition?> UpdateModifierAsync(
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
            return null;
        }

        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.ScoringType = input.ScoringType;
        entity.Category = input.Category;
        entity.RequiresHostControl = input.RequiresHostControl;
        entity.ActivationCost = input.ActivationCost;
        entity.DefaultLimitPerGame = ToPerGameLimit(input.ActivationLimit);
        entity.MetadataJson = SerializeMetadata(input.Effect, input.ActivationLimit);
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

        return MapDefinition(entity, input.ConflictingModifierIds);
    }

    public async Task<bool> ArchiveModifierAsync(
        Guid modifierId,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _dbContext.ModifierDefinitions.FirstOrDefaultAsync(
            x => x.Id == modifierId && !x.IsArchived,
            cancellationToken
        );
        if (entity is null)
        {
            return false;
        }

        entity.IsArchived = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static GameModifierDefinition MapDefinition(
        ModifierDefinition x,
        IReadOnlyList<Guid> conflictingModifierIds
    )
    {
        var metadata = DeserializeMetadata(x.MetadataJson, x.ScoringType, x.DefaultLimitPerGame);
        var activationLimit = new GameModifierActivationLimit(
            metadata.ActivationLimit?.Count ?? x.DefaultLimitPerGame
        );

        return new GameModifierDefinition(
            x.Id,
            x.ScoringType,
            x.Category,
            x.RequiresHostControl,
            metadata.Effect.MechanicType,
            x.Name,
            x.Description,
            x.ActivationCost,
            x.DefaultLimitPerGame,
            activationLimit,
            metadata.Effect,
            conflictingModifierIds,
            x.IconEmoji,
            x.ActivationCommand
        );
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

    private static string SerializeMetadata(
        GameModifierEffect effect,
        GameModifierActivationLimit activationLimit
    )
    {
        return JsonSerializer.Serialize(new ModifierMetadata(effect, activationLimit), JsonOptions);
    }

    private static ModifierMetadata DeserializeMetadata(
        string? metadataJson,
        string scoringType,
        int? defaultLimitPerGame
    )
    {
        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<ModifierMetadata>(metadataJson, JsonOptions);
                if (metadata?.Effect is not null)
                {
                    return new ModifierMetadata(
                        metadata.Effect,
                        metadata.ActivationLimit ?? new GameModifierActivationLimit(defaultLimitPerGame)
                    );
                }
            }
            catch (JsonException)
            {
                // Old seed metadata used ad-hoc payloads. Fall back to a safe effect below.
            }
        }

        return new ModifierMetadata(
            BuildFallbackEffect(scoringType, metadataJson),
            new GameModifierActivationLimit(defaultLimitPerGame)
        );
    }

    private static GameModifierEffect BuildFallbackEffect(string scoringType, string? metadataJson)
    {
        return scoringType switch
        {
            GameModifierScoringTypes.Multiplier => new GameModifierEffect(
                GameModifierMechanicTypes.Multiplier,
                ["requires_manual_resolution"],
                null,
                null,
                null,
                [],
                ["killsDuringWindow"],
                null,
                new GameModifierMultiplierEffect("kills", TryReadDecimal(metadataJson, "killMultiplierDelta"), "until_condition", "health_restored"),
                null
            ),
            GameModifierScoringTypes.ConditionalBonusPenalty => new GameModifierEffect(
                GameModifierMechanicTypes.RestrictionWithReward,
                ["requires_manual_resolution"],
                null,
                null,
                new GameModifierScoreImpact(
                    null,
                    TryReadInt(metadataJson, "bonusPerKill"),
                    TryReadInt(metadataJson, "missionFailurePenalty"),
                    null,
                    null,
                    null
                ),
                [new GameModifierCondition("at_least_one_kill", "manual_input")],
                ["kills"],
                null,
                null,
                null
            ),
            GameModifierScoringTypes.ConditionalBonus => new GameModifierEffect(
                GameModifierMechanicTypes.KillCounter,
                ["requires_manual_resolution"],
                null,
                null,
                new GameModifierScoreImpact(
                    null,
                    null,
                    null,
                    null,
                    TryReadInt(metadataJson, "bonusKills"),
                    null
                ),
                [],
                ["kills"],
                new GameModifierKillEffect("conditional_bonus_kill", TryReadInt(metadataJson, "bonusKills") ?? 1, null, []),
                null,
                null
            ),
            _ => new GameModifierEffect(
                GameModifierMechanicTypes.RuleOnly,
                [],
                null,
                null,
                null,
                [],
                [],
                null,
                null,
                null
            )
        };
    }

    private static int? TryReadInt(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement.TryGetProperty(propertyName, out var value)
            && value.TryGetInt32(out var parsed)
            ? parsed
            : null;
    }

    private static decimal? TryReadDecimal(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement.TryGetProperty(propertyName, out var value)
            && value.TryGetDecimal(out var parsed)
            ? parsed
            : null;
    }

    private sealed record ModifierMetadata(
        GameModifierEffect Effect,
        GameModifierActivationLimit? ActivationLimit
    );
}
