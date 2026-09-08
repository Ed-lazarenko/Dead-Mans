using System.Linq.Expressions;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameQuestionRepository : IGameQuestionRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DbGameQuestionRepository(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
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
                    || x.AcceptedAnswers.Any(answer =>
                        EF.Functions.ILike(answer.AnswerText, $"%{searchLower}%"))
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
        await EnsureFallbackCategoryAsync(cancellationToken);

        return await _dbContext.QuestionCategories
            .AsNoTracking()
            .OrderBy(x => x.Name == QuestionCatalogDefaults.UncategorizedCategoryName ? 0 : 1)
            .ThenBy(x => x.Name)
            .Select(
                x => new GameQuestionCategoryItem(
                    x.Id,
                    x.Name,
                    x.Questions.Count(question => !question.IsDeleted),
                    IsProtectedCategory(x.Id, x.Name)
                )
            )
            .ToArrayAsync(cancellationToken);
    }

    public async Task<GameQuestionCategoryItem> EnsureFallbackCategoryAsync(
        CancellationToken cancellationToken = default
    )
    {
        var existingById = await _dbContext.QuestionCategories
            .AsNoTracking()
            .Where(x => x.Id == QuestionCatalogDefaults.UncategorizedCategoryId)
            .Select(
                x => new GameQuestionCategoryItem(
                    x.Id,
                    x.Name,
                    x.Questions.Count(question => !question.IsDeleted),
                    IsProtectedCategory(x.Id, x.Name)
                )
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (existingById is not null)
        {
            if (!string.Equals(
                    existingById.Name,
                    QuestionCatalogDefaults.UncategorizedCategoryName,
                    StringComparison.Ordinal
                ))
            {
                var entityToNormalize = await _dbContext.QuestionCategories.FirstAsync(
                    x => x.Id == QuestionCatalogDefaults.UncategorizedCategoryId,
                    cancellationToken
                );
                entityToNormalize.Name = QuestionCatalogDefaults.UncategorizedCategoryName;
                entityToNormalize.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return existingById with
                {
                    Name = QuestionCatalogDefaults.UncategorizedCategoryName,
                    IsProtected = true
                };
            }

            return existingById;
        }

        var existingByName = await _dbContext.QuestionCategories
            .AsNoTracking()
            .Where(x => x.Name == QuestionCatalogDefaults.UncategorizedCategoryName)
            .Select(
                x => new GameQuestionCategoryItem(
                    x.Id,
                    x.Name,
                    x.Questions.Count(question => !question.IsDeleted),
                    IsProtectedCategory(x.Id, x.Name)
                )
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (existingByName is not null)
        {
            return existingByName;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = new QuestionCategory
        {
            Id = QuestionCatalogDefaults.UncategorizedCategoryId,
            Name = QuestionCatalogDefaults.UncategorizedCategoryName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.QuestionCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameQuestionCategoryItem(entity.Id, entity.Name, 0, true);
    }

    public async Task<GameQuestionCategoryItem?> GetCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        if (categoryId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.QuestionCategories
            .AsNoTracking()
            .Where(x => x.Id == categoryId)
            .Select(
                x => new GameQuestionCategoryItem(
                    x.Id,
                    x.Name,
                    x.Questions.Count(question => !question.IsDeleted),
                    IsProtectedCategory(x.Id, x.Name)
                )
            )
            .FirstOrDefaultAsync(cancellationToken);
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
                x => new GameQuestionCategoryItem(
                    x.Id,
                    x.Name,
                    x.Questions.Count(question => !question.IsDeleted),
                    IsProtectedCategory(x.Id, x.Name)
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
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = new QuestionCategory
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.QuestionCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameQuestionCategoryItem(entity.Id, entity.Name, 0, false);
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

        if (IsProtectedCategory(category.Id, category.Name))
        {
            return DeleteGameQuestionCategoryOutcome.Protected;
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
        category.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameQuestionCategoryItem(
            category.Id,
            category.Name,
            category.Questions.Count(question => !question.IsDeleted),
            IsProtectedCategory(category.Id, category.Name)
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
        question.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        question.IsDeleted = true;
        question.DeletedAtUtc = now;
        question.IsEnabled = false;
        question.UpdatedAtUtc = now;
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var normalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Answer);
        var entity = new QuestionDefinition
        {
            Id = Guid.NewGuid(),
            ExternalCode = externalCode,
            CategoryId = input.CategoryId,
            Text = input.Text,
            Reward = input.Reward,
            Revision = 1,
            IsEnabled = input.IsEnabled,
            IsDeleted = false,
            DeletedAtUtc = null,
            Priority = input.Priority,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        entity.AcceptedAnswers.Add(
            new QuestionAcceptedAnswer
            {
                Id = Guid.NewGuid(),
                QuestionId = entity.Id,
                AnswerText = input.Answer,
                NormalizedAnswer = normalizedAnswer,
                IsPrimary = true,
                SortOrder = 0,
                CreatedAtUtc = now
            }
        );

        _dbContext.QuestionDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadCatalogItemAsync(entity.Id, cancellationToken);
    }

    public async Task<GameQuestionCatalogItem?> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _dbContext.QuestionDefinitions
            .Include(question => question.AcceptedAnswers)
            .FirstOrDefaultAsync(
            x => x.Id == questionId && !x.IsDeleted,
            cancellationToken
        );
        if (entity is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var normalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Answer);
        entity.CategoryId = input.CategoryId;
        entity.Text = input.Text;
        entity.Reward = input.Reward;
        entity.IsEnabled = input.IsEnabled;
        entity.Priority = input.Priority;
        entity.Revision += 1;
        entity.UpdatedAtUtc = now;

        var primaryAnswer = entity.AcceptedAnswers.SingleOrDefault(answer => answer.IsPrimary);
        if (primaryAnswer is null)
        {
            entity.AcceptedAnswers.Add(
                new QuestionAcceptedAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = entity.Id,
                    AnswerText = input.Answer,
                    NormalizedAnswer = normalizedAnswer,
                    IsPrimary = true,
                    SortOrder = 0,
                    CreatedAtUtc = now
                }
            );
        }
        else
        {
            primaryAnswer.AnswerText = input.Answer;
            primaryAnswer.NormalizedAnswer = normalizedAnswer;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadCatalogItemAsync(entity.Id, cancellationToken);
    }

    public async Task<ImportGameQuestionsResult> ImportQuestionsAsync(
        IReadOnlyList<ImportGameQuestionCandidate> inputs,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureFallbackCategoryAsync(cancellationToken);

        var categoryIds = inputs.Select(input => input.Question.CategoryId).Distinct().ToArray();
        var existingCategoryIds = await _dbContext.QuestionCategories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .Select(category => category.Id)
            .ToArrayAsync(cancellationToken);
        var validInputs = inputs
            .Where(input => existingCategoryIds.Contains(input.Question.CategoryId))
            .ToArray();
        var skipped = inputs
            .Where(input => !existingCategoryIds.Contains(input.Question.CategoryId))
            .Select(
                input =>
                    new ImportGameQuestionSkippedItem(
                        input.RowNumber,
                        input.QuestionText,
                        AppMessages.ErrorCodes.GameQuestionImportCategoryUnresolved,
                        "The selected category could not be resolved.",
                        input.SourceQuestion
                    )
            )
            .ToList();

        var requestedExternalCodes = validInputs
            .Where(input => !string.IsNullOrWhiteSpace(input.Question.ExternalCode))
            .Select(input => input.Question.ExternalCode!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var existingExternalCodes = requestedExternalCodes.Length == 0
            ? Array.Empty<string>()
            : await _dbContext.QuestionDefinitions
                .AsNoTracking()
                .Where(question => requestedExternalCodes.Contains(question.ExternalCode))
                .Select(question => question.ExternalCode)
                .ToArrayAsync(cancellationToken);
        var existingExternalCodeSet = existingExternalCodes.ToHashSet(StringComparer.Ordinal);

        var allKnownCodes = new HashSet<string>(requestedExternalCodes, StringComparer.Ordinal);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entities = new List<QuestionDefinition>(validInputs.Length);

        foreach (var input in validInputs)
        {
            if (!string.IsNullOrWhiteSpace(input.Question.ExternalCode)
                && existingExternalCodeSet.Contains(input.Question.ExternalCode))
            {
                skipped.Add(
                    new ImportGameQuestionSkippedItem(
                        input.RowNumber,
                        input.QuestionText,
                        AppMessages.ErrorCodes.GameQuestionImportDuplicateCodeExisting,
                        $"External code '{input.Question.ExternalCode}' already exists.",
                        input.SourceQuestion
                    )
                );
                continue;
            }

            var externalCode = input.Question.ExternalCode;
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

            var questionId = Guid.NewGuid();
            var normalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Question.Answer);
            entities.Add(
                new QuestionDefinition
                {
                    Id = questionId,
                    ExternalCode = externalCode,
                    CategoryId = input.Question.CategoryId,
                    Text = input.Question.Text,
                    Reward = input.Question.Reward,
                    Revision = 1,
                    IsEnabled = input.Question.IsEnabled,
                    IsDeleted = false,
                    DeletedAtUtc = null,
                    Priority = input.Question.Priority,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    AcceptedAnswers =
                    [
                        new QuestionAcceptedAnswer
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = questionId,
                            AnswerText = input.Question.Answer,
                            NormalizedAnswer = normalizedAnswer,
                            IsPrimary = true,
                            SortOrder = 0,
                            CreatedAtUtc = now
                        }
                    ]
                }
            );
        }

        _dbContext.QuestionDefinitions.AddRange(entities);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ImportGameQuestionsResult(entities.Count, skipped.OrderBy(item => item.RowNumber).ToArray());
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

    private async Task<GameQuestionCatalogItem?> LoadCatalogItemAsync(
        Guid questionId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .Where(x => x.Id == questionId && !x.IsDeleted)
            .Select(ToCatalogItemSelector())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GenerateExternalCode()
    {
        return $"q_{Guid.NewGuid():N}"[..10];
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!_dbContext.Database.IsRelational())
        {
            var questions = await _dbContext.QuestionDefinitions
                .Where(x => x.CategoryId == categoryId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

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
                    .SetProperty(x => x.UpdatedAtUtc, now),
            cancellationToken
        );
        return true;
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
                x.AcceptedAnswers
                    .Where(answer => answer.IsPrimary)
                    .Select(answer => answer.AnswerText)
                    .FirstOrDefault() ?? string.Empty,
                x.Reward,
                x.Priority,
                x.IsEnabled,
                x.AskedInQuizRounds.Count,
                x.AskedInQuizRounds.Count(round => round.CorrectAnswer != null),
                x.AskedInQuizRounds
                    .OrderByDescending(round => round.AskedAtUtc)
                    .Select(round => (DateTime?)round.AskedAtUtc)
                    .FirstOrDefault()
            );
    }

    private static string NormalizeFilter(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static bool IsProtectedCategory(Guid categoryId, string categoryName)
    {
        return categoryId == QuestionCatalogDefaults.UncategorizedCategoryId
            || string.Equals(
                categoryName,
                QuestionCatalogDefaults.UncategorizedCategoryName,
                StringComparison.Ordinal
            );
    }

}
