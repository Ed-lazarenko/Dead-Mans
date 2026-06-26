using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        return await _dbContext.GameModifierSelections
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.ModifierId)
            .Select(x => x.ModifierId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameModifierActivation>> GetActiveModifiersForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.GameActiveModifiers
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                x => new GameModifierActivation(
                    x.ModifierId,
                    x.ActivatedByUserId.ToString(),
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

        var modifierDefinition = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => x.Id == modifierId && !x.IsArchived)
            .Select(x => new { x.Id, x.DefaultLimitPerGame })
            .FirstOrDefaultAsync(cancellationToken);
        if (modifierDefinition is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.NotFound
            );
        }

        var isEnabled = await _dbContext.GameModifierSelections.AnyAsync(
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
            var hasConflict = await _dbContext.GameActiveModifiers.AnyAsync(
                x => x.GameId == activeGame.Id && conflictingActiveIds.Contains(x.ModifierId),
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
            var activationCount = await _dbContext.GameActiveModifiers.CountAsync(
                x => x.GameId == activeGame.Id && x.ModifierId == modifierId,
                cancellationToken
            );
            if (activationCount >= modifierDefinition.DefaultLimitPerGame.Value)
            {
                return new ActivateGameModifierRepositoryResult(
                    ActivateGameModifierRepositoryStatus.ModifierLimitReached
                );
            }
        }

        var now = DateTime.UtcNow;
        _dbContext.GameActiveModifiers.Add(
            new GameActiveModifier
            {
                Id = Guid.NewGuid(),
                GameId = activeGame.Id,
                ModifierId = modifierId,
                ActivatedByUserId = activatedByUserId,
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
            new GameModifierActivation(modifierId, activatedByUserId.ToString(), now)
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
        var activationLimit = metadata.ActivationLimit
            ?? new GameModifierActivationLimit(x.DefaultLimitPerGame);

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
            BuildLegacyEffect(scoringType, metadataJson),
            new GameModifierActivationLimit(defaultLimitPerGame)
        );
    }

    private static GameModifierEffect BuildLegacyEffect(string scoringType, string? metadataJson)
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
                new GameModifierScoreImpact(null, null, null, null, TryReadInt(metadataJson, "bonusKills")),
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
