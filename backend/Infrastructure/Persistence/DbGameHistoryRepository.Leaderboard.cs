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
                        x.Round.FinishedAtUtc ?? x.Round.CreatedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var quizRows = await _dbContext.GameQuizCorrectAnswers
            .AsNoTracking()
            .Where(x => !x.QuizRound.Game!.IsDeleted)
            .Select(
                x =>
                    new LeaderboardQuizRow(
                        x.AwardedToUserId,
                        x.DisplayNameSnapshot,
                        x.GameId,
                        x.PointEntries
                            .Where(entry => entry.EntryType == GameQuizPointEntryTypeValue.QuizReward)
                            .Sum(entry => entry.PointsDelta),
                        true,
                        x.AnsweredAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var manualQuizRows = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment
                && !x.Game.IsDeleted)
            .Select(
                x =>
                    new LeaderboardQuizRow(
                        x.UserId,
                        x.User.DisplayName,
                        x.GameId,
                        x.PointsDelta,
                        true,
                        x.OccurredAtUtc
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

}
