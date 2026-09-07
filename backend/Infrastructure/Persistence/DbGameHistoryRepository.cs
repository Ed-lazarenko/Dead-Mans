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

public sealed class DbGameHistoryRepository : IGameHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;

    public DbGameHistoryRepository(
        ApplicationDbContext dbContext,
        IOptions<StorageOptions> storageOptions
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = storageOptions.Value.PublicBaseUrl.TrimEnd('/');
    }

    public async Task<IReadOnlyList<GameHistoryLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default
    )
    {
        var mainGameRows = await _dbContext.GameRoundParticipants
            .AsNoTracking()
            .Where(
                x =>
                    !x.Round.Game.IsDeleted
                    && x.Round.Status == GameRoundStatusValue.Completed
            )
            .Select(
                x =>
                    new LeaderboardMainGameRow(
                        x.UserId,
                        x.DisplayNameSnapshot,
                        x.Round.GameId,
                        x.Round.FinalScore ?? x.Round.BaseScore,
                        x.Round.FinishedAtUtc ?? x.Round.StartedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var quizRows = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game!.IsDeleted
                    && x.AnsweredAtUtc.HasValue
                    && (x.AnsweredForUserId.HasValue || x.AnsweredByUserId.HasValue)
            )
            .Select(
                x =>
                    new LeaderboardQuizRow(
                        x.AnsweredForUserId ?? x.AnsweredByUserId!.Value,
                        x.AnsweredByDisplayName,
                        x.GameId,
                        x.AwardedPoints ?? 0,
                        x.IsCorrect ?? false,
                        x.AnsweredAtUtc!.Value
                    )
            )
            .ToArrayAsync(cancellationToken);

        var manualQuizRows = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => !x.Game!.IsDeleted)
            .Select(
                x =>
                    new LeaderboardQuizRow(
                        x.AwardedToUserId,
                        x.AwardedToUser != null ? x.AwardedToUser.DisplayName : null,
                        x.GameId,
                        x.Points,
                        true,
                        x.AwardedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var modifierRows = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game.IsDeleted
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .Select(
                x =>
                    new LeaderboardModifierRow(
                        x.ActivatedByUserId,
                        x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : null,
                        x.GameId,
                        x.ActivatedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var userDisplayNames = await LoadUserDisplayNamesAsync(
            mainGameRows.Select(x => x.UserId)
                .Concat(quizRows.Select(x => x.UserId))
                .Concat(manualQuizRows.Select(x => x.UserId))
                .Concat(modifierRows.Select(x => x.UserId))
                .Distinct()
                .ToArray(),
            cancellationToken
        );

        var leaderboard = new Dictionary<Guid, LeaderboardAccumulator>();
        foreach (var row in mainGameRows)
        {
            var entry = GetOrCreateLeaderboardEntry(
                leaderboard,
                row.UserId,
                ResolveDisplayName(row.DisplayName, userDisplayNames, row.UserId)
            );
            entry.MainGamePoints += row.Points;
            entry.MainGameRoundsPlayed += 1;
            entry.GamesPlayed.Add(row.GameId);
            entry.LastActivityAtUtc = Max(entry.LastActivityAtUtc, row.OccurredAtUtc);
        }

        foreach (var row in quizRows)
        {
            AddQuizLeaderboardRow(leaderboard, row, userDisplayNames, countAsCorrectAnswer: row.IsCorrect);
        }

        foreach (var row in manualQuizRows)
        {
            AddQuizLeaderboardRow(leaderboard, row, userDisplayNames, countAsCorrectAnswer: false);
        }

        foreach (var row in modifierRows)
        {
            var entry = GetOrCreateLeaderboardEntry(
                leaderboard,
                row.UserId,
                ResolveDisplayName(row.DisplayName, userDisplayNames, row.UserId)
            );
            entry.ModifiersActivated += 1;
            entry.GamesPlayed.Add(row.GameId);
            entry.LastActivityAtUtc = Max(entry.LastActivityAtUtc, row.OccurredAtUtc);
        }

        return leaderboard
            .Select(
                x =>
                    new GameHistoryLeaderboardEntry(
                        x.Key,
                        x.Value.DisplayName,
                        SaturatingInt32.From(x.Value.MainGamePoints),
                        SaturatingInt32.From(x.Value.QuizPoints),
                        SaturatingInt32.From(x.Value.MainGamePoints + x.Value.QuizPoints),
                        x.Value.GamesPlayed.Count,
                        SaturatingInt32.From(x.Value.MainGameRoundsPlayed),
                        SaturatingInt32.From(x.Value.QuizRoundsAnswered),
                        SaturatingInt32.From(x.Value.CorrectQuizAnswers),
                        SaturatingInt32.From(x.Value.ModifiersActivated),
                        x.Value.LastActivityAtUtc
                    )
            )
            .OrderByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.MainGamePoints)
            .ThenByDescending(x => x.QuizPoints)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<GameHistoryGameSummary>> GetGamesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var games = await _dbContext.Games
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
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
            .ToArrayAsync(cancellationToken);

        var roundCounts = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game.IsDeleted
                    && x.Status == GameRoundStatusValue.Completed
            )
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var quizCounts = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => !x.Game!.IsDeleted)
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var manualQuizCounts = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => !x.Game!.IsDeleted)
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var uniquePlayers = await LoadUniquePlayerCountsByGameAsync(cancellationToken);

        return games
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(
                x =>
                    new GameHistoryGameSummary(
                        x.GameId,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc,
                        roundCounts.GetValueOrDefault(x.GameId, 0),
                        quizCounts.GetValueOrDefault(x.GameId, 0)
                            + manualQuizCounts.GetValueOrDefault(x.GameId, 0),
                        uniquePlayers.GetValueOrDefault(x.GameId, 0)
                    )
            )
            .ToArray();
    }

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
                        x.StartedAtUtc,
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
            .Select(x => new RoundCellMediaRow(x.RoundId, x.Url, x.SortOrder))
            .ToArrayAsync(cancellationToken);
        var cellMediaSnapshotsByRoundId = mediaSnapshotsByRoundId
            .GroupBy(x => x.RoundId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<GameBoardCellMedia>)x
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new GameBoardCellMedia(item.Url))
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
                        x.Question != null ? x.Question.ExternalCode : string.Empty,
                        x.Question != null ? x.Question.Text : string.Empty,
                        x.Question != null && x.Question.CategoryDefinition != null
                            ? x.Question.CategoryDefinition.Name
                            : string.Empty,
                        x.Question != null ? x.Question.Reward : 0,
                        x.Status,
                        x.AskedAtUtc,
                        x.AnsweredAtUtc,
                        x.AnsweredByDisplayName,
                        x.AnsweredByUserId,
                        x.AnsweredForUserId,
                        x.SubmittedAnswer,
                        x.IsCorrect,
                        x.AwardedPoints
                    )
            )
            .ToArrayAsync(cancellationToken);

        var manualQuizAwards = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.AwardedAtUtc)
            .Select(
                x =>
                    new QuizManualAwardRow(
                        x.Id,
                        x.AwardedToUserId,
                        x.AwardedToUser != null ? x.AwardedToUser.DisplayName : null,
                        x.AwardedByUserId,
                        x.AwardedByUser != null ? x.AwardedByUser.DisplayName : null,
                        x.Points,
                        x.OperationType,
                        x.Reason,
                        x.AwardedAtUtc
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

    public async Task<IReadOnlyList<UserGameHistoryItem>> GetUserGameHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var modifierGameIds = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.ActivatedByUserId == userId
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var answeredGameIds = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.AnsweredAtUtc.HasValue
                    && (
                        x.AnsweredForUserId == userId
                        || (x.AnsweredForUserId == null && x.AnsweredByUserId == userId)
                    )
            )
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var manualAwardGameIds = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.AwardedToUserId == userId)
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var gameIds = modifierGameIds
            .Concat(answeredGameIds)
            .Concat(manualAwardGameIds)
            .Distinct()
            .ToArray();
        if (gameIds.Length == 0)
        {
            return Array.Empty<UserGameHistoryItem>();
        }

        var games = await _dbContext.Games
            .AsNoTracking()
            .Where(x => gameIds.Contains(x.Id))
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
            .ToArrayAsync(cancellationToken);

        var modifierActivations = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.ActivatedByUserId == userId
                    && gameIds.Contains(x.GameId)
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameModifierActivationHistoryItem(x.ModifierId, x.ActivatedAtUtc)
                    }
            )
            .ToArrayAsync(cancellationToken);

        var questionAnswers = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.AnsweredAtUtc.HasValue
                    && gameIds.Contains(x.GameId)
                    && (
                        x.AnsweredForUserId == userId
                        || (x.AnsweredForUserId == null && x.AnsweredByUserId == userId)
                    )
            )
            .OrderBy(x => x.AnsweredAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameQuestionAnswerHistoryItem(
                            x.Id,
                            x.QuestionId,
                            x.Question != null ? x.Question.Text : string.Empty,
                            x.Question != null && x.Question.CategoryDefinition != null
                                ? x.Question.CategoryDefinition.Name
                                : string.Empty,
                            x.AnsweredAtUtc!.Value,
                            x.IsCorrect ?? false,
                            x.AwardedPoints ?? 0,
                            x.SubmittedAnswer,
                            x.AnsweredByUserId
                        )
                    }
            )
            .ToArrayAsync(cancellationToken);

        var manualAwards = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.AwardedToUserId == userId && gameIds.Contains(x.GameId))
            .OrderBy(x => x.AwardedAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameQuizManualAwardHistoryItem(
                            x.Id,
                            x.AwardedAtUtc,
                            x.Points,
                            x.AwardedByUserId,
                            x.AwardedByUser != null ? x.AwardedByUser.DisplayName : x.AwardedByUserId.ToString(),
                            x.OperationType,
                            x.Reason
                        )
                    }
            )
            .ToArrayAsync(cancellationToken);

        var modifiersByGameId = modifierActivations
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<UserGameModifierActivationHistoryItem>)
                        x.Select(item => item.Item).ToArray()
            );
        var answersByGameId = questionAnswers
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<UserGameQuestionAnswerHistoryItem>)
                        x.Select(item => item.Item).ToArray()
            );
        var manualAwardsByGameId = manualAwards
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<UserGameQuizManualAwardHistoryItem>)
                        x.Select(item => item.Item).ToArray()
            );

        return games
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(
                x =>
                    new UserGameHistoryItem(
                        x.GameId,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc,
                        modifiersByGameId.GetValueOrDefault(
                            x.GameId,
                            Array.Empty<UserGameModifierActivationHistoryItem>()
                        ),
                        answersByGameId.GetValueOrDefault(
                            x.GameId,
                            Array.Empty<UserGameQuestionAnswerHistoryItem>()
                        ),
                        manualAwardsByGameId.GetValueOrDefault(
                            x.GameId,
                            Array.Empty<UserGameQuizManualAwardHistoryItem>()
                        )
                    )
            )
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadUserDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken
    )
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> LoadUniquePlayerCountsByGameAsync(
        CancellationToken cancellationToken
    )
    {
        var mainPlayers = await _dbContext.GameRoundParticipants
            .AsNoTracking()
            .Where(x => !x.Round.Game.IsDeleted)
            .Select(x => new GamePlayerRow(x.Round.GameId, x.UserId))
            .ToArrayAsync(cancellationToken);

        var quizPlayers = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game!.IsDeleted
                    && (x.AnsweredForUserId.HasValue || x.AnsweredByUserId.HasValue)
            )
            .Select(
                x => new GamePlayerRow(x.GameId, x.AnsweredForUserId ?? x.AnsweredByUserId!.Value)
            )
            .ToArrayAsync(cancellationToken);

        var manualQuizPlayers = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => !x.Game!.IsDeleted)
            .Select(x => new GamePlayerRow(x.GameId, x.AwardedToUserId))
            .ToArrayAsync(cancellationToken);

        var modifierPlayers = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game.IsDeleted
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .Select(x => new GamePlayerRow(x.GameId, x.ActivatedByUserId))
            .ToArrayAsync(cancellationToken);

        return mainPlayers
            .Concat(quizPlayers)
            .Concat(manualQuizPlayers)
            .Concat(modifierPlayers)
            .GroupBy(x => x.GameId)
            .ToDictionary(x => x.Key, x => x.Select(item => item.UserId).Distinct().Count());
    }

    private static GameRoundScoreDetails BuildRoundScoreDetails(
        RoundRow round,
        IReadOnlyList<GameHistoryRoundModifierItem> modifiers
    )
    {
        var breakdown = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                round.Status,
                round.BaseScore,
                round.KillsCount,
                round.BountyCount,
                modifiers
                    .Select(x => new GameRoundScoreModifierInput(
                        x.ScoreDelta,
                        x.KillDelta,
                        x.ModifierId,
                        x.ModifierName,
                        x.DefinitionRevision,
                        x.RuntimeBehavior,
                        x.ResolutionDataJson
                    ))
                    .ToArray()
            )
        );

        return new GameRoundScoreDetails(
            breakdown.ScoreUnit,
            breakdown.KillsScore,
            breakdown.BountyScore,
            breakdown.ModifierKillDelta,
            breakdown.ModifierKillScore,
            breakdown.ModifierScoreDelta,
            breakdown.EmptyCardPenaltyApplied,
            breakdown.EmptyCardPenaltyScore,
            breakdown.PenaltyTotal,
            breakdown.BonusDelta,
            breakdown.TotalKillCount,
            breakdown.FinalScore,
            breakdown.CalculationLines
        );
    }

    private static IReadOnlyList<GameHistoryTeamLeaderboardEntry> BuildTeamLeaderboard(
        IReadOnlyList<GameHistoryRoundItem> rounds
    )
    {
        var countedRounds = rounds.Where(IsCountedRound).ToArray();
        var roundsById = countedRounds.ToDictionary(x => x.RoundId);
        var inputs = countedRounds
            .GroupBy(x => x.TeamId)
            .Select(teamRounds =>
            {
                var roundsArray = teamRounds.ToArray();
                var first = roundsArray[0];
                return new GameTeamResultCalculationInput(
                    first.TeamId,
                    first.TeamName,
                    first.TeamSlotIndex,
                    roundsArray
                        .SelectMany(x => x.Participants)
                        .Select(x => x.DisplayName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    roundsArray
                        .Select(x => new GameTeamRoundScoreFact(
                            x.RoundId,
                            x.ScoreDetails.FinalScore,
                            x.ScoreDetails.PenaltyTotal,
                            x.ScoreDetails.BonusDelta,
                            x.ScoreDetails.TotalKillCount,
                            x.BountyCount,
                            GetRoundSortTimestamp(x)
                        ))
                        .ToArray()
                );
            });

        return GameTeamResultCalculator.Calculate(inputs)
            .Select(result =>
            {
                var bestRound = roundsById[result.BestRoundId!.Value];
                var latestRound = roundsById[result.LatestRoundId!.Value];
                return new GameHistoryTeamLeaderboardEntry(
                    result.TeamId,
                    result.TeamName,
                    result.TeamSlotIndex,
                    result.RoundsPlayed,
                    result.BestScore!.Value,
                    result.PenaltyTotal,
                    result.FinalScore!.Value,
                    bestRound,
                    latestRound,
                    result.RoundIdsByRecency.Select(id => roundsById[id]).ToArray(),
                    result.TotalScore,
                    result.AverageScore,
                    result.TotalBonusDelta,
                    result.TotalKills,
                    result.TotalBounties,
                    result.ParticipantNames,
                    result.LastFinishedAtUtc!.Value
                );
            })
            .ToArray();
    }

    private static long GetRoundScore(GameHistoryRoundItem round)
    {
        return round.ScoreDetails.FinalScore;
    }

    private static long GetRoundScoreBeforePenalty(GameHistoryRoundItem round)
    {
        return (long)round.ScoreDetails.FinalScore + round.ScoreDetails.PenaltyTotal;
    }

    private static long GetRoundPenaltyTotal(GameHistoryRoundItem round)
    {
        return round.ScoreDetails.PenaltyTotal;
    }

    private static long GetRoundBonusDelta(GameHistoryRoundItem round)
    {
        return round.ScoreDetails.BonusDelta;
    }

    private static DateTime GetRoundSortTimestamp(GameHistoryRoundItem round)
    {
        return round.FinishedAtUtc ?? round.StartedAtUtc;
    }

    private static bool IsCountedRound(GameHistoryRoundItem round)
    {
        return IsCountedRoundStatus(round.Status);
    }

    private static bool IsCountedRoundStatus(string status)
    {
        return status == GameRoundStatusValue.Completed;
    }

    private static IReadOnlyList<GameHistoryPlayerSummary> BuildMainGamePlayerStats(
        IReadOnlyList<RoundParticipantRow> participants,
        IReadOnlyList<RoundRow> rounds,
        IReadOnlyList<ModifierActivationRow> modifierActivations,
        IReadOnlyDictionary<Guid, string> userDisplayNames
    )
    {
        var roundLookup = rounds
            .Where(x => IsCountedRoundStatus(x.Status))
            .ToDictionary(x => x.RoundId);
        var summary = new Dictionary<Guid, PlayerStatsAccumulator>();

        foreach (var participant in participants)
        {
            if (!roundLookup.TryGetValue(participant.RoundId, out var round))
            {
                continue;
            }

            var points = round.FinalScore ?? round.BaseScore;
            var row = GetOrCreatePlayerStatsEntry(
                summary,
                participant.UserId,
                ResolveDisplayName(participant.DisplayName, userDisplayNames, participant.UserId)
            );
            row.Points += points;
            row.EventCount += 1;
            row.LastActivityAtUtc = Max(row.LastActivityAtUtc, round.FinishedAtUtc ?? round.StartedAtUtc);
        }

        foreach (var activation in modifierActivations)
        {
            var row = GetOrCreatePlayerStatsEntry(
                summary,
                activation.ActivatedByUserId,
                ResolveDisplayName(
                    activation.ActivatedByDisplayName,
                    userDisplayNames,
                    activation.ActivatedByUserId
                )
            );
            row.EventCount += 1;
            row.LastActivityAtUtc = Max(row.LastActivityAtUtc, activation.ActivatedAtUtc);
        }

        return summary
            .Select(
                x =>
                    new GameHistoryPlayerSummary(
                        x.Key,
                        x.Value.DisplayName,
                        SaturatingInt32.From(x.Value.Points),
                        SaturatingInt32.From(x.Value.EventCount),
                        x.Value.LastActivityAtUtc
                    )
            )
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.EventCount)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<GameHistoryPlayerSummary> BuildQuizPlayerStats(
        IReadOnlyList<QuizRoundRow> quizRounds,
        IReadOnlyList<QuizManualAwardRow> manualAwards,
        IReadOnlyDictionary<Guid, string> userDisplayNames
    )
    {
        var summary = new Dictionary<Guid, PlayerStatsAccumulator>();
        foreach (var round in quizRounds)
        {
            var creditedUserId = round.AnsweredForUserId ?? round.AnsweredByUserId;
            if (!creditedUserId.HasValue)
            {
                continue;
            }

            var row = GetOrCreatePlayerStatsEntry(
                summary,
                creditedUserId.Value,
                round.AnsweredForUserId.HasValue
                    ? ResolveDisplayName(null, userDisplayNames, creditedUserId.Value)
                    : ResolveDisplayName(
                        round.AnsweredByDisplayName,
                        userDisplayNames,
                        creditedUserId.Value
                    )
            );
            row.Points += round.AwardedPoints ?? 0;
            row.EventCount += 1;
            row.LastActivityAtUtc = Max(row.LastActivityAtUtc, round.AnsweredAtUtc ?? round.AskedAtUtc);
        }

        foreach (var award in manualAwards)
        {
            var row = GetOrCreatePlayerStatsEntry(
                summary,
                award.AwardedToUserId,
                ResolveDisplayName(award.AwardedToDisplayName, userDisplayNames, award.AwardedToUserId)
            );
            row.Points += award.Points;
            row.EventCount += 1;
            row.LastActivityAtUtc = Max(row.LastActivityAtUtc, award.AwardedAtUtc);
        }

        return summary
            .Select(
                x =>
                    new GameHistoryPlayerSummary(
                        x.Key,
                        x.Value.DisplayName,
                        SaturatingInt32.From(x.Value.Points),
                        SaturatingInt32.From(x.Value.EventCount),
                        x.Value.LastActivityAtUtc
                    )
            )
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.EventCount)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddQuizLeaderboardRow(
        IDictionary<Guid, LeaderboardAccumulator> leaderboard,
        LeaderboardQuizRow row,
        IReadOnlyDictionary<Guid, string> userDisplayNames,
        bool countAsCorrectAnswer
    )
    {
        var entry = GetOrCreateLeaderboardEntry(
            leaderboard,
            row.UserId,
            ResolveDisplayName(row.DisplayName, userDisplayNames, row.UserId)
        );
        entry.QuizPoints += row.Points;
        entry.QuizRoundsAnswered += 1;
        entry.CorrectQuizAnswers += countAsCorrectAnswer ? 1 : 0;
        entry.GamesPlayed.Add(row.GameId);
        entry.LastActivityAtUtc = Max(entry.LastActivityAtUtc, row.OccurredAtUtc);
    }

    private static LeaderboardAccumulator GetOrCreateLeaderboardEntry(
        IDictionary<Guid, LeaderboardAccumulator> leaderboard,
        Guid userId,
        string displayName
    )
    {
        if (!leaderboard.TryGetValue(userId, out var entry))
        {
            entry = new LeaderboardAccumulator(displayName);
            leaderboard[userId] = entry;
        }
        else if (string.IsNullOrWhiteSpace(entry.DisplayName) && !string.IsNullOrWhiteSpace(displayName))
        {
            entry.DisplayName = displayName;
        }

        return entry;
    }

    private static PlayerStatsAccumulator GetOrCreatePlayerStatsEntry(
        IDictionary<Guid, PlayerStatsAccumulator> summary,
        Guid userId,
        string displayName
    )
    {
        if (!summary.TryGetValue(userId, out var row))
        {
            row = new PlayerStatsAccumulator(displayName);
            summary[userId] = row;
        }
        else if (string.IsNullOrWhiteSpace(row.DisplayName) && !string.IsNullOrWhiteSpace(displayName))
        {
            row.DisplayName = displayName;
        }

        return row;
    }

    private static string ResolveDisplayName(
        string? preferredDisplayName,
        IReadOnlyDictionary<Guid, string> userDisplayNames,
        Guid userId
    )
    {
        if (!string.IsNullOrWhiteSpace(preferredDisplayName))
        {
            return preferredDisplayName;
        }

        if (userDisplayNames.TryGetValue(userId, out var displayName) && !string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return userId.ToString();
    }

    private static DateTime? Max(DateTime? left, DateTime? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return left.Value >= right.Value ? left : right;
    }

    private sealed record GameRow(
        Guid GameId,
        string Title,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? StartedAtUtc,
        DateTime? FinishedAtUtc
    );

    private sealed record CountRow(Guid GameId, int Count);

    private sealed record GamePlayerRow(Guid GameId, Guid UserId);

    private sealed record LeaderboardMainGameRow(
        Guid UserId,
        string DisplayName,
        Guid GameId,
        int Points,
        DateTime OccurredAtUtc
    );

    private sealed record LeaderboardQuizRow(
        Guid UserId,
        string? DisplayName,
        Guid GameId,
        int Points,
        bool IsCorrect,
        DateTime OccurredAtUtc
    );

    private sealed record LeaderboardModifierRow(
        Guid UserId,
        string? DisplayName,
        Guid GameId,
        DateTime OccurredAtUtc
    );

    private sealed record RoundRow(
        Guid RoundId,
        Guid TeamId,
        int TeamSlotIndex,
        string Status,
        int RoundVersion,
        DateTime StartedAtUtc,
        DateTime? PreparedAtUtc,
        DateTime? GameplayStartedAtUtc,
        DateTime? ReviewedAtUtc,
        DateTime? FinishedAtUtc,
        int BaseScore,
        int? FinalScore,
        bool EmptyCardPenaltyApplied,
        int KillsCount,
        int BountyCount,
        Guid CellId,
        int CellRowIndex,
        int CellColIndex,
        string CellType,
        string? CellTitle,
        string? CellDescription,
        int CellCost,
        string? Notes,
        string? TechnicalCancellationReasonCode,
        string? PublicCancellationSummary,
        string? TechnicalCancellationStage
    );

    private sealed record RoundCellMediaRow(Guid RoundId, string Url, int SortOrder);

    private sealed record RoundParticipantRow(
        Guid RoundId,
        Guid UserId,
        string DisplayName,
        DateTime CreatedAtUtc
    );

    private sealed record RoundModifierRow(
        Guid RoundId,
        Guid ModifierResultId,
        Guid ModifierId,
        string ModifierName,
        string ModifierDescription,
        string ModifierCategory,
        string OutcomeStatus,
        int ScoreDelta,
        int KillDelta,
        decimal? MultiplierApplied,
        string? ResolutionDataJson,
        Guid? ResolvedByUserId,
        DateTime? ResolvedAtUtc,
        Guid ActivationId,
        int DefinitionRevision,
        string? ResolutionKind,
        string? ViolationComment,
        string? BehaviorJson
    );

    private sealed record ModifierActivationRow(
        Guid ActivationId,
        Guid ModifierId,
        string ModifierName,
        Guid ActivatedByUserId,
        string? ActivatedByDisplayName,
        DateTime ActivatedAtUtc,
        string Status,
        DateTime? CancelledAtUtc,
        int RefundAmount,
        Guid? ModifierVersionId
    );

    private sealed record PinnedModifierRow(
        Guid ModifierId,
        Guid VersionId,
        int Revision,
        string Name,
        string Description,
        string Category,
        string? IconEmoji,
        string? ActivationCommand,
        int ActivationCost,
        int? MaxActivationsPerRound,
        string[] NormalizedTags,
        string BehaviorV2Json,
        DateTime? EmergencyDisabledAtUtc
    );

    private sealed record PinnedConflictRow(
        Guid VersionId,
        Guid ConflictingModifierId,
        string Name
    );

    private sealed record QuizRoundRow(
        Guid RoundId,
        Guid QuestionId,
        string QuestionCode,
        string QuestionText,
        string CategoryName,
        int Reward,
        string Status,
        DateTime AskedAtUtc,
        DateTime? AnsweredAtUtc,
        string? AnsweredByDisplayName,
        Guid? AnsweredByUserId,
        Guid? AnsweredForUserId,
        string? SubmittedAnswer,
        bool? IsCorrect,
        int? AwardedPoints
    );

    private sealed record QuizManualAwardRow(
        Guid AwardId,
        Guid AwardedToUserId,
        string? AwardedToDisplayName,
        Guid AwardedByUserId,
        string? AwardedByDisplayName,
        int Points,
        string OperationType,
        string? Reason,
        DateTime AwardedAtUtc
    );

    private sealed class LeaderboardAccumulator
    {
        public LeaderboardAccumulator(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; set; }

        public long MainGamePoints { get; set; }

        public long QuizPoints { get; set; }

        public long MainGameRoundsPlayed { get; set; }

        public long QuizRoundsAnswered { get; set; }

        public long CorrectQuizAnswers { get; set; }

        public long ModifiersActivated { get; set; }

        public HashSet<Guid> GamesPlayed { get; } = [];

        public DateTime? LastActivityAtUtc { get; set; }
    }

    private sealed class PlayerStatsAccumulator
    {
        public PlayerStatsAccumulator(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; set; }

        public long Points { get; set; }

        public long EventCount { get; set; }

        public DateTime? LastActivityAtUtc { get; set; }
    }
}
