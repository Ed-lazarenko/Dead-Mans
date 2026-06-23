using System.Linq.Expressions;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameQuestionRepository : IGameQuestionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DbGameQuestionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        Guid? categoryId,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedSearch = NormalizeFilter(search);

        var query = _dbContext.QuestionDefinitions
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var searchLower = normalizedSearch.ToLowerInvariant();
            query = query.Where(
                x =>
                    EF.Functions.ILike(x.Text, $"%{searchLower}%")
                    || EF.Functions.ILike(x.Answer, $"%{searchLower}%")
            );
        }

        if (!includeDisabled)
        {
            query = query.Where(x => x.IsEnabled);
        }

        return await query
            .OrderBy(x => x.CategoryDefinition!.Name)
            .ThenBy(x => x.Priority)
            .Select(ToCatalogItemSelector())
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameQuestionCategoryItem>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.QuestionCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(
                x =>
                    new GameQuestionCategoryItem(
                        x.Id,
                        x.Name,
                        x.Questions.Count(question => !question.IsDeleted)
                    )
            )
            .ToArrayAsync(cancellationToken);
    }

    public async Task<GameQuestionCategoryItem?> GetCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = NormalizeFilter(categoryName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        return await _dbContext.QuestionCategories
            .AsNoTracking()
            .Where(x => x.Name == normalizedName)
            .Select(
                x =>
                    new GameQuestionCategoryItem(
                        x.Id,
                        x.Name,
                        x.Questions.Count(question => !question.IsDeleted)
                    )
            )
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GameQuestionCategoryItem> CreateCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = NormalizeFilter(categoryName);
        var now = DateTime.UtcNow;
        var entity = new QuestionCategory
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.QuestionCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameQuestionCategoryItem(entity.Id, entity.Name, 0);
    }

    public async Task<DeleteGameQuestionCategoryOutcome> DeleteCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        if (categoryId == Guid.Empty)
        {
            return DeleteGameQuestionCategoryOutcome.NotFound;
        }

        var category = await _dbContext.QuestionCategories.FirstOrDefaultAsync(
            x => x.Id == categoryId,
            cancellationToken
        );
        if (category is null)
        {
            return DeleteGameQuestionCategoryOutcome.NotFound;
        }

        var hasQuestions = await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .AnyAsync(
                x => x.CategoryId == categoryId && !x.IsDeleted,
                cancellationToken
            );
        if (hasQuestions)
        {
            return DeleteGameQuestionCategoryOutcome.NotEmpty;
        }

        _dbContext.QuestionCategories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return DeleteGameQuestionCategoryOutcome.Deleted;
    }

    public async Task<GameQuestionCategoryItem?> UpdateCategoryAsync(
        Guid categoryId,
        string categoryName,
        CancellationToken cancellationToken = default
    )
    {
        if (categoryId == Guid.Empty)
        {
            return null;
        }

        var category = await _dbContext.QuestionCategories
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        category.Name = NormalizeFilter(categoryName);
        category.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameQuestionCategoryItem(
            category.Id,
            category.Name,
            category.Questions.Count(question => !question.IsDeleted)
        );
    }

    public Task<bool> CategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        return _dbContext.QuestionCategories
            .AsNoTracking()
            .AnyAsync(x => x.Id == categoryId, cancellationToken);
    }

    public async Task<bool> SetQuestionEnabledAsync(
        Guid questionId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        var question = await _dbContext.QuestionDefinitions.FirstOrDefaultAsync(
            x => x.Id == questionId && !x.IsDeleted,
            cancellationToken
        );
        if (question is null)
        {
            return false;
        }

        question.IsEnabled = isEnabled;
        question.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SoftDeleteQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default
    )
    {
        var question = await _dbContext.QuestionDefinitions.FirstOrDefaultAsync(
            x => x.Id == questionId && !x.IsDeleted,
            cancellationToken
        );
        if (question is null)
        {
            return false;
        }

        question.IsDeleted = true;
        question.DeletedAtUtc = DateTime.UtcNow;
        question.IsEnabled = false;
        question.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GameQuestionCatalogItem?> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        var externalCode = string.IsNullOrWhiteSpace(input.ExternalCode)
            ? GenerateExternalCode()
            : input.ExternalCode.Trim();

        var codeTaken = await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .AnyAsync(x => x.ExternalCode == externalCode, cancellationToken);
        if (codeTaken)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var entity = new QuestionDefinition
        {
            Id = Guid.NewGuid(),
            ExternalCode = externalCode,
            CategoryId = input.CategoryId,
            Text = input.Text,
            Answer = input.Answer,
            NormalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Answer),
            Reward = input.Reward,
            IsEnabled = input.IsEnabled,
            IsDeleted = false,
            DeletedAtUtc = null,
            Priority = input.Priority,
            AskedTotalCount = 0,
            CorrectTotalCount = 0,
            LastAskedAtUtc = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.QuestionDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var categoryName = await GetCategoryNameAsync(entity.CategoryId, cancellationToken);
        return MapCatalogItem(entity, categoryName);
    }

    public async Task<GameQuestionCatalogItem?> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _dbContext.QuestionDefinitions.FirstOrDefaultAsync(
            x => x.Id == questionId && !x.IsDeleted,
            cancellationToken
        );
        if (entity is null)
        {
            return null;
        }

        entity.CategoryId = input.CategoryId;
        entity.Text = input.Text;
        entity.Answer = input.Answer;
        entity.NormalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Answer);
        entity.Reward = input.Reward;
        entity.IsEnabled = input.IsEnabled;
        entity.Priority = input.Priority;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        var categoryName = await GetCategoryNameAsync(entity.CategoryId, cancellationToken);
        return MapCatalogItem(entity, categoryName);
    }

    public async Task<ImportGameQuestionsResult> ImportQuestionsAsync(
        IReadOnlyList<CreateGameQuestionInput> inputs,
        CancellationToken cancellationToken = default
    )
    {
        var categoryIds = inputs.Select(input => input.CategoryId).Distinct().ToArray();
        var existingCategoryIds = await _dbContext.QuestionCategories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .Select(category => category.Id)
            .ToArrayAsync(cancellationToken);
        var missingCategoryId = categoryIds.Except(existingCategoryIds).FirstOrDefault();
        if (missingCategoryId != Guid.Empty)
        {
            return new ImportGameQuestionsResult(
                ImportGameQuestionsOutcome.CategoryNotFound,
                ErrorMessage: $"Category '{missingCategoryId}' was not found."
            );
        }

        var requestedExternalCodes = inputs
            .Where(input => !string.IsNullOrWhiteSpace(input.ExternalCode))
            .Select(input => input.ExternalCode!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requestedExternalCodes.Length > 0)
        {
            var existingExternalCode = await _dbContext.QuestionDefinitions
                .AsNoTracking()
                .Where(question => requestedExternalCodes.Contains(question.ExternalCode))
                .Select(question => question.ExternalCode)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(existingExternalCode))
            {
                return new ImportGameQuestionsResult(
                    ImportGameQuestionsOutcome.DuplicateCode,
                    ErrorMessage: $"External code '{existingExternalCode}' already exists."
                );
            }
        }

        var allKnownCodes = new HashSet<string>(requestedExternalCodes, StringComparer.Ordinal);
        var now = DateTime.UtcNow;
        var entities = new List<QuestionDefinition>(inputs.Count);

        foreach (var input in inputs)
        {
            var externalCode = input.ExternalCode;
            if (string.IsNullOrWhiteSpace(externalCode))
            {
                do
                {
                    externalCode = GenerateExternalCode();
                } while (!allKnownCodes.Add(externalCode));
            }
            else
            {
                allKnownCodes.Add(externalCode);
            }

            entities.Add(
                new QuestionDefinition
                {
                    Id = Guid.NewGuid(),
                    ExternalCode = externalCode,
                    CategoryId = input.CategoryId,
                    Text = input.Text,
                    Answer = input.Answer,
                    NormalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Answer),
                    Reward = input.Reward,
                    IsEnabled = input.IsEnabled,
                    IsDeleted = false,
                    DeletedAtUtc = null,
                    Priority = input.Priority,
                    AskedTotalCount = 0,
                    CorrectTotalCount = 0,
                    LastAskedAtUtc = null,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
        }

        _dbContext.QuestionDefinitions.AddRange(entities);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ImportGameQuestionsResult(
            ImportGameQuestionsOutcome.Imported,
            ImportedCount: entities.Count
        );
    }

    public async Task<bool> QuestionIdsExistAsync(
        IReadOnlyList<Guid> questionIds,
        CancellationToken cancellationToken = default
    )
    {
        if (questionIds.Count == 0)
        {
            return true;
        }

        var distinctIds = questionIds.Distinct().ToArray();
        var knownCount = await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .Where(x => !x.IsDeleted && distinctIds.Contains(x.Id))
            .CountAsync(cancellationToken);
        return knownCount == distinctIds.Length;
    }

    private async Task<string> GetCategoryNameAsync(
        Guid categoryId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.QuestionCategories
                .AsNoTracking()
                .Where(x => x.Id == categoryId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken)
            ?? string.Empty;
    }

    private static string GenerateExternalCode()
    {
        return $"q_{Guid.NewGuid():N}"[..10];
    }

    private static GameQuestionCatalogItem MapCatalogItem(QuestionDefinition x, string categoryName)
    {
        return new GameQuestionCatalogItem(
            x.Id,
            x.ExternalCode,
            x.CategoryId,
            categoryName,
            x.Text,
            x.Answer,
            x.Reward,
            x.Priority,
            x.IsEnabled,
            x.AskedTotalCount,
            x.CorrectTotalCount,
            x.LastAskedAtUtc
        );
    }

    public async Task<bool> SetCategoryEnabledAsync(
        Guid categoryId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        if (categoryId == Guid.Empty)
        {
            return false;
        }

        var categoryExists = await _dbContext.QuestionCategories
            .AsNoTracking()
            .AnyAsync(x => x.Id == categoryId, cancellationToken);
        if (!categoryExists)
        {
            return false;
        }

        if (!_dbContext.Database.IsRelational())
        {
            var questions = await _dbContext.QuestionDefinitions
                .Where(x => x.CategoryId == categoryId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var question in questions)
            {
                question.IsEnabled = isEnabled;
                question.UpdatedAtUtc = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var query = _dbContext.QuestionDefinitions.Where(
            x => x.CategoryId == categoryId && !x.IsDeleted
        );

        await query.ExecuteUpdateAsync(
            setters =>
                setters
                    .SetProperty(x => x.IsEnabled, isEnabled)
                    .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken
        );
        return true;
    }

    public async Task<Guid?> GetActiveGameIdAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AskedGameQuestion?> AskNextQuestionAsync(
        Guid gameId,
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var alreadyAskedQuestionIds = await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .Select(x => x.QuestionId)
            .ToArrayAsync(cancellationToken);

        var minimumAskedTotalCount = await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .Where(
                x =>
                    !x.IsDeleted
                    && x.IsEnabled
                    && !alreadyAskedQuestionIds.Contains(x.Id)
                    && _dbContext.GameQuestionSelections.Any(
                        selection => selection.GameId == gameId && selection.QuestionId == x.Id
                    )
            )
            .MinAsync(x => (int?)x.AskedTotalCount, cancellationToken);

        if (!minimumAskedTotalCount.HasValue)
        {
            return null;
        }

        var minimumPriority = await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .Where(
                x =>
                    !x.IsDeleted
                    && x.IsEnabled
                    && x.AskedTotalCount == minimumAskedTotalCount.Value
                    && !alreadyAskedQuestionIds.Contains(x.Id)
                    && _dbContext.GameQuestionSelections.Any(
                        selection => selection.GameId == gameId && selection.QuestionId == x.Id
                    )
            )
            .MinAsync(x => (int?)x.Priority, cancellationToken);

        if (!minimumPriority.HasValue)
        {
            return null;
        }

        var candidates = await _dbContext.QuestionDefinitions
            .Include(x => x.CategoryDefinition)
            .Where(
                x =>
                    !x.IsDeleted
                    && x.IsEnabled
                    && x.AskedTotalCount == minimumAskedTotalCount.Value
                    && x.Priority == minimumPriority.Value
                    && !alreadyAskedQuestionIds.Contains(x.Id)
                    && _dbContext.GameQuestionSelections.Any(
                        selection => selection.GameId == gameId && selection.QuestionId == x.Id
                    )
            )
            .ToArrayAsync(cancellationToken);

        if (candidates.Length == 0)
        {
            return null;
        }

        var selectedQuestion = candidates[Random.Shared.Next(candidates.Length)];
        var nextAskOrder =
            (await _dbContext.GameQuestionRounds
                .Where(x => x.GameId == gameId)
                .MaxAsync(x => (int?)x.AskOrder, cancellationToken)
                ?? 0) + 1;

        var now = DateTime.UtcNow;
        var round = new GameQuestionRound
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            QuestionId = selectedQuestion.Id,
            AskOrder = nextAskOrder,
            AskedAtUtc = now,
            AskedByUserId = askedByUserId,
            Status = GameQuestionRoundStatusValue.Asked
        };

        selectedQuestion.AskedTotalCount += 1;
        selectedQuestion.LastAskedAtUtc = now;
        selectedQuestion.UpdatedAtUtc = now;

        _dbContext.GameQuestionRounds.Add(round);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new AskedGameQuestion(
            round.Id,
            gameId,
            nextAskOrder,
            selectedQuestion.Id,
            selectedQuestion.ExternalCode,
            selectedQuestion.CategoryDefinition?.Name ?? string.Empty,
            selectedQuestion.Text,
            selectedQuestion.Reward,
            now
        );
    }

    public async Task<GameQuestionRoundSummary?> AnswerRoundAsync(
        Guid roundId,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        string submittedAnswer,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameQuestionRounds
            .Include(x => x.Question)
            .ThenInclude(q => q!.CategoryDefinition)
            .FirstOrDefaultAsync(x => x.Id == roundId, cancellationToken);
        if (round is null || round.Question is null)
        {
            return null;
        }

        if (round.Status != GameQuestionRoundStatusValue.Asked)
        {
            return null;
        }

        var normalizedSubmittedAnswer = NormalizeAnswer(submittedAnswer);
        var isCorrect = normalizedSubmittedAnswer == round.Question.NormalizedAnswer;
        var now = DateTime.UtcNow;

        round.SubmittedAnswer = submittedAnswer.Trim();
        round.AnsweredByUserId = answeredByUserId;
        round.AnsweredForUserId = answeredForUserId ?? answeredByUserId;
        round.AnsweredByDisplayName = NormalizeDisplayName(answeredByDisplayName);
        round.AnsweredAtUtc = now;
        round.IsCorrect = isCorrect;
        round.AwardedPoints = isCorrect ? round.Question.Reward : 0;
        round.Status = isCorrect
            ? GameQuestionRoundStatusValue.AnsweredCorrect
            : GameQuestionRoundStatusValue.AnsweredWrong;

        if (isCorrect)
        {
            round.Question.CorrectTotalCount += 1;
            round.Question.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapRoundSummary(round, round.Question);
    }

    public async Task<GameQuestionRoundSummary?> GetRoundAsync(
        Guid roundId,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(x => x.Id == roundId)
            .Select(
                x =>
                    new
                    {
                        Round = x,
                        QuestionText = x.Question != null ? x.Question.Text : string.Empty,
                        Category =
                            x.Question != null && x.Question.CategoryDefinition != null
                                ? x.Question.CategoryDefinition.Name
                                : string.Empty,
                        Reward = x.Question != null ? x.Question.Reward : 0
                    }
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (round is null)
        {
            return null;
        }

        return GameQuestionRoundSummaryFactory.Create(
            round.Round.Id,
            round.Round.GameId,
            round.Round.AskOrder,
            round.Round.QuestionId,
            round.QuestionText,
            round.Category,
            round.Reward,
            round.Round.Status,
            round.Round.AskedAtUtc,
            round.Round.AnsweredAtUtc,
            round.Round.AnsweredByDisplayName,
            round.Round.AnsweredByUserId,
            round.Round.AnsweredForUserId,
            round.Round.SubmittedAnswer,
            round.Round.IsCorrect,
            round.Round.AwardedPoints
        );
    }

    public async Task<IReadOnlyList<GameQuestionRoundSummary>> GetGameHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => x.AskOrder)
            .Select(
                x =>
                    GameQuestionRoundSummaryFactory.Create(
                        x.Id,
                        x.GameId,
                        x.AskOrder,
                        x.QuestionId,
                        x.Question != null ? x.Question.Text : string.Empty,
                        x.Question != null && x.Question.CategoryDefinition != null
                            ? x.Question.CategoryDefinition.Name
                            : string.Empty,
                        x.Question != null ? x.Question.Reward : 0,
                        x.Status,
                        x.AskedAtUtc,
                        x.AnsweredAtUtc,
                        x.AnsweredByDisplayName,
                        x.AnsweredByUserId,
                        x.AnsweredForUserId,
                        x.SubmittedAnswer,
                        x.IsCorrect,
                        x.AwardedPoints
                    )
            )
            .ToArrayAsync(cancellationToken);
    }

    private static Expression<Func<QuestionDefinition, GameQuestionCatalogItem>>
        ToCatalogItemSelector()
    {
        return x =>
            new GameQuestionCatalogItem(
                x.Id,
                x.ExternalCode,
                x.CategoryId,
                x.CategoryDefinition != null ? x.CategoryDefinition.Name : string.Empty,
                x.Text,
                x.Answer,
                x.Reward,
                x.Priority,
                x.IsEnabled,
                x.AskedTotalCount,
                x.CorrectTotalCount,
                x.LastAskedAtUtc
            );
    }

    private static GameQuestionRoundSummary MapRoundSummary(
        GameQuestionRound round,
        QuestionDefinition question
    )
    {
        return GameQuestionRoundSummaryFactory.Create(
            round.Id,
            round.GameId,
            round.AskOrder,
            round.QuestionId,
            question.Text,
            question.CategoryDefinition?.Name ?? string.Empty,
            question.Reward,
            round.Status,
            round.AskedAtUtc,
            round.AnsweredAtUtc,
            round.AnsweredByDisplayName,
            round.AnsweredByUserId,
            round.AnsweredForUserId,
            round.SubmittedAnswer,
            round.IsCorrect,
            round.AwardedPoints
        );
    }

    private static string NormalizeFilter(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static string NormalizeAnswer(string answer)
    {
        return QuestionAnswerNormalizer.Normalize(answer);
    }
}
