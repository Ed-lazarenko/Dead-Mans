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
    private async Task<IReadOnlyDictionary<Guid, string>> LoadUserDisplayNamesAsync(
        Guid[] userIds,
        CancellationToken cancellationToken
    )
    {
        if (userIds.Length == 0)
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

        var quizPlayers = await _dbContext.GameQuizCorrectAnswers
            .AsNoTracking()
            .Where(x => !x.QuizRound.Game!.IsDeleted)
            .Select(x => new GamePlayerRow(x.GameId, x.AwardedToUserId))
            .ToArrayAsync(cancellationToken);

        var manualQuizPlayers = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment
                && !x.Game.IsDeleted)
            .Select(x => new GamePlayerRow(x.GameId, x.UserId))
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

    private static GameHistoryTeamLeaderboardEntry[] BuildTeamLeaderboard(
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

    private static GameHistoryPlayerSummary[] BuildMainGamePlayerStats(
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

    private static GameHistoryPlayerSummary[] BuildQuizPlayerStats(
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

}
