using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameQuestionRepository
{
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
}
