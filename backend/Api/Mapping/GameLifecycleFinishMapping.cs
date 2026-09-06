using backend.Api.Contracts;
using backend.Application.Contracts;

namespace backend.Api.Mapping;

public static class GameLifecycleFinishMapping
{
    public static GameFinishIssueDto ToDto(this GameFinishIssue item) =>
        new(item.Code, item.Count);

    public static GameFinishTeamResultDto ToDto(this GameFinishTeamResult item) =>
        new(
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.ParticipantNames,
            item.RoundsPlayed,
            item.BestScore,
            item.PenaltyTotal,
            item.FinalScore,
            item.TotalScore,
            item.TotalBonusDelta,
            item.TotalKills,
            item.TotalBounties,
            item.Placement,
            item.LastFinishedAtUtc
        );

    public static GameFinishSummaryDto ToDto(this GameFinishSummary item) =>
        new(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.BoardVersion,
            item.FinishedAtUtc,
            item.FinishedByUserId?.ToString(),
            item.FinishedByDisplayName,
            item.PublicNote,
            item.CalculationVersion,
            item.CompletedRoundCount,
            item.CancelledRoundCount,
            item.TotalKills,
            item.TotalBounties,
            item.QuizTotalPoints,
            item.PendingQuizQuestionCount,
            item.SkippedQuizQuestionCount,
            item.Teams.Select(ToDto).ToArray()
        );

    public static GameFinishPreviewDto ToDto(this GameFinishPreview item) =>
        new(
            item.Summary.ToDto(),
            item.CanFinish,
            item.Blockers.Select(ToDto).ToArray(),
            item.Warnings.Select(ToDto).ToArray()
        );

    public static GameLifecycleChangedEventDto ToDto(this GameLifecycleChangedEvent item) =>
        new(item.GameId.ToString(), item.Status, item.BoardVersion, item.OccurredAtUtc);
}
