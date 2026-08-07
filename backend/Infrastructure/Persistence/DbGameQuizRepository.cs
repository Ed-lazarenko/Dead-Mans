using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameQuizRepository : IGameQuizRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DbGameQuizRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

    public async Task<AskedQuizQuestion?> AskNextQuizQuestionAsync(
        Guid gameId,
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var alreadyAskedQuestionIds = await _dbContext.GameQuizRounds
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
                    && _dbContext.GameEnabledQuestions.Any(
                        enabledQuestion =>
                            enabledQuestion.GameId == gameId && enabledQuestion.QuestionId == x.Id
                    )
            )
            .MinAsync(x => (int?)x.AskedTotalCount, cancellationToken);

        if (!minimumAskedTotalCount.HasValue)
        {
            return null;
        }

        var maximumPriority = await _dbContext.QuestionDefinitions
            .AsNoTracking()
            .Where(
                x =>
                    !x.IsDeleted
                    && x.IsEnabled
                    && x.AskedTotalCount == minimumAskedTotalCount.Value
                    && !alreadyAskedQuestionIds.Contains(x.Id)
                    && _dbContext.GameEnabledQuestions.Any(
                        enabledQuestion =>
                            enabledQuestion.GameId == gameId && enabledQuestion.QuestionId == x.Id
                    )
            )
            .MaxAsync(x => (int?)x.Priority, cancellationToken);

        if (!maximumPriority.HasValue)
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
                    && x.Priority == maximumPriority.Value
                    && !alreadyAskedQuestionIds.Contains(x.Id)
                    && _dbContext.GameEnabledQuestions.Any(
                        enabledQuestion =>
                            enabledQuestion.GameId == gameId && enabledQuestion.QuestionId == x.Id
                    )
            )
            .ToArrayAsync(cancellationToken);

        if (candidates.Length == 0)
        {
            return null;
        }

        var selectedQuestion = candidates[Random.Shared.Next(candidates.Length)];
        var nextAskOrder =
            (await _dbContext.GameQuizRounds
                .Where(x => x.GameId == gameId)
                .MaxAsync(x => (int?)x.AskOrder, cancellationToken)
                ?? 0) + 1;

        var now = DateTime.UtcNow;
        var round = new GameQuizRound
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            QuestionId = selectedQuestion.Id,
            AskOrder = nextAskOrder,
            AskedAtUtc = now,
            AskedByUserId = askedByUserId,
            Status = GameQuizRoundStatusValue.Asked
        };

        selectedQuestion.AskedTotalCount += 1;
        selectedQuestion.LastAskedAtUtc = now;
        selectedQuestion.UpdatedAtUtc = now;

        _dbContext.GameQuizRounds.Add(round);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new AskedQuizQuestion(
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

    public async Task<GameQuizRoundSummary?> AnswerQuizRoundAsync(
        Guid roundId,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        string submittedAnswer,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameQuizRounds
            .Include(x => x.Question)
            .ThenInclude(q => q!.CategoryDefinition)
            .FirstOrDefaultAsync(x => x.Id == roundId, cancellationToken);
        if (round is null || round.Question is null)
        {
            return null;
        }

        if (round.Status != GameQuizRoundStatusValue.Asked)
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
            ? GameQuizRoundStatusValue.AnsweredCorrect
            : GameQuizRoundStatusValue.AnsweredWrong;

        if (isCorrect)
        {
            round.Question.CorrectTotalCount += 1;
            round.Question.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapRoundSummary(round, round.Question);
    }

    public async Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(
        ManualQuizAwardInput input,
        Guid awardedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.NoActiveGame);
        }

        var player = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == input.AwardedToUserId && user.IsActive)
            .Select(
                user =>
                    new
                    {
                        UserId = user.Id,
                        user.DisplayName
                    }
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (player is null)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.PlayerNotFound);
        }

        var awardedByDisplayName = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == awardedByUserId)
            .Select(user => user.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var award = new GameQuizManualAward
        {
            Id = Guid.NewGuid(),
            GameId = activeGameId.Value,
            AwardedToUserId = input.AwardedToUserId,
            AwardedByUserId = awardedByUserId,
            Points = input.Points,
            AwardedAtUtc = now
        };

        _dbContext.GameQuizManualAwards.Add(award);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManualQuizAwardResult(
            ManualQuizAwardOutcome.Awarded,
            new ManualQuizAwardSummary(
                award.Id,
                award.GameId,
                award.AwardedToUserId,
                string.IsNullOrWhiteSpace(player.DisplayName)
                    ? player.UserId.ToString()
                    : player.DisplayName,
                award.AwardedByUserId,
                string.IsNullOrWhiteSpace(awardedByDisplayName)
                    ? awardedByUserId.ToString()
                    : awardedByDisplayName,
                award.Points,
                award.AwardedAtUtc
            )
        );
    }

    public async Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.Users
            .ActiveUsersByDisplayName()
            .Select(user => new ManualQuizAwardPlayer(user.Id, user.Login, user.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<GameQuizRoundSummary?> GetQuizRoundAsync(
        Guid roundId,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameQuizRounds
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

        return GameQuizRoundSummaryFactory.Create(
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

    private static GameQuizRoundSummary MapRoundSummary(
        GameQuizRound round,
        QuestionDefinition question
    )
    {
        return GameQuizRoundSummaryFactory.Create(
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
