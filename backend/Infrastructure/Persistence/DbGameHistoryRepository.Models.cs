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

    private sealed record RoundCellMediaRow(
        Guid RoundId,
        string Bucket,
        string ObjectKey,
        int SortOrder
    );

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
