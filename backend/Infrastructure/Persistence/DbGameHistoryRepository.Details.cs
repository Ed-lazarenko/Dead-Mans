using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameRounds;
using backend.Application.Features.Scoring;
using backend.Data;
using backend.Infrastructure.Configuration;
using backend.Domain.Persistence;
using backend.Domain.GameModifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameHistoryRepository : IGameHistoryRepository
{
    public async Task<GameHistoryGameDetails?> GetGameDetailsAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Id == gameId && !x.IsDeleted)
            .Select(
                x =>
                    new GameRow(
                        x.Id,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc
                    )
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        var rounds = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == gameId
                    && (
                        x.Status == GameRoundStatusValue.Completed
                        || x.Status == GameRoundStatusValue.Cancelled
                    )
            )
            .Select(
                x =>
                    new RoundRow(
                        x.Id,
                        x.TeamId,
                        x.TeamSlotIndexSnapshot,
                        x.Status,
                        x.Version,
                        x.CreatedAtUtc,
                        x.PreparedAtUtc,
                        x.GameplayStartedAtUtc,
                        x.ReviewedAtUtc,
                        x.FinishedAtUtc,
                        x.BaseScore,
                        x.FinalScore,
                        x.EmptyCardPenaltyApplied,
                        x.KillsCount,
                        x.BountyCount,
                        x.BoardCellId,
                        x.CellRowIndex,
                        x.CellColIndex,
                        x.BoardCell.CellType,
                        x.CellTitleSnapshot,
                        x.CellDescriptionSnapshot ?? x.BoardCell.Description,
                        x.CellCostSnapshot,
                        x.Notes,
                        x.TechnicalCancellationReasonCode,
                        x.PublicCancellationSummary,
                        x.TransitionAudits
                            .Where(
                                audit =>
                                    audit.ActionCode
                                    == GameRoundTransitionActionValue.TechnicalCancel
                            )
                            .OrderByDescending(audit => audit.Sequence)
                            .Select(audit => audit.FromStatus)
                            .FirstOrDefault()
                    )
            )
            .ToArrayAsync(cancellationToken);

        var teamIds = rounds.Select(x => x.TeamId).Distinct().ToArray();
        var teamNamesById = teamIds.Length == 0
            ? new Dictionary<Guid, string?>()
            : await _dbContext.GameTeams
                .AsNoTracking()
                .Where(x => teamIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var roundIds = rounds.Select(x => x.RoundId).ToArray();
        var fullyRefundedRoundIds = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => roundIds.Contains(x.RoundId))
            .GroupBy(x => x.RoundId)
            .Where(
                group =>
                    group.All(
                        activation =>
                            activation.Status == GameModifierActivationStatusValue.Cancelled
                            && activation.RefundAmount == activation.ActivationCostSnapshot
                    )
            )
            .Select(group => group.Key)
            .ToArrayAsync(cancellationToken);
        var fullyRefundedRoundIdSet = fullyRefundedRoundIds.ToHashSet();
        var activationRoundIdSet = (await _dbContext.GameModifierActivations
                .AsNoTracking()
                .Where(x => roundIds.Contains(x.RoundId))
                .Select(x => x.RoundId)
                .Distinct()
                .ToArrayAsync(cancellationToken))
            .ToHashSet();
        var mediaSnapshotsByRoundId = await _dbContext.GameRoundCellMedia
            .AsNoTracking()
            .Where(x => roundIds.Contains(x.RoundId))
            .OrderBy(x => x.SortOrder)
            .Select(x => new RoundCellMediaRow(x.RoundId, x.Bucket, x.ObjectKey, x.SortOrder))
            .ToArrayAsync(cancellationToken);
        var cellMediaSnapshotsByRoundId = mediaSnapshotsByRoundId
            .GroupBy(x => x.RoundId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<GameBoardCellMedia>)x
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new GameBoardCellMedia(
                            GameBoardMediaUrlBuilder.Build(
                                _storagePublicBaseUrl,
                                item.Bucket,
                                item.ObjectKey
                            )
                        ))
                        .ToArray()
            );

        var roundCellIds = rounds.Select(x => x.CellId).Distinct().ToArray();
        var cellMediaById = await GameBoardCellProjection.LoadMediaByCellIdAsync(
            _dbContext,
            _storagePublicBaseUrl,
            roundCellIds,
            cancellationToken
        );

        var participants = await _dbContext.GameRoundParticipants
            .AsNoTracking()
            .Where(x => x.Round.GameId == gameId)
            .Select(
                x =>
                    new RoundParticipantRow(
                        x.RoundId,
                        x.UserId,
                        x.DisplayNameSnapshot,
                        x.CreatedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var modifierResults = await _dbContext.GameRoundModifierResults
            .AsNoTracking()
            .Where(x => x.Round.GameId == gameId)
            .Select(
                x =>
                    new RoundModifierRow(
                        x.RoundId,
                        x.Id,
                        x.ModifierId,
                        x.ModifierNameSnapshot,
                        x.ModifierDescriptionSnapshot,
                        x.ModifierCategorySnapshot,
                        x.OutcomeStatus,
                        x.ScoreDelta,
                        x.KillDelta,
                        x.MultiplierApplied,
                        x.ResolutionDataJson,
                        x.ResolvedByUserId,
                        x.ResolvedAtUtc,
                        x.GameModifierActivationId,
                        x.DefinitionRevisionSnapshot,
                        x.ResolutionKind,
                        x.ViolationComment,
                        x.ModifierBehaviorV2SnapshotJson
                    )
            )
            .ToArrayAsync(cancellationToken);

        var modifierActivations = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                x =>
                    new ModifierActivationRow(
                        x.Id,
                        x.ModifierId,
                        x.ModifierNameSnapshot,
                        x.ActivatedByUserId,
                        x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : null,
                        x.ActivatedAtUtc,
                        x.Status,
                        x.CancelledAtUtc,
                        x.RefundAmount,
                        x.ModifierVersionId
                    )
            )
            .ToArrayAsync(cancellationToken);

        var enabledModifierCount = await _dbContext.GameEnabledModifiers.AsNoTracking()
            .CountAsync(x => x.GameId == gameId, cancellationToken);
        var pinnedRows = await _dbContext.GameEnabledModifiers.AsNoTracking()
            .Where(x => x.GameId == gameId && x.ModifierVersionId != null)
            .OrderBy(x => x.ModifierVersion!.ActivationCost)
            .ThenBy(x => x.ModifierVersion!.Name)
            .Select(x => new PinnedModifierRow(
                x.ModifierId, x.ModifierVersionId!.Value, x.ModifierVersion!.Revision,
                x.ModifierVersion.Name, x.ModifierVersion.Description,
                x.ModifierVersion.Category, x.ModifierVersion.IconEmoji,
                x.ModifierVersion.ActivationCommand, x.ModifierVersion.ActivationCost,
                x.ModifierVersion.MaxActivationsPerRound, x.ModifierVersion.NormalizedTags,
                x.ModifierVersion.BehaviorV2Json, x.EmergencyDisabledAtUtc))
            .ToArrayAsync(cancellationToken);
        var pinnedVersionIds = pinnedRows.Select(x => x.VersionId).ToArray();
        var pinnedConflicts = await _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => pinnedVersionIds.Contains(x.ModifierVersionId))
            .OrderBy(x => x.ConflictingModifierNameSnapshot)
            .Select(x => new PinnedConflictRow(
                x.ModifierVersionId, x.ConflictingModifierId,
                x.ConflictingModifierNameSnapshot))
            .ToArrayAsync(cancellationToken);

        var quizRounds = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.AskedAtUtc)
            .Select(
                x =>
                    new QuizRoundRow(
                        x.Id,
                        x.QuestionId,
                        x.QuestionCodeSnapshot,
                        x.QuestionTextSnapshot,
                        x.CategoryNameSnapshot,
                        x.RewardSnapshot,
                        x.Status,
                        x.AskedAtUtc,
                        x.CorrectAnswer != null ? x.CorrectAnswer.AnsweredAtUtc : null,
                        x.CorrectAnswer != null && x.CorrectAnswer.CapturedByUser != null
                            ? x.CorrectAnswer.CapturedByUser.DisplayName
                            : null,
                        x.CorrectAnswer != null ? x.CorrectAnswer.CapturedByUserId : null,
                        x.CorrectAnswer != null ? x.CorrectAnswer.AwardedToUserId : null,
                        x.CorrectAnswer != null ? x.CorrectAnswer.SubmittedAnswer : null,
                        x.CorrectAnswer != null ? true : null,
                        x.CorrectAnswer != null
                            ? x.CorrectAnswer.PointEntries
                                .Where(entry =>
                                    entry.EntryType == GameQuizPointEntryTypeValue.QuizReward)
                                .Sum(entry => entry.PointsDelta)
                            : null
                    )
            )
            .ToArrayAsync(cancellationToken);

        var manualQuizAwards = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment)
            .OrderBy(x => x.SequenceNumber)
            .Select(
                x =>
                    new QuizManualAwardRow(
                        x.Id,
                        x.UserId,
                        x.User.DisplayName,
                        x.CreatedByUserId!.Value,
                        x.CreatedByUser != null ? x.CreatedByUser.DisplayName : null,
                        x.PointsDelta,
                        x.PointsDelta < 0
                            ? GameQuizManualAdjustmentOperationValue.Deduct
                            : GameQuizManualAdjustmentOperationValue.Award,
                        x.Reason,
                        x.OccurredAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var userDisplayNames = await LoadUserDisplayNamesAsync(
            participants.Select(x => x.UserId)
                .Concat(modifierActivations.Select(x => x.ActivatedByUserId))
                .Concat(quizRounds.Select(x => x.AnsweredByUserId).Where(x => x.HasValue).Select(x => x!.Value))
                .Concat(quizRounds.Select(x => x.AnsweredForUserId ?? x.AnsweredByUserId).Where(x => x.HasValue).Select(x => x!.Value))
                .Concat(manualQuizAwards.Select(x => x.AwardedToUserId))
                .Concat(manualQuizAwards.Select(x => x.AwardedByUserId))
                .Distinct()
                .ToArray(),
            cancellationToken
        );

        var participantsByRoundId = participants
            .GroupBy(x => x.RoundId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<GameHistoryRoundParticipantItem>)
                        x.OrderBy(item => item.CreatedAtUtc)
                            .Select(
                                item =>
                                    new GameHistoryRoundParticipantItem(
                                        item.UserId,
                                        ResolveDisplayName(item.DisplayName, userDisplayNames, item.UserId),
                                        item.CreatedAtUtc
                                    )
                            )
                            .ToArray()
            );

        var modifiersByRoundId = modifierResults
            .GroupBy(x => x.RoundId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<GameHistoryRoundModifierItem>)
                        x.Select(
                                item =>
                                    new GameHistoryRoundModifierItem(
                                        item.ModifierResultId,
                                        item.ModifierId,
                                        item.ModifierName,
                                        item.ModifierDescription,
                                        item.ModifierCategory,
                                        item.OutcomeStatus,
                                        item.ScoreDelta,
                                        item.KillDelta,
                                        item.MultiplierApplied,
                                        item.ResolutionDataJson,
                                        item.ResolvedByUserId,
                                        item.ResolvedAtUtc,
                                        item.ActivationId,
                                        item.DefinitionRevision,
                                        item.ResolutionKind,
                                        item.ViolationComment,
                                        string.IsNullOrWhiteSpace(item.BehaviorJson)
                                            ? null
                                            : ModifierBehaviorV2Json.Deserialize(item.BehaviorJson)
                                    )
                            )
                            .ToArray()
            );

        var successfulModifierActivations = modifierActivations
            .Where(x => x.Status != GameModifierActivationStatusValue.Cancelled).ToArray();
        var mainPlayerStats = BuildMainGamePlayerStats(
            participants,
            rounds,
            successfulModifierActivations,
            userDisplayNames
        );
        var quizPlayerStats = BuildQuizPlayerStats(quizRounds, manualQuizAwards, userDisplayNames);
        var mainModifierActivations = modifierActivations
            .Select(
                x =>
                    new GameHistoryModifierActivationItem(
                        x.ActivationId,
                        x.ModifierId,
                        x.ModifierName,
                        x.ActivatedByUserId,
                        ResolveDisplayName(x.ActivatedByDisplayName, userDisplayNames, x.ActivatedByUserId),
                        x.ActivatedAtUtc,
                        x.Status,
                        x.CancelledAtUtc,
                        x.RefundAmount
                    )
            )
            .ToArray();
        var activationCountsByVersion = modifierActivations.Where(x => x.ModifierVersionId.HasValue
                && x.Status != GameModifierActivationStatusValue.Cancelled)
            .GroupBy(x => x.ModifierVersionId!.Value).ToDictionary(x => x.Key, x => x.Count());
        var cancelledCountsByVersion = modifierActivations
            .Where(x => x.ModifierVersionId.HasValue
                && x.Status == GameModifierActivationStatusValue.Cancelled)
            .GroupBy(x => x.ModifierVersionId!.Value).ToDictionary(x => x.Key, x => x.Count());
        var activationVersionById = modifierActivations.Where(x => x.ModifierVersionId.HasValue)
            .ToDictionary(x => x.ActivationId, x => x.ModifierVersionId!.Value);
        var resultCountsByVersion = modifierResults
            .Where(x => activationVersionById.ContainsKey(x.ActivationId))
            .GroupBy(x => activationVersionById[x.ActivationId])
            .ToDictionary(x => x.Key, x => x.Count());
        var conflictLookupByVersion = pinnedConflicts.ToLookup(x => x.VersionId);
        var modifierSnapshots = pinnedRows.Select(x => new GameHistoryModifierSnapshot(
            x.ModifierId, x.VersionId, x.Revision, x.Name, x.Description, x.Category,
            x.IconEmoji, x.ActivationCommand, x.ActivationCost,
            new GameModifierActivationLimit(x.MaxActivationsPerRound), x.NormalizedTags,
            ModifierBehaviorV2Json.Deserialize(x.BehaviorV2Json),
            conflictLookupByVersion[x.VersionId].Select(c => new ModifierConflictSnapshot(
                c.ConflictingModifierId, c.Name)).ToArray(),
            activationCountsByVersion.GetValueOrDefault(x.VersionId),
            cancelledCountsByVersion.GetValueOrDefault(x.VersionId),
            resultCountsByVersion.GetValueOrDefault(x.VersionId),
            x.EmergencyDisabledAtUtc.HasValue, x.EmergencyDisabledAtUtc)).ToArray();
        var mainRounds = rounds
            .OrderBy(x => x.StartedAtUtc)
            .Select(
                x =>
                {
                    var roundModifiers = modifiersByRoundId.GetValueOrDefault(
                        x.RoundId,
                        Array.Empty<GameHistoryRoundModifierItem>()
                    );

                    return new GameHistoryRoundItem(
                        x.RoundId,
                        x.TeamId,
                        teamNamesById.GetValueOrDefault(x.TeamId),
                        x.TeamSlotIndex,
                        x.Status,
                        x.RoundVersion,
                        x.StartedAtUtc,
                        x.PreparedAtUtc,
                        x.GameplayStartedAtUtc,
                        x.ReviewedAtUtc,
                        x.FinishedAtUtc,
                        x.BaseScore,
                        x.FinalScore,
                        x.EmptyCardPenaltyApplied,
                        BuildRoundScoreDetails(x, roundModifiers),
                        x.KillsCount,
                        x.BountyCount,
                        x.CellId,
                        x.CellRowIndex,
                        x.CellColIndex,
                        x.CellType,
                        x.CellTitle,
                        x.CellDescription,
                        x.CellCost,
                        x.Notes,
                        x.TechnicalCancellationReasonCode,
                        x.PublicCancellationSummary,
                        x.TechnicalCancellationStage,
                        x.Status == GameRoundStatusValue.Cancelled
                            && (!activationRoundIdSet.Contains(x.RoundId)
                                || fullyRefundedRoundIdSet.Contains(x.RoundId)),
                        cellMediaSnapshotsByRoundId.GetValueOrDefault(x.RoundId)
                        ?? (cellMediaById.TryGetValue(x.CellId, out var cellMedia)
                            ? cellMedia
                            : Array.Empty<GameBoardCellMedia>()),
                        participantsByRoundId.GetValueOrDefault(
                            x.RoundId,
                            Array.Empty<GameHistoryRoundParticipantItem>()
                        ),
                        roundModifiers
                    );
                }
            )
            .ToArray();

        var finalResult = await LoadFinalResultAsync(gameId, cancellationToken);

        return new GameHistoryGameDetails(
            game.GameId,
            game.Title,
            game.Status,
            game.CreatedAtUtc,
            game.StartedAtUtc,
            game.FinishedAtUtc,
            new GameHistoryMainGameSection(
                mainPlayerStats,
                BuildTeamLeaderboard(mainRounds),
                mainModifierActivations,
                mainRounds
            ),
            new GameHistoryQuizSection(
                SaturatingInt32.From(quizPlayerStats.Sum(x => (long)x.Points)),
                quizPlayerStats,
                quizRounds
                    .Select(
                        x =>
                            new GameHistoryQuizRoundItem(
                                x.RoundId,
                                x.QuestionId,
                                x.QuestionCode,
                                x.QuestionText,
                                x.CategoryName,
                                x.Reward,
                                x.Status,
                                x.AskedAtUtc,
                                x.AnsweredAtUtc,
                                x.AnsweredByUserId.HasValue
                                    ? ResolveDisplayName(
                                        x.AnsweredByDisplayName,
                                        userDisplayNames,
                                        x.AnsweredByUserId.Value
                                    )
                                    : x.AnsweredByDisplayName,
                                x.AnsweredByUserId,
                                x.AnsweredForUserId,
                                x.AnsweredForUserId.HasValue
                                    ? ResolveDisplayName(
                                        null,
                                        userDisplayNames,
                                        x.AnsweredForUserId.Value
                                    )
                                    : null,
                                x.SubmittedAnswer,
                                x.IsCorrect,
                                x.AwardedPoints
                            )
                    )
                    .ToArray(),
                manualQuizAwards
                    .Select(
                        x =>
                            new GameHistoryQuizManualAwardItem(
                                x.AwardId,
                                x.AwardedToUserId,
                                ResolveDisplayName(x.AwardedToDisplayName, userDisplayNames, x.AwardedToUserId),
                                x.AwardedByUserId,
                                ResolveDisplayName(x.AwardedByDisplayName, userDisplayNames, x.AwardedByUserId),
                                x.Points,
                                x.OperationType,
                                x.Reason,
                                x.AwardedAtUtc
                            )
                    )
                    .ToArray()
            ),
            finalResult,
            enabledModifierCount == pinnedRows.Length ? "complete" : "legacy_unavailable",
            modifierSnapshots
        );
    }

    private async Task<GameFinishSummary?> LoadFinalResultAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var finalization = await _dbContext.GameFinalizations
            .AsNoTracking()
            .Include(x => x.Game)
            .ThenInclude(x => x.Board)
            .Include(x => x.TeamResults)
            .FirstOrDefaultAsync(x => x.GameId == gameId, cancellationToken);
        if (finalization is null)
        {
            return null;
        }

        return new GameFinishSummary(
            finalization.GameId,
            finalization.Game.Title,
            finalization.Game.Status,
            finalization.Game.Board?.Version ?? 0,
            finalization.FinishedAtUtc,
            finalization.FinishedByUserId,
            finalization.FinishedByDisplayNameSnapshot,
            finalization.PublicNote,
            finalization.CalculationVersion,
            finalization.CompletedRoundCount,
            finalization.CancelledRoundCount,
            finalization.TotalKills,
            finalization.TotalBounties,
            finalization.QuizTotalPoints,
            0,
            finalization.SkippedQuizQuestionCount,
            finalization.TeamResults
                .OrderBy(x => x.Placement ?? int.MaxValue)
                .ThenByDescending(x => x.FinalScore)
                .ThenByDescending(x => x.BestScore)
                .ThenByDescending(x => x.TotalScore)
                .ThenByDescending(x => x.LastFinishedAtUtc)
                .ThenBy(x => x.TeamSlotIndexSnapshot)
                .Select(x => new GameFinishTeamResult(
                    x.TeamId,
                    x.TeamNameSnapshot,
                    x.TeamSlotIndexSnapshot,
                    x.ParticipantNamesSnapshot,
                    x.RoundsPlayed,
                    x.BestScore,
                    x.PenaltyTotal,
                    x.FinalScore,
                    x.TotalScore,
                    x.TotalBonusDelta,
                    x.TotalKills,
                    x.TotalBounties,
                    x.Placement,
                    x.LastFinishedAtUtc
                ))
                .ToArray()
        );
    }

}
