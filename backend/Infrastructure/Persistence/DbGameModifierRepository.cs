using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameModifierRepository : IGameModifierRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DbGameModifierRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.ActivationCost)
            .ThenBy(x => x.Name)
            .Select(
                x => new GameModifierDefinition(
                    x.Code,
                    x.Kind,
                    x.Category,
                    x.ScoringType,
                    x.Tier,
                    x.Name,
                    x.Description,
                    x.ActivationCost,
                    x.DefaultLimitPerGame,
                    x.IconEmoji,
                    x.ActivationCommand
                )
            )
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ModifierCodeExistsAsync(
        string modifierCode,
        CancellationToken cancellationToken = default
    )
    {
        return _dbContext.ModifierDefinitions
            .AsNoTracking()
            .AnyAsync(x => x.Code == modifierCode && !x.IsArchived, cancellationToken);
    }

    public async Task<bool> ModifierCodesExistAsync(
        IReadOnlyList<string> modifierCodes,
        CancellationToken cancellationToken = default
    )
    {
        if (modifierCodes.Count == 0)
        {
            return true;
        }

        var knownCount = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => !x.IsArchived && modifierCodes.Contains(x.Code))
            .CountAsync(cancellationToken);
        return knownCount == modifierCodes.Distinct(StringComparer.Ordinal).Count();
    }

    public async Task<IReadOnlyList<string>> GetEnabledModifierCodesForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.GameModifierSelections
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.ModifierCode)
            .Select(x => x.ModifierCode)
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
                    x.ModifierCode,
                    x.ActivatedByUserId.ToString(),
                    x.ActivatedAtUtc
                )
            )
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ActivateGameModifierRepositoryResult> ActivateModifierAsync(
        string modifierCode,
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
            .Where(x => x.Code == modifierCode && !x.IsArchived)
            .Select(x => new { x.Code, x.DefaultLimitPerGame })
            .FirstOrDefaultAsync(cancellationToken);
        if (modifierDefinition is null)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.UnknownModifierCode
            );
        }

        var isEnabled = await _dbContext.GameModifierSelections.AnyAsync(
            x => x.GameId == activeGame.Id && x.ModifierCode == modifierCode,
            cancellationToken
        );
        if (!isEnabled)
        {
            return new ActivateGameModifierRepositoryResult(
                ActivateGameModifierRepositoryStatus.ModifierNotEnabled
            );
        }

        var conflictingActiveCodes = await _dbContext.ModifierConflicts
            .AsNoTracking()
            .Where(
                x =>
                    x.ModifierCode == modifierCode
                    || x.ConflictsWithModifierCode == modifierCode
            )
            .Select(
                x =>
                    x.ModifierCode == modifierCode
                        ? x.ConflictsWithModifierCode
                        : x.ModifierCode
            )
            .ToArrayAsync(cancellationToken);
        if (conflictingActiveCodes.Length > 0)
        {
            var hasConflict = await _dbContext.GameActiveModifiers.AnyAsync(
                x => x.GameId == activeGame.Id && conflictingActiveCodes.Contains(x.ModifierCode),
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
                x => x.GameId == activeGame.Id && x.ModifierCode == modifierCode,
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
                ModifierCode = modifierCode,
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
            new GameModifierActivation(modifierCode, activatedByUserId.ToString(), now)
        );
    }

    public async Task<GameModifierDefinition?> CreateModifierAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        var codeTaken = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .AnyAsync(x => x.Code == input.Code, cancellationToken);
        if (codeTaken)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var entity = new ModifierDefinition
        {
            Code = input.Code,
            Name = input.Name,
            Description = input.Description,
            Kind = input.Kind,
            Category = input.Category,
            ScoringType = input.ScoringType,
            Tier = input.Tier,
            IconEmoji = input.IconEmoji,
            ActivationCommand = input.ActivationCommand,
            ActivationCost = input.ActivationCost,
            DefaultLimitPerGame = input.DefaultLimitPerGame,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.ModifierDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapDefinition(entity);
    }

    public async Task<GameModifierDefinition?> UpdateModifierAsync(
        string modifierCode,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _dbContext.ModifierDefinitions.FirstOrDefaultAsync(
            x => x.Code == modifierCode && !x.IsArchived,
            cancellationToken
        );
        if (entity is null)
        {
            return null;
        }

        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.Kind = input.Kind;
        entity.Category = input.Category;
        entity.ScoringType = input.ScoringType;
        entity.Tier = input.Tier;
        entity.ActivationCost = input.ActivationCost;
        entity.DefaultLimitPerGame = input.DefaultLimitPerGame;
        entity.IconEmoji = input.IconEmoji;
        entity.ActivationCommand = input.ActivationCommand;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapDefinition(entity);
    }

    public async Task<bool> ArchiveModifierAsync(
        string modifierCode,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _dbContext.ModifierDefinitions.FirstOrDefaultAsync(
            x => x.Code == modifierCode && !x.IsArchived,
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

    private static GameModifierDefinition MapDefinition(ModifierDefinition x)
    {
        return new GameModifierDefinition(
            x.Code,
            x.Kind,
            x.Category,
            x.ScoringType,
            x.Tier,
            x.Name,
            x.Description,
            x.ActivationCost,
            x.DefaultLimitPerGame,
            x.IconEmoji,
            x.ActivationCommand
        );
    }
}
