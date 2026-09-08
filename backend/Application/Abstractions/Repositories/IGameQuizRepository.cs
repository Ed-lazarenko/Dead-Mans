using backend.Application.Contracts;
using backend.Application.Abstractions;

namespace backend.Application.Abstractions.Repositories;

public enum SubmitQuizAnswerRepositoryOutcome
{
    Correct,
    Incorrect,
    RoundNotFound,
    RoundNotPending,
    PlayerNotFound
}

public sealed record SubmitQuizAnswerRepositoryResult(
    SubmitQuizAnswerRepositoryOutcome Outcome,
    GameQuizRoundSummary? Round = null
);

public interface IGameQuizRepository
{
    Task<Guid?> GetActiveGameIdAsync(CancellationToken cancellationToken = default);

    Task<AskedQuizQuestion?> AskNextQuizQuestionAsync(
        Guid gameId,
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<SubmitQuizAnswerRepositoryResult> AnswerQuizRoundAsync(
        Guid roundId,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        string submittedAnswer,
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

    Task<GameQuizRoundSummary?> GetQuizRoundAsync(
        Guid roundId,
        CancellationToken cancellationToken = default
    );
}
