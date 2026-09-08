using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameQuestionRepository
{
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
}
