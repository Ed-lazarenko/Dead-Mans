using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameHistoryRepository : IGameHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DbGameHistoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserGameHistoryItem>> GetUserGameHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var modifierGameIds = await _dbContext.GameActiveModifiers
            .AsNoTracking()
            .Where(x => x.ActivatedByUserId == userId)
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var answeredGameIds = await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.AnsweredAtUtc.HasValue
                    && (
                        x.AnsweredForUserId == userId
                        || (x.AnsweredForUserId == null && x.AnsweredByUserId == userId)
                    )
            )
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var gameIds = modifierGameIds
            .Concat(answeredGameIds)
            .Distinct()
            .ToArray();
        if (gameIds.Length == 0)
        {
            return Array.Empty<UserGameHistoryItem>();
        }

        var games = await _dbContext.Games
            .AsNoTracking()
            .Where(x => gameIds.Contains(x.Id))
            .Select(
                x =>
                    new GameRow(
                        x.Id,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var modifierActivations = await _dbContext.GameActiveModifiers
            .AsNoTracking()
            .Where(x => x.ActivatedByUserId == userId && gameIds.Contains(x.GameId))
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameModifierActivationHistoryItem(x.ModifierId, x.ActivatedAtUtc)
                    }
            )
            .ToArrayAsync(cancellationToken);

        var questionAnswers = await _dbContext.GameQuestionRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.AnsweredAtUtc.HasValue
                    && gameIds.Contains(x.GameId)
                    && (
                        x.AnsweredForUserId == userId
                        || (x.AnsweredForUserId == null && x.AnsweredByUserId == userId)
                    )
            )
            .OrderBy(x => x.AnsweredAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameQuestionAnswerHistoryItem(
                            x.Id,
                            x.QuestionId,
                            x.Question != null ? x.Question.Text : string.Empty,
                            x.Question != null && x.Question.CategoryDefinition != null
                                ? x.Question.CategoryDefinition.Name
                                : string.Empty,
                            x.AnsweredAtUtc!.Value,
                            x.IsCorrect ?? false,
                            x.AwardedPoints ?? 0,
                            x.SubmittedAnswer,
                            x.AnsweredByUserId
                        )
                    }
            )
            .ToArrayAsync(cancellationToken);

        var modifiersByGameId = modifierActivations
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<UserGameModifierActivationHistoryItem>)x.Select(item => item.Item).ToArray()
            );
        var answersByGameId = questionAnswers
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<UserGameQuestionAnswerHistoryItem>)x.Select(item => item.Item).ToArray()
            );

        return games
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(
                x =>
                    new UserGameHistoryItem(
                        x.GameId,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc,
                        modifiersByGameId.GetValueOrDefault(
                            x.GameId,
                            Array.Empty<UserGameModifierActivationHistoryItem>()
                        ),
                        answersByGameId.GetValueOrDefault(
                            x.GameId,
                            Array.Empty<UserGameQuestionAnswerHistoryItem>()
                        )
                    )
            )
            .ToArray();
    }

    private sealed record GameRow(
        Guid GameId,
        string Title,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? StartedAtUtc,
        DateTime? FinishedAtUtc
    );
}
