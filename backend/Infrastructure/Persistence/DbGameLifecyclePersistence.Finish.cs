using backend.Application.Contracts;
using backend.Application.Features.Scoring;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameLifecyclePersistence
{
    private static readonly string[] NonTerminalRoundStatuses =
    [
        GameRoundStatusValue.AwaitingModifiers,
        GameRoundStatusValue.Preparing,
        GameRoundStatusValue.InProgress,
        GameRoundStatusValue.ReviewingResults
    ];

    public async Task<GameFinishPreviewResult> GetFinishPreviewAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var gameExists = await _dbContext.Games.AsNoTracking().AnyAsync(
            x => x.Id == gameId && !x.IsDeleted,
            cancellationToken
        );
        if (!gameExists)
        {
            return new GameFinishPreviewResult(GameLifecycleErrorCode.GameNotFound, null);
        }

        var preview = await BuildActiveFinishPreviewAsync(gameId, cancellationToken);
        return preview is null
            ? new GameFinishPreviewResult(GameLifecycleErrorCode.GameNotActive, null)
            : new GameFinishPreviewResult(GameLifecycleErrorCode.None, preview);
    }

    public async Task<FinishGameResult> FinishGameAsync(
        Guid gameId,
        FinishGameInput input,
        Guid finishedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {gameId} FOR UPDATE""",
                cancellationToken
            );
        }

        var game = await _dbContext.Games
            .Include(x => x.Board)
            .Include(x => x.Finalization)
            .ThenInclude(x => x!.TeamResults)
            .FirstOrDefaultAsync(x => x.Id == gameId && !x.IsDeleted, cancellationToken);
        if (game is null)
        {
            return new FinishGameResult(GameLifecycleErrorCode.GameNotFound, null);
        }

        if (game.Finalization is not null)
        {
            var existing = await LoadPersistedFinishSummaryAsync(gameId, cancellationToken);
            return new FinishGameResult(
                existing is null ? GameLifecycleErrorCode.GameNotActive : GameLifecycleErrorCode.None,
                existing,
                existing is not null
            );
        }

        if (game.Status != GameStatusValue.Active || game.Board is null)
        {
            return new FinishGameResult(GameLifecycleErrorCode.GameNotActive, null);
        }

        if (game.Board.Version != input.ExpectedBoardVersion)
        {
            return new FinishGameResult(GameLifecycleErrorCode.FinishStaleVersion, null);
        }

        var requestIdInUse = await _dbContext.GameFinalizations.AsNoTracking().AnyAsync(
            x => x.RequestId == input.RequestId && x.GameId != gameId,
            cancellationToken
        );
        if (requestIdInUse)
        {
            return new FinishGameResult(GameLifecycleErrorCode.FinishInvalidRequest, null);
        }

        var preview = await BuildActiveFinishPreviewAsync(gameId, cancellationToken);
        if (preview is null)
        {
            return new FinishGameResult(GameLifecycleErrorCode.GameNotActive, null);
        }

        if (preview.Blockers.Any(x => x.Code == GameFinishBlockerCodes.RoundInProgress))
        {
            return new FinishGameResult(GameLifecycleErrorCode.FinishRoundInProgress, null);
        }

        if (preview.Blockers.Any(x => x.Code == GameFinishBlockerCodes.ModifierStateInvalid))
        {
            return new FinishGameResult(GameLifecycleErrorCode.FinishModifierStateInvalid, null);
        }

        if (preview.Warnings.Any(x => !input.AcknowledgedWarningCodes.Contains(x.Code)))
        {
            return new FinishGameResult(
                GameLifecycleErrorCode.FinishWarningsNotAcknowledged,
                null
            );
        }

        var finishedByDisplayName = await _dbContext.Users
            .Where(x => x.Id == finishedByUserId && x.IsActive)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(finishedByDisplayName))
        {
            return new FinishGameResult(GameLifecycleErrorCode.FinishInvalidRequest, null);
        }

        var now = DateTime.UtcNow;
        var pendingQuizRounds = await _dbContext.GameQuizRounds
            .Where(x => x.GameId == gameId && x.Status == GameQuizRoundStatusValue.Asked)
            .ToArrayAsync(cancellationToken);
        foreach (var quizRound in pendingQuizRounds)
        {
            quizRound.Status = GameQuizRoundStatusValue.Skipped;
        }

        var finalization = new GameFinalization
        {
            GameId = gameId,
            RequestId = input.RequestId,
            FinishedByUserId = finishedByUserId,
            FinishedByDisplayNameSnapshot = finishedByDisplayName,
            FinishedAtUtc = now,
            PublicNote = string.IsNullOrWhiteSpace(input.PublicNote) ? null : input.PublicNote.Trim(),
            CalculationVersion = GameTeamResultCalculator.CalculationVersion,
            CompletedRoundCount = preview.Summary.CompletedRoundCount,
            CancelledRoundCount = preview.Summary.CancelledRoundCount,
            TotalKills = preview.Summary.TotalKills,
            TotalBounties = preview.Summary.TotalBounties,
            QuizTotalPoints = preview.Summary.QuizTotalPoints,
            SkippedQuizQuestionCount = pendingQuizRounds.Length,
            TeamResults = preview.Summary.Teams
                .Select(team => new GameTeamFinalResult
                {
                    GameId = gameId,
                    TeamId = team.TeamId,
                    TeamNameSnapshot = team.TeamName,
                    TeamSlotIndexSnapshot = team.TeamSlotIndex,
                    ParticipantNamesSnapshot = team.ParticipantNames.ToArray(),
                    RoundsPlayed = team.RoundsPlayed,
                    BestScore = team.BestScore,
                    PenaltyTotal = team.PenaltyTotal,
                    FinalScore = team.FinalScore,
                    TotalScore = team.TotalScore,
                    TotalBonusDelta = team.TotalBonusDelta,
                    TotalKills = team.TotalKills,
                    TotalBounties = team.TotalBounties,
                    Placement = team.Placement,
                    LastFinishedAtUtc = team.LastFinishedAtUtc
                })
                .ToList()
        };

        game.ActiveTeamId = null;
        game.Status = GameStatusValue.Finished;
        game.FinishedAtUtc = now;
        game.Board.Version += 1;
        _dbContext.GameFinalizations.Add(finalization);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Game {GameId} was finalized by user {UserId} with {TeamCount} team snapshots.",
            gameId,
            finishedByUserId,
            finalization.TeamResults.Count
        );

        return new FinishGameResult(
            GameLifecycleErrorCode.None,
            MapPersistedSummary(game, finalization, pendingQuizQuestionCount: 0),
            false
        );
    }

    private async Task<GameFinishPreview?> BuildActiveFinishPreviewAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Id == gameId && x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Title,
                BoardVersion = x.Board != null ? x.Board.Version : 0
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        var teams = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.Status == TeamStatusValue.Confirmed
                && x.DisbandedAtUtc == null
            )
            .Select(x => new
            {
                x.Id,
                x.Name,
                TeamSlotIndex = x.Slot != null ? x.Slot.SlotIndex : 0,
                x.IsPlayed,
                ParticipantNames = x.Members
                    .Where(member => member.LeftAtUtc == null && member.User != null)
                    .OrderBy(member => member.JoinedAtUtc)
                    .Select(member => member.User!.DisplayName)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        var rounds = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .Select(x => new
            {
                x.Id,
                x.TeamId,
                x.Status,
                x.FinishedAtUtc,
                x.StartedAtUtc,
                x.BaseScore,
                x.FinalScore,
                x.EmptyCardPenaltyApplied,
                x.KillsCount,
                x.BountyCount,
                ModifierKillDelta = x.ModifierResults.Sum(result => (int?)result.KillDelta) ?? 0
            })
            .ToArrayAsync(cancellationToken);

        var completedRounds = rounds
            .Where(x => x.Status == GameRoundStatusValue.Completed)
            .ToArray();
        var roundFactsByTeam = completedRounds
            .GroupBy(x => x.TeamId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<GameTeamRoundScoreFact>)group
                    .Select(round =>
                    {
                        var finalScore = round.FinalScore ?? 0;
                        var penalty = finalScore < 0
                            ? SaturatingInt32.From(Math.Abs((long)finalScore))
                            : 0;
                        var bonus = round.EmptyCardPenaltyApplied
                            ? finalScore
                            : SaturatingInt32.From((long)finalScore - round.BaseScore);
                        return new GameTeamRoundScoreFact(
                            round.Id,
                            finalScore,
                            penalty,
                            bonus,
                            SaturatingInt32.From((long)round.KillsCount + round.ModifierKillDelta),
                            round.BountyCount,
                            round.FinishedAtUtc ?? round.StartedAtUtc
                        );
                    })
                    .ToArray()
            );

        var calculatedTeams = GameTeamResultCalculator.Calculate(
            teams.Select(team => new GameTeamResultCalculationInput(
                team.Id,
                team.Name,
                team.TeamSlotIndex,
                team.ParticipantNames,
                roundFactsByTeam.GetValueOrDefault(
                    team.Id,
                    Array.Empty<GameTeamRoundScoreFact>()
                )
            ))
        );

        var pendingQuizQuestionCount = await _dbContext.GameQuizRounds.AsNoTracking().CountAsync(
            x => x.GameId == gameId && x.Status == GameQuizRoundStatusValue.Asked,
            cancellationToken
        );
        var quizPoints = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .SumAsync(x => (long?)(x.AwardedPoints ?? 0), cancellationToken) ?? 0;
        quizPoints += await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .SumAsync(x => (long?)x.Points, cancellationToken) ?? 0;

        var activeRoundCount = rounds.Count(x => NonTerminalRoundStatuses.Contains(x.Status));
        var invalidModifierCount = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .CountAsync(
                x =>
                    x.GameId == gameId
                    && x.ArchivedAtUtc == null
                    && (x.Round.Status == GameRoundStatusValue.Completed
                        || x.Round.Status == GameRoundStatusValue.Cancelled),
                cancellationToken
            );
        var blockers = new List<GameFinishIssue>();
        if (activeRoundCount > 0)
        {
            blockers.Add(new GameFinishIssue(GameFinishBlockerCodes.RoundInProgress, activeRoundCount));
        }
        if (invalidModifierCount > 0)
        {
            blockers.Add(
                new GameFinishIssue(GameFinishBlockerCodes.ModifierStateInvalid, invalidModifierCount)
            );
        }

        var warnings = new List<GameFinishIssue>();
        var unplayedTeamCount = teams.Count(x => !x.IsPlayed);
        if (unplayedTeamCount > 0)
        {
            warnings.Add(new GameFinishIssue(GameFinishWarningCodes.UnplayedTeams, unplayedTeamCount));
        }
        if (completedRounds.Length == 0)
        {
            warnings.Add(new GameFinishIssue(GameFinishWarningCodes.NoCompletedRounds));
        }

        var teamResults = calculatedTeams.Select(MapCalculatedTeam).ToArray();
        var summary = new GameFinishSummary(
            game.Id,
            game.Title,
            GameStatusValue.Active,
            game.BoardVersion,
            null,
            null,
            null,
            null,
            GameTeamResultCalculator.CalculationVersion,
            completedRounds.Length,
            rounds.Count(x => x.Status == GameRoundStatusValue.Cancelled),
            SaturatingInt32.From(teamResults.Sum(x => (long)x.TotalKills)),
            SaturatingInt32.From(teamResults.Sum(x => (long)x.TotalBounties)),
            SaturatingInt32.From(quizPoints),
            pendingQuizQuestionCount,
            0,
            teamResults
        );
        return new GameFinishPreview(summary, blockers.Count == 0, blockers, warnings);
    }

    private async Task<GameFinishSummary?> LoadPersistedFinishSummaryAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var finalization = await _dbContext.GameFinalizations
            .AsNoTracking()
            .Include(x => x.Game)
            .ThenInclude(x => x.Board)
            .Include(x => x.TeamResults)
            .FirstOrDefaultAsync(x => x.GameId == gameId && !x.Game.IsDeleted, cancellationToken);
        return finalization is null
            ? null
            : MapPersistedSummary(finalization.Game, finalization, pendingQuizQuestionCount: 0);
    }

    private static GameFinishSummary MapPersistedSummary(
        Game game,
        GameFinalization finalization,
        int pendingQuizQuestionCount
    ) =>
        new(
            game.Id,
            game.Title,
            game.Status,
            game.Board?.Version ?? 0,
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
            pendingQuizQuestionCount,
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

    private static GameFinishTeamResult MapCalculatedTeam(CalculatedGameTeamResult team) =>
        new(
            team.TeamId,
            team.TeamName,
            team.TeamSlotIndex,
            team.ParticipantNames,
            team.RoundsPlayed,
            team.BestScore,
            team.PenaltyTotal,
            team.FinalScore,
            team.TotalScore,
            team.TotalBonusDelta,
            team.TotalKills,
            team.TotalBounties,
            team.Placement,
            team.LastFinishedAtUtc
        );
}
