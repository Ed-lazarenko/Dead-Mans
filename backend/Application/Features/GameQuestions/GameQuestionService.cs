using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameQuestions;

public sealed class GameQuestionService : IGameQuestionService
{
    private readonly IGameQuestionRepository _repository;

    public GameQuestionService(IGameQuestionRepository repository)
    {
        _repository = repository;
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

        var updated = await _repository.UpdateCategoryAsync(
            categoryId,
            normalizedName,
            cancellationToken
        );
        return updated is null
            ? new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.NotFound)
            : new UpdateGameQuestionCategoryResult(UpdateGameQuestionCategoryOutcome.Updated, updated);
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
        IReadOnlyList<CreateGameQuestionInput> inputs,
        CancellationToken cancellationToken = default
    )
    {
        if (inputs.Count == 0)
        {
            return new ImportGameQuestionsResult(
                ImportGameQuestionsOutcome.InvalidRequest,
                ErrorMessage: "The import file does not contain any questions."
            );
        }

        var normalizedInputs = new List<CreateGameQuestionInput>(inputs.Count);
        for (var index = 0; index < inputs.Count; index++)
        {
            if (!GameQuestionValidator.TryNormalizeCreate(inputs[index], out var normalized))
            {
                return new ImportGameQuestionsResult(
                    ImportGameQuestionsOutcome.InvalidRequest,
                    ErrorMessage: $"Question #{index + 1} is invalid."
                );
            }

            normalizedInputs.Add(normalized);
        }

        var duplicateExternalCode = normalizedInputs
            .Where(input => !string.IsNullOrWhiteSpace(input.ExternalCode))
            .GroupBy(input => input.ExternalCode!, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateExternalCode is not null)
        {
            return new ImportGameQuestionsResult(
                ImportGameQuestionsOutcome.DuplicateCode,
                ErrorMessage: $"External code '{duplicateExternalCode.Key}' is duplicated in the import file."
            );
        }

        return await _repository.ImportQuestionsAsync(normalizedInputs, cancellationToken);
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

    public async Task<AskNextGameQuestionResult> AskNextAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _repository.GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new AskNextGameQuestionResult(AskNextGameQuestionOutcome.NoActiveGame);
        }

        var askedQuestion = await _repository.AskNextQuestionAsync(
            activeGameId.Value,
            askedByUserId,
            cancellationToken
        );
        if (askedQuestion is null)
        {
            return new AskNextGameQuestionResult(AskNextGameQuestionOutcome.NoAvailableQuestions);
        }

        return new AskNextGameQuestionResult(AskNextGameQuestionOutcome.Asked, askedQuestion);
    }

    public async Task<AnswerGameQuestionResult> AnswerRoundAsync(
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

        var round = await _repository.GetRoundAsync(roundId, cancellationToken);
        if (round is null)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.RoundNotFound);
        }

        if (round.Status != GameQuestionRoundStatusValue.Asked)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.RoundNotPending, round);
        }

        var updatedRound = await _repository.AnswerRoundAsync(
            roundId,
            answeredByUserId,
            answeredForUserId,
            answeredByDisplayName,
            submittedAnswer,
            cancellationToken
        );
        if (updatedRound is null)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.RoundNotPending, round);
        }

        return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.Answered, updatedRound);
    }

    public Task<IReadOnlyList<GameQuestionRoundSummary>> GetGameHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetGameHistoryAsync(gameId, cancellationToken);
    }
}
