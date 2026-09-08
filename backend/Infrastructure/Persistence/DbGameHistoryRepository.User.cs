using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameRounds;
using backend.Application.Features.Scoring;
using backend.Data;
using backend.Infrastructure.Configuration;
using backend.Domain.Persistence;
using backend.Domain.GameModifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameHistoryRepository : IGameHistoryRepository
{
    public async Task<IReadOnlyList<UserGameHistoryItem>> GetUserGameHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var modifierGameIds = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.ActivatedByUserId == userId
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var answeredGameIds = await _dbContext.GameQuizCorrectAnswers
            .AsNoTracking()
            .Where(x => x.AwardedToUserId == userId)
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var manualAwardGameIds = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment)
            .Select(x => x.GameId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var gameIds = modifierGameIds
            .Concat(answeredGameIds)
            .Concat(manualAwardGameIds)
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

        var modifierActivations = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.ActivatedByUserId == userId
                    && gameIds.Contains(x.GameId)
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
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

        var questionAnswers = await _dbContext.GameQuizCorrectAnswers
            .AsNoTracking()
            .Where(x => gameIds.Contains(x.GameId) && x.AwardedToUserId == userId)
            .OrderBy(x => x.AnsweredAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameQuestionAnswerHistoryItem(
                            x.QuizRoundId,
                            x.QuizRound.QuestionId,
                            x.QuizRound.QuestionTextSnapshot,
                            x.QuizRound.CategoryNameSnapshot,
                            x.AnsweredAtUtc,
                            true,
                            x.PointEntries
                                .Where(entry =>
                                    entry.EntryType == GameQuizPointEntryTypeValue.QuizReward)
                                .Sum(entry => entry.PointsDelta),
                            x.SubmittedAnswer,
                            x.CapturedByUserId
                        )
                    }
            )
            .ToArrayAsync(cancellationToken);

        var manualAwards = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && gameIds.Contains(x.GameId)
                && x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment)
            .OrderBy(x => x.SequenceNumber)
            .Select(
                x =>
                    new
                    {
                        x.GameId,
                        Item = new UserGameQuizManualAwardHistoryItem(
                            x.Id,
                            x.OccurredAtUtc,
                            x.PointsDelta,
                            x.CreatedByUserId!.Value,
                            x.CreatedByUser != null
                                ? x.CreatedByUser.DisplayName
                                : x.CreatedByUserId.Value.ToString(),
                            x.PointsDelta < 0
                                ? GameQuizManualAdjustmentOperationValue.Deduct
                                : GameQuizManualAdjustmentOperationValue.Award,
                            x.Reason
                        )
                    }
            )
            .ToArrayAsync(cancellationToken);

        var modifiersByGameId = modifierActivations
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<UserGameModifierActivationHistoryItem>)
                        x.Select(item => item.Item).ToArray()
            );
        var answersByGameId = questionAnswers
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<UserGameQuestionAnswerHistoryItem>)
                        x.Select(item => item.Item).ToArray()
            );
        var manualAwardsByGameId = manualAwards
            .GroupBy(x => x.GameId)
            .ToDictionary(
                x => x.Key,
                x =>
                    (IReadOnlyList<UserGameQuizManualAwardHistoryItem>)
                        x.Select(item => item.Item).ToArray()
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
                        ),
                        manualAwardsByGameId.GetValueOrDefault(
                            x.GameId,
                            Array.Empty<UserGameQuizManualAwardHistoryItem>()
                        )
                    )
            )
            .ToArray();
    }

}
