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

public enum AnswerGameQuestionOutcome
{
    Answered,
    QuizRoundNotFound,
    QuizRoundNotPending,
    InvalidAnswer
}

public sealed record AnswerGameQuestionResult(
    AnswerGameQuestionOutcome Outcome,
    GameQuizRoundSummary? QuizRound = null
);

public enum ManualQuizAwardOutcome
{
    Awarded,
    NoActiveGame,
    PlayerNotFound,
    InvalidPoints
}

public sealed record ManualQuizAwardResult(
    ManualQuizAwardOutcome Outcome,
    ManualQuizAwardSummary? Award = null
);

public enum CreateGameQuestionOutcome
{
    Created,
    InvalidRequest,
    DuplicateCode,
    CategoryNotFound
}

public sealed record CreateGameQuestionResult(
    CreateGameQuestionOutcome Outcome,
    GameQuestionCatalogItem? Question = null
);

public enum UpdateGameQuestionOutcome
{
    Updated,
    NotFound,
    InvalidRequest,
    CategoryNotFound
}

public sealed record UpdateGameQuestionResult(
    UpdateGameQuestionOutcome Outcome,
    GameQuestionCatalogItem? Question = null
);

public enum CreateGameQuestionCategoryOutcome
{
    Created,
    Existing,
    InvalidRequest
}

public sealed record CreateGameQuestionCategoryResult(
    CreateGameQuestionCategoryOutcome Outcome,
    GameQuestionCategoryItem? Category = null
);

public enum UpdateGameQuestionCategoryOutcome
{
    Updated,
    NotFound,
    InvalidRequest,
    Protected
}

public sealed record UpdateGameQuestionCategoryResult(
    UpdateGameQuestionCategoryOutcome Outcome,
    GameQuestionCategoryItem? Category = null
);

public enum DeleteGameQuestionCategoryOutcome
{
    Deleted,
    NotFound,
    NotEmpty,
    Protected
}

public sealed record DeleteGameQuestionCategoryResult(DeleteGameQuestionCategoryOutcome Outcome);

public sealed record ImportGameQuestionsResult(
    int ImportedCount = 0,
    IReadOnlyList<ImportGameQuestionSkippedItem>? SkippedQuestions = null
);

public interface IGameQuestionService
{
    Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        Guid? categoryId,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameQuestionCategoryItem>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCategoryItem> EnsureFallbackCategoryAsync(
        CancellationToken cancellationToken = default
    );

    Task<CreateGameQuestionCategoryResult> CreateCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken = default
    );

    Task<DeleteGameQuestionCategoryResult> DeleteCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameQuestionCategoryResult> UpdateCategoryAsync(
        Guid categoryId,
        string categoryName,
        CancellationToken cancellationToken = default
    );

    Task<CreateGameQuestionResult> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameQuestionResult> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    Task<ImportGameQuestionsResult> ImportQuestionsAsync(
        IReadOnlyList<ImportGameQuestionInput> inputs,
        CancellationToken cancellationToken = default
    );

    Task<bool> SetQuestionEnabledAsync(
        Guid questionId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    );

    Task<bool> SoftDeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<bool> SetCategoryEnabledAsync(
        Guid categoryId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    );

    Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<AnswerGameQuestionResult> AnswerQuizRoundAsync(
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
