using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Infrastructure.Configuration;
using backend.Domain.Persistence;
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

        var quizRows = await _dbContext.GameQuestionRounds
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
            .Where(x => !x.Game.IsDeleted)
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
                        x.Value.MainGamePoints,
                        x.Value.QuizPoints,
                        x.Value.MainGamePoints + x.Value.QuizPoints,
                        x.Value.GamesPlayed.Count,
                        x.Value.MainGameRoundsPlayed,
                        x.Value.QuizRoundsAnswered,
                        x.Value.CorrectQuizAnswers,
                        x.Value.ModifiersActivated,
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
            .Where(x => !x.Game.IsDeleted)
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var quizCounts = await _dbContext.GameQuestionRounds
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
            .Where(x => x.GameId == gameId)
            .Select(
                x =>
                    new RoundRow(
                        x.Id,
                        x.TeamId,
                        x.TeamSlotIndexSnapshot,
                        x.Status,
                        x.StartedAtUtc,
                        x.FinishedAtUtc,
                        x.BaseScore,
                        x.FinalScore,
                        x.KillsCount,
                        x.BountyCount,
                        x.BoardCellId,
                        x.CellRowIndex,
                        x.CellColIndex,
                        x.BoardCell.CellType,
                        x.CellTitleSnapshot,
                        x.CellDescriptionSnapshot ?? x.BoardCell.Description,
                        x.CellCostSnapshot,
                        x.Notes
                    )
            )
            .ToArrayAsync(cancellationToken);

        var roundIds = rounds.Select(x => x.RoundId).ToArray();
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
                        x.ModifierMechanicTypeSnapshot,
                        x.OutcomeStatus,
                        x.ScoreDelta,
                        x.KillDelta,
                        x.MultiplierApplied,
                        x.ResolvedByUserId,
                        x.ResolvedAtUtc
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
                        x.ModifierDefinition.Name,
                        x.ActivatedByUserId,
                        x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : null,
                        x.ActivatedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var quizRounds = await _dbContext.GameQuestionRounds
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
                                        item.ModifierMechanicType,
                                        item.OutcomeStatus,
                                        item.ScoreDelta,
                                        item.KillDelta,
                                        item.MultiplierApplied,
                                        item.ResolvedByUserId,
                                        item.ResolvedAtUtc
                                    )
                            )
                            .ToArray()
            );

        var mainPlayerStats = BuildMainGamePlayerStats(
            participants,
            rounds,
            modifierActivations,
            userDisplayNames
        );
        var quizPlayerStats = BuildQuizPlayerStats(quizRounds, manualQuizAwards, userDisplayNames);

        return new GameHistoryGameDetails(
            game.GameId,
            game.Title,
            game.Status,
            game.CreatedAtUtc,
            game.StartedAtUtc,
            game.FinishedAtUtc,
            new GameHistoryMainGameSection(
                mainPlayerStats,
                modifierActivations
                    .Select(
                        x =>
                            new GameHistoryModifierActivationItem(
                                x.ActivationId,
                                x.ModifierId,
                                x.ModifierName,
                                x.ActivatedByUserId,
                                ResolveDisplayName(x.ActivatedByDisplayName, userDisplayNames, x.ActivatedByUserId),
                                x.ActivatedAtUtc
                            )
                    )
                    .ToArray(),
                rounds
                    .OrderBy(x => x.StartedAtUtc)
                    .Select(
                        x =>
                            new GameHistoryRoundItem(
                                x.RoundId,
                                x.TeamId,
                                x.TeamSlotIndex,
                                x.Status,
                                x.StartedAtUtc,
                                x.FinishedAtUtc,
                                x.BaseScore,
                                x.FinalScore,
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
                                cellMediaSnapshotsByRoundId.GetValueOrDefault(x.RoundId)
                                ?? (cellMediaById.TryGetValue(x.CellId, out var cellMedia)
                                    ? cellMedia
                                    : Array.Empty<GameBoardCellMedia>()),
                                participantsByRoundId.GetValueOrDefault(
                                    x.RoundId,
                                    Array.Empty<GameHistoryRoundParticipantItem>()
                                ),
                                modifiersByRoundId.GetValueOrDefault(
                                    x.RoundId,
                                    Array.Empty<GameHistoryRoundModifierItem>()
                                )
                            )
                    )
                    .ToArray()
            ),
            new GameHistoryQuizSection(
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
                                x.AwardedAtUtc
                            )
                    )
                    .ToArray()
            )
        );
    }

    public async Task<IReadOnlyList<UserGameHistoryItem>> GetUserGameHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var modifierGameIds = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.ActivatedByUserId == userId)
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var answeredGameIds = await _dbContext.GameQuestionRounds
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
            .Where(x => x.ActivatedByUserId == userId && gameIds.Contains(x.GameId))
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

        var questionAnswers = await _dbContext.GameQuestionRounds
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
                            x.AwardedByUser != null ? x.AwardedByUser.DisplayName : x.AwardedByUserId.ToString()
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

        var quizPlayers = await _dbContext.GameQuestionRounds
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
            .Where(x => !x.Game.IsDeleted)
            .Select(x => new GamePlayerRow(x.GameId, x.ActivatedByUserId))
            .ToArrayAsync(cancellationToken);

        return mainPlayers
            .Concat(quizPlayers)
            .Concat(manualQuizPlayers)
            .Concat(modifierPlayers)
            .GroupBy(x => x.GameId)
            .ToDictionary(x => x.Key, x => x.Select(item => item.UserId).Distinct().Count());
    }

    private static IReadOnlyList<GameHistoryPlayerSummary> BuildMainGamePlayerStats(
        IReadOnlyList<RoundParticipantRow> participants,
        IReadOnlyList<RoundRow> rounds,
        IReadOnlyList<ModifierActivationRow> modifierActivations,
        IReadOnlyDictionary<Guid, string> userDisplayNames
    )
    {
        var roundLookup = rounds.ToDictionary(x => x.RoundId);
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
                        x.Value.Points,
                        x.Value.EventCount,
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
                        x.Value.Points,
                        x.Value.EventCount,
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
        DateTime StartedAtUtc,
        DateTime? FinishedAtUtc,
        int BaseScore,
        int? FinalScore,
        int KillsCount,
        int BountyCount,
        Guid CellId,
        int CellRowIndex,
        int CellColIndex,
        string CellType,
        string? CellTitle,
        string? CellDescription,
        int CellCost,
        string? Notes
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
        string ModifierMechanicType,
        string OutcomeStatus,
        int ScoreDelta,
        int KillDelta,
        decimal? MultiplierApplied,
        Guid? ResolvedByUserId,
        DateTime? ResolvedAtUtc
    );

    private sealed record ModifierActivationRow(
        Guid ActivationId,
        Guid ModifierId,
        string ModifierName,
        Guid ActivatedByUserId,
        string? ActivatedByDisplayName,
        DateTime ActivatedAtUtc
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
        DateTime AwardedAtUtc
    );

    private sealed class LeaderboardAccumulator
    {
        public LeaderboardAccumulator(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; set; }

        public int MainGamePoints { get; set; }

        public int QuizPoints { get; set; }

        public int MainGameRoundsPlayed { get; set; }

        public int QuizRoundsAnswered { get; set; }

        public int CorrectQuizAnswers { get; set; }

        public int ModifiersActivated { get; set; }

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

        public int Points { get; set; }

        public int EventCount { get; set; }

        public DateTime? LastActivityAtUtc { get; set; }
    }
}
