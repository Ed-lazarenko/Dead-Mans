namespace backend.Api.Contracts;

public sealed record GameFinishIssueDto(string Code, int Count);

public sealed record GameFinishTeamResultDto(
    string TeamId,
    string? TeamName,
    int TeamSlotIndex,
    IReadOnlyList<string> ParticipantNames,
    int RoundsPlayed,
    int? BestScore,
    int PenaltyTotal,
    int? FinalScore,
    int TotalScore,
    int TotalBonusDelta,
    int TotalKills,
    int TotalBounties,
    int? Placement,
    DateTime? LastFinishedAtUtc
);

public sealed record GameFinishSummaryDto(
    string GameId,
    string GameTitle,
    string GameStatus,
    int BoardVersion,
    DateTime? FinishedAtUtc,
    string? FinishedByUserId,
    string? FinishedByDisplayName,
    string? PublicNote,
    int CalculationVersion,
    int CompletedRoundCount,
    int CancelledRoundCount,
    int TotalKills,
    int TotalBounties,
    int QuizTotalPoints,
    int PendingQuizQuestionCount,
    int SkippedQuizQuestionCount,
    IReadOnlyList<GameFinishTeamResultDto> Teams
);

public sealed record GameFinishPreviewDto(
    GameFinishSummaryDto Summary,
    bool CanFinish,
    IReadOnlyList<GameFinishIssueDto> Blockers,
    IReadOnlyList<GameFinishIssueDto> Warnings
);

public sealed record FinishGameRequestDto(
    int ExpectedBoardVersion,
    Guid RequestId,
    IReadOnlyList<string>? AcknowledgedWarningCodes,
    string? Note
);

public sealed record FinishGameResponseDto(GameFinishSummaryDto Summary, bool AlreadyFinished);

public sealed record GameLifecycleChangedEventDto(
    string GameId,
    string Status,
    int BoardVersion,
    DateTime OccurredAtUtc
);
