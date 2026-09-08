using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Api.Mapping;

public static partial class ApiContractMapper
{
    public static UserGameHistoryItemDto ToDto(this UserGameHistoryItem item)
    {
        return new UserGameHistoryItemDto(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.CreatedAtUtc,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.ModifierActivations.Select(ToDto).ToArray(),
            item.QuestionAnswers.Select(ToDto).ToArray(),
            item.ManualQuizAwards.Select(ToDto).ToArray()
        );
    }

    public static UserGameModifierActivationHistoryItemDto ToDto(
        this UserGameModifierActivationHistoryItem item
    )
    {
        return new UserGameModifierActivationHistoryItemDto(item.ModifierId.ToString(), item.ActivatedAtUtc);
    }

    public static UserGameQuestionAnswerHistoryItemDto ToDto(
        this UserGameQuestionAnswerHistoryItem item
    )
    {
        return new UserGameQuestionAnswerHistoryItemDto(
            item.RoundId.ToString(),
            item.QuestionId.ToString(),
            item.QuestionText,
            item.CategoryName,
            item.AnsweredAtUtc,
            item.IsCorrect,
            item.AwardedPoints,
            item.SubmittedAnswer,
            item.AnsweredByUserId?.ToString()
        );
    }

    public static UserGameQuizManualAwardHistoryItemDto ToDto(
        this UserGameQuizManualAwardHistoryItem item
    )
    {
        return new UserGameQuizManualAwardHistoryItemDto(
            item.AwardId.ToString(),
            item.AwardedAtUtc,
            item.AwardedPoints,
            item.AwardedByUserId.ToString(),
            item.AwardedByDisplayName,
            item.OperationType,
            item.Reason
        );
    }

    public static GameHistoryLeaderboardEntryDto ToDto(this GameHistoryLeaderboardEntry item)
    {
        return new GameHistoryLeaderboardEntryDto(
            item.UserId.ToString(),
            item.DisplayName,
            item.MainGamePoints,
            item.QuizPoints,
            item.TotalPoints,
            item.GamesPlayed,
            item.MainGameRoundsPlayed,
            item.QuizRoundsAnswered,
            item.CorrectQuizAnswers,
            item.ModifiersActivated,
            item.LastActivityAtUtc
        );
    }

    public static GameHistoryGameSummaryDto ToDto(this GameHistoryGameSummary item)
    {
        return new GameHistoryGameSummaryDto(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.CreatedAtUtc,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.MainGameRoundCount,
            item.QuizRoundCount,
            item.UniquePlayerCount
        );
    }

