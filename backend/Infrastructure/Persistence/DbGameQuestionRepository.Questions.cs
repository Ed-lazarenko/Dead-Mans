using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameQuestionRepository
{
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
}
