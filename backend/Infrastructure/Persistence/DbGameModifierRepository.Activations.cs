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

}
