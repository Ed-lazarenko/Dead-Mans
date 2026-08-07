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
                entityToNormalize.UpdatedAtUtc = DateTime.UtcNow;
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

        var now = DateTime.UtcNow;
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
        category.UpdatedAtUtc = DateTime.UtcNow;
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
        var now = DateTime.UtcNow;
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

            entities.Add(
                new QuestionDefinition
                {
                    Id = Guid.NewGuid(),
                    ExternalCode = externalCode,
                    CategoryId = input.Question.CategoryId,
                    Text = input.Question.Text,
                    Answer = input.Question.Answer,
                    NormalizedAnswer = QuestionAnswerNormalizer.Normalize(input.Question.Answer),
                    Reward = input.Question.Reward,
                    IsEnabled = input.Question.IsEnabled,
                    IsDeleted = false,
                    DeletedAtUtc = null,
                    Priority = input.Question.Priority,
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
