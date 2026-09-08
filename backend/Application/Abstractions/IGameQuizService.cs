using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public enum AskNextGameQuizQuestionOutcome
{
    Asked,
    NoActiveGame,
    NoAvailableQuestions
}

public sealed record AskNextGameQuizQuestionResult(
    AskNextGameQuizQuestionOutcome Outcome,
    AskedQuizQuestion? AskedQuestion = null
);

public enum AnswerGameQuizRoundOutcome
{
    Answered,
    Incorrect,
    QuizRoundNotFound,
    QuizRoundNotPending,
    PlayerNotFound,
    InvalidAnswer
}

public sealed record AnswerGameQuizRoundResult(
    AnswerGameQuizRoundOutcome Outcome,
    GameQuizRoundSummary? QuizRound = null
);

public enum ManualQuizAwardOutcome
{
    Awarded,
    NoActiveGame,
    PlayerNotFound,
    InvalidPoints,
    InvalidOperation,
    InvalidReason,
    InsufficientPoints,
    DuplicateRequestConflict
}

public sealed record ManualQuizAwardResult(
    ManualQuizAwardOutcome Outcome,
    ManualQuizAwardSummary? Award = null,
    bool StateChanged = false
);

public interface IGameQuizService
{
    Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<AnswerGameQuizRoundResult> AnswerQuizRoundAsync(
        Guid roundId,
        string submittedAnswer,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        CancellationToken cancellationToken = default
    );

    Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(
        ManualQuizAwardInput input,
        Guid awardedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(
        CancellationToken cancellationToken = default
    );
}