    public static GameHistoryGameDetailsDto ToDto(this GameHistoryGameDetails item)
    {
        return new GameHistoryGameDetailsDto(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.CreatedAtUtc,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.MainGame.ToDto(),
            item.Quiz.ToDto(),
            item.FinalResult?.ToDto(),
            item.ModifierSnapshotStatus,
            item.ModifierSnapshots.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryMainGameSectionDto ToDto(this GameHistoryMainGameSection item)
    {
        return new GameHistoryMainGameSectionDto(
            item.PlayerStats.Select(ToDto).ToArray(),
            item.TeamStats.Select(ToDto).ToArray(),
            item.ModifierActivations.Select(ToDto).ToArray(),
            item.Rounds.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryQuizSectionDto ToDto(this GameHistoryQuizSection item)
    {
        return new GameHistoryQuizSectionDto(
            item.TotalPoints,
            item.PlayerStats.Select(ToDto).ToArray(),
            item.Rounds.Select(ToDto).ToArray(),
            item.ManualAwards.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryPlayerSummaryDto ToDto(this GameHistoryPlayerSummary item)
    {
        return new GameHistoryPlayerSummaryDto(
            item.UserId.ToString(),
            item.DisplayName,
            item.Points,
            item.EventCount,
            item.LastActivityAtUtc
        );
    }

    public static GameHistoryModifierActivationItemDto ToDto(
        this GameHistoryModifierActivationItem item
    )
    {
        return new GameHistoryModifierActivationItemDto(
            item.ActivationId.ToString(),
            item.ModifierId.ToString(),
            item.ModifierName,
            item.ActivatedByUserId.ToString(),
            item.ActivatedByDisplayName,
            item.ActivatedAtUtc,
            item.Status,
            item.CancelledAtUtc,
            item.RefundAmount
        );
    }

    public static GameHistoryModifierSnapshotDto ToDto(this GameHistoryModifierSnapshot item) => new(
        item.ModifierId.ToString(), item.VersionId.ToString(), item.Revision, item.Name,
        item.Description, item.Category, item.IconEmoji, item.ActivationCommand,
        item.ActivationCost, item.ActivationLimit.ToDto(), item.NormalizedTags,
        item.BehaviorV2.ToDto(), item.Conflicts.Select(x => new ModifierConflictSnapshotDto(
            x.ModifierId.ToString(), x.Name)).ToArray(), item.SuccessfulActivationsCount,
        item.CancelledActivationsCount, item.ResultsCount, item.IsEmergencyDisabled,
        item.EmergencyDisabledAtUtc);

    public static GameHistoryRoundItemDto ToDto(this GameHistoryRoundItem item)
    {
        return new GameHistoryRoundItemDto(
            item.RoundId.ToString(),
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.Status,
            item.RoundVersion,
            item.StartedAtUtc,
            item.PreparedAtUtc,
            item.GameplayStartedAtUtc,
            item.ReviewedAtUtc,
            item.FinishedAtUtc,
            item.BaseScore,
            item.FinalScore,
            item.EmptyCardPenaltyApplied,
            item.ScoreDetails.ToDto(),
            item.KillsCount,
            item.BountyCount,
            item.CellId.ToString(),
            item.CellRowIndex,
            item.CellColIndex,
            item.CellType,
            item.CellTitle,
            item.CellDescription,
            item.CellCost,
            item.Notes,
            item.TechnicalCancellationReasonCode,
            item.PublicCancellationSummary,
            item.TechnicalCancellationStage,
            item.PurchasesRefunded,
            item.CellMedia.Select(ToDto).ToArray(),
            item.Participants.Select(ToDto).ToArray(),
            item.Modifiers.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryRoundParticipantItemDto ToDto(
        this GameHistoryRoundParticipantItem item
    )
    {
        return new GameHistoryRoundParticipantItemDto(
            item.UserId.ToString(),
            item.DisplayName,
            item.CreatedAtUtc
        );
    }

    public static GameHistoryRoundModifierItemDto ToDto(
        this GameHistoryRoundModifierItem item
    )
    {
        return new GameHistoryRoundModifierItemDto(
            item.ModifierResultId.ToString(),
            item.ModifierId.ToString(),
            item.ModifierName,
            item.ModifierDescription,
            item.ModifierCategory,
            item.OutcomeStatus,
            item.ScoreDelta,
            item.KillDelta,
            item.MultiplierApplied,
            item.ResolutionDataJson,
            item.ResolvedByUserId?.ToString(),
            item.ResolvedAtUtc,
            item.ActivationId.ToString(),
            item.DefinitionRevision,
            item.ResolutionKind,
            item.ViolationComment
        );
    }

    public static GameHistoryQuizRoundItemDto ToDto(this GameHistoryQuizRoundItem item)
    {
        return new GameHistoryQuizRoundItemDto(
            item.RoundId.ToString(),
            item.QuestionId.ToString(),
            item.QuestionCode,
            item.QuestionText,
            item.CategoryName,
            item.Reward,
            item.Status,
            item.AskedAtUtc,
            item.AnsweredAtUtc,
            item.AnsweredByDisplayName,
            item.AnsweredByUserId?.ToString(),
            item.AnsweredForUserId?.ToString(),
            item.AnsweredForDisplayName,
            item.SubmittedAnswer,
            item.IsCorrect,
            item.AwardedPoints
        );
    }

    public static GameHistoryQuizManualAwardItemDto ToDto(this GameHistoryQuizManualAwardItem item)
    {
        return new GameHistoryQuizManualAwardItemDto(
            item.AwardId.ToString(),
            item.AwardedToUserId.ToString(),
            item.AwardedToDisplayName,
            item.AwardedByUserId.ToString(),
            item.AwardedByDisplayName,
            item.AwardedPoints,
            item.OperationType,
            item.Reason,
            item.AwardedAtUtc
        );
    }

}
