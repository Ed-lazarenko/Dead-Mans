using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Domain.Persistence;
using backend.Messaging;

namespace backend.Application.Features.GameQuestions;

public sealed class GameQuestionService : IGameQuestionService
{
    private readonly IGameQuestionRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameQuestionService> _logger;

    public GameQuestionService(
        IGameQuestionRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameQuestionService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        Guid? categoryId,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetCatalogAsync(categoryId, search, includeDisabled, cancellationToken);
    }

    public Task<IReadOnlyList<GameQuestionCategoryItem>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetCategoriesAsync(cancellationToken);
    }

    public Task<GameQuestionCategoryItem> EnsureFallbackCategoryAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.EnsureFallbackCategoryAsync(cancellationToken);
    }

    public async Task<CreateGameQuestionCategoryResult> CreateCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = (categoryName ?? string.Empty).Trim();
        if (normalizedName.Length is 0 or > GameQuestionValidator.MaxCategoryLength)
        {
            return new CreateGameQuestionCategoryResult(CreateGameQuestionCategoryOutcome.InvalidRequest);
        }

        var existing = await _repository.GetCategoryAsync(normalizedName, cancellationToken);
        if (existing is not null)
        {
            return new CreateGameQuestionCategoryResult(CreateGameQuestionCategoryOutcome.Existing, existing);
        }

        var created = await _repository.CreateCategoryAsync(normalizedName, cancellationToken);
        return new CreateGameQuestionCategoryResult(CreateGameQuestionCategoryOutcome.Created, created);
    }

    public async Task<DeleteGameQuestionCategoryResult> DeleteCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _repository.DeleteCategoryAsync(categoryId, cancellationToken);
        return new DeleteGameQuestionCategoryResult(outcome);
    }

    public async Task<UpdateGameQuestionCategoryResult> UpdateCategoryAsync(
        Guid categoryId,
        string categoryName,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = (categoryName ?? string.Empty).Trim();
        if (normalizedName.Length is 0 or > GameQuestionValidator.MaxCategoryLength)
        {
            return new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.InvalidRequest);
        }

        var existingCategory = await _repository.GetCategoryAsync(categoryId, cancellationToken);
        if (existingCategory is null)
        {
            return new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.NotFound);
        }

        if (existingCategory.IsProtected)
        {
            return new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.Protected);
        }

        var updated = await _repository.UpdateCategoryAsync(
            categoryId,
            normalizedName,
            cancellationToken
        );
        if (updated is null)
        {
            return new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.NotFound);
        }

        return new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.Updated, updated);
    }

    public async Task<CreateGameQuestionResult> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameQuestionValidator.TryNormalizeCreate(input, out var normalized))
        {
            return new CreateGameQuestionResult(CreateGameQuestionOutcome.InvalidRequest);
        }

        if (!await _repository.CategoryExistsAsync(normalized.CategoryId, cancellationToken))
        {
            return new CreateGameQuestionResult(CreateGameQuestionOutcome.CategoryNotFound);
        }

        var created = await _repository.CreateQuestionAsync(normalized, cancellationToken);
        return created is null
            ? new CreateGameQuestionResult(CreateGameQuestionOutcome.DuplicateCode)
            : new CreateGameQuestionResult(CreateGameQuestionOutcome.Created, created);
    }

    public async Task<UpdateGameQuestionResult> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameQuestionValidator.TryNormalizeUpdate(input, out var normalized))
        {
            return new UpdateGameQuestionResult(UpdateGameQuestionOutcome.InvalidRequest);
        }

        if (!await _repository.CategoryExistsAsync(normalized.CategoryId, cancellationToken))
        {
            return new UpdateGameQuestionResult(UpdateGameQuestionOutcome.CategoryNotFound);
        }

        var updated = await _repository.UpdateQuestionAsync(questionId, normalized, cancellationToken);
        return updated is null
            ? new UpdateGameQuestionResult(UpdateGameQuestionOutcome.NotFound)
            : new UpdateGameQuestionResult(UpdateGameQuestionOutcome.Updated, updated);
    }

    public async Task<ImportGameQuestionsResult> ImportQuestionsAsync(
        IReadOnlyList<ImportGameQuestionInput> inputs,
        CancellationToken cancellationToken = default
    )
    {
        if (inputs.Count == 0)
        {
            return new ImportGameQuestionsResult(0, Array.Empty<ImportGameQuestionSkippedItem>());
        }

        var skipped = new List<ImportGameQuestionSkippedItem>();
        var normalizedInputs = new List<ImportGameQuestionCandidate>(inputs.Count);
        var seenExternalCodes = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            var candidate = new CreateGameQuestionInput(
                input.ExternalCode,
                input.CategoryId,
                input.Text ?? string.Empty,
                input.Answer ?? string.Empty,
                input.Reward ?? -1,
                input.IsEnabled ?? false,
                input.Priority ?? 0
            );

            if (!GameQuestionValidator.TryNormalizeCreate(candidate, out var normalized))
            {
                skipped.Add(
                    new ImportGameQuestionSkippedItem(
                        input.RowNumber,
                        input.Text?.Trim(),
                        AppMessages.ErrorCodes.GameQuestionImportInvalidFields,
                        "Missing or invalid required fields. Each question must include text, answer, and a non-negative reward.",
                        input.SourceQuestion
                    )
                );
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalized.ExternalCode)
                && !seenExternalCodes.Add(normalized.ExternalCode))
            {
                skipped.Add(
                    new ImportGameQuestionSkippedItem(
                        input.RowNumber,
                        normalized.Text,
                        AppMessages.ErrorCodes.GameQuestionImportDuplicateCodeInFile,
                        $"External code '{normalized.ExternalCode}' is duplicated inside the import file.",
                        input.SourceQuestion
                    )
                );
                continue;
            }

            normalizedInputs.Add(
                new ImportGameQuestionCandidate(
                    input.RowNumber,
                    normalized.Text,
                    normalized,
                    input.SourceQuestion
                )
            );
        }

        var repositoryResult = await _repository.ImportQuestionsAsync(normalizedInputs, cancellationToken);
        var mergedSkipped = skipped
            .Concat(repositoryResult.SkippedQuestions ?? Array.Empty<ImportGameQuestionSkippedItem>())
            .OrderBy(item => item.RowNumber)
            .ToArray();

        return new ImportGameQuestionsResult(
            repositoryResult.ImportedCount,
            mergedSkipped
        );
    }

    public Task<bool> SetQuestionEnabledAsync(
        Guid questionId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.SetQuestionEnabledAsync(questionId, isEnabled, cancellationToken);
    }

    public Task<bool> SoftDeleteQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.SoftDeleteQuestionAsync(questionId, cancellationToken);
    }

    public Task<bool> SetCategoryEnabledAsync(
        Guid categoryId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.SetCategoryEnabledAsync(categoryId, isEnabled, cancellationToken);
    }

    public async Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _repository.GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.NoActiveGame);
        }

        var askedQuestion = await _repository.AskNextQuizQuestionAsync(
            activeGameId.Value,
            askedByUserId,
            cancellationToken
        );
        if (askedQuestion is null)
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.NoAvailableQuestions);
        }

        await PublishQuizStateChangedBestEffortAsync(
            askedQuestion.GameId,
            GameQuizStateChangeKinds.QuestionAsked,
            askedQuestion.AskedAtUtc,
            cancellationToken
        );

        return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.Asked, askedQuestion);
    }

    public async Task<AnswerGameQuestionResult> AnswerQuizRoundAsync(
        Guid roundId,
        string submittedAnswer,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(submittedAnswer))
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.InvalidAnswer);
        }

        var round = await _repository.GetQuizRoundAsync(roundId, cancellationToken);
        if (round is null)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.QuizRoundNotFound);
        }

        if (round.Status != GameQuizRoundStatusValue.Asked)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.QuizRoundNotPending, round);
        }

        var updatedRound = await _repository.AnswerQuizRoundAsync(
            roundId,
            answeredByUserId,
            answeredForUserId,
            answeredByDisplayName,
            submittedAnswer,
            cancellationToken
        );
        if (updatedRound is null)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.QuizRoundNotPending, round);
        }

        await PublishQuizStateChangedBestEffortAsync(
            updatedRound.GameId,
            GameQuizStateChangeKinds.QuestionAnswered,
            updatedRound.AnsweredAtUtc ?? DateTime.UtcNow,
            cancellationToken
        );

        return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.Answered, updatedRound);
    }

    public async Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(
        ManualQuizAwardInput input,
        Guid awardedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (input.Points <= 0)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.InvalidPoints);
        }

        var result = await _repository.AwardManualQuizPointsAsync(
            input,
            awardedByUserId,
            cancellationToken
        );

        if (result.Outcome == ManualQuizAwardOutcome.Awarded && result.Award is not null)
        {
            await PublishQuizStateChangedBestEffortAsync(
                result.Award.GameId,
                GameQuizStateChangeKinds.ManualAwardGranted,
                result.Award.AwardedAtUtc,
                cancellationToken
            );
        }

        return result;
    }

    public Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetManualQuizAwardPlayersAsync(cancellationToken);
    }

    private Task PublishQuizStateChangedBestEffortAsync(
        Guid gameId,
        string changeKind,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken
    )
    {
        return RealtimePublishGuard.TryPublishAsync(
            () => _eventsPublisher.PublishQuizStateChangedAsync(
                new GameQuizStateChangedEvent(gameId, changeKind, occurredAtUtc),
                cancellationToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameQuizStateChangedPublishFailed,
            gameId,
            changeKind
        );
    }
}
