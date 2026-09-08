using System.Linq.Expressions;
using backend.Application.Contracts;
using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameQuestionRepository
{
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
