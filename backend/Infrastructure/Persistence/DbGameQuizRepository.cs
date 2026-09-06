using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.Scoring;
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

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {gameId} FOR UPDATE",
                cancellationToken
            );
        }
        if (!await _dbContext.Games.AsNoTracking().AnyAsync(
                x => x.Id == gameId && x.Status == GameStatusValue.Active && !x.IsDeleted,
                cancellationToken
            ))
        {
            return null;
        }

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
        var gameId = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => x.Id == roundId)
            .Select(x => (Guid?)x.GameId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!gameId.HasValue)
        {
            return null;
        }

        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {gameId.Value} FOR UPDATE",
                cancellationToken
            );
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM game_quiz_rounds WHERE id = {roundId} FOR UPDATE",
                cancellationToken
            );
        }

        var round = await _dbContext.GameQuizRounds
            .Include(x => x.Question)
            .ThenInclude(q => q!.CategoryDefinition)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == roundId
                    && x.Game != null
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted,
                cancellationToken
            );
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
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return MapRoundSummary(round, round.Question);
    }

    public async Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(
        ManualQuizAwardInput input,
        Guid awardedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var activeGameId = await GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.NoActiveGame);
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {activeGameId.Value} FOR UPDATE",
                cancellationToken
            );
        }

        if (!await _dbContext.Games.AsNoTracking().AnyAsync(
                x =>
                    x.Id == activeGameId.Value
                    && x.Status == GameStatusValue.Active
                    && !x.IsDeleted,
                cancellationToken
            ))
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.NoActiveGame);
        }

        var existing = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.RequestId.HasValue && x.RequestId.Value == input.RequestId)
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.GameId != activeGameId.Value
                || existing.AwardedToUserId != input.AwardedToUserId
                || existing.AwardedByUserId != awardedByUserId
                || existing.OperationType != input.OperationType
                || existing.Points != ResolvePointsDelta(input)
                || existing.Reason != input.Reason)
            {
                return new ManualQuizAwardResult(
                    ManualQuizAwardOutcome.DuplicateRequestConflict
                );
            }

            var existingDisplayNames = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == existing.AwardedToUserId || x.Id == existing.AwardedByUserId)
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);
            return new ManualQuizAwardResult(
                ManualQuizAwardOutcome.Awarded,
                MapManualAdjustmentSummary(
                    existing,
                    existingDisplayNames.GetValueOrDefault(existing.AwardedToUserId)
                        ?? existing.AwardedToUserId.ToString(),
                    existingDisplayNames.GetValueOrDefault(existing.AwardedByUserId)
                        ?? existing.AwardedByUserId.ToString()
                )
            );
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

        var earnedPoints = await GetEarnedQuizPointsAsync(
            activeGameId.Value,
            input.AwardedToUserId,
            cancellationToken
        );
        var spentPoints = await GetSpentQuizPointsAsync(
            activeGameId.Value,
            input.AwardedToUserId,
            cancellationToken
        );
        var availableBefore = earnedPoints - spentPoints;
        var pointsDelta = ResolvePointsDelta(input);
        if (pointsDelta < 0 && availableBefore < input.Points)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.InsufficientPoints);
        }
        var availableAfter = availableBefore + pointsDelta;

        var now = DateTime.UtcNow;
        var award = new GameQuizManualAward
        {
            Id = Guid.NewGuid(),
            GameId = activeGameId.Value,
            AwardedToUserId = input.AwardedToUserId,
            AwardedByUserId = awardedByUserId,
            Points = pointsDelta,
            OperationType = input.OperationType,
            Reason = input.Reason,
            RequestId = input.RequestId,
            AvailablePointsBefore = SaturatingInt32.From(availableBefore),
            AvailablePointsAfter = SaturatingInt32.From(availableAfter),
            AwardedAtUtc = now
        };

        _dbContext.GameQuizManualAwards.Add(award);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new ManualQuizAwardResult(
            ManualQuizAwardOutcome.Awarded,
            MapManualAdjustmentSummary(
                award,
                string.IsNullOrWhiteSpace(player.DisplayName)
                    ? player.UserId.ToString()
                    : player.DisplayName,
                string.IsNullOrWhiteSpace(awardedByDisplayName)
                    ? awardedByUserId.ToString()
                    : awardedByDisplayName
            ),
            StateChanged: true
        );
    }

    public async Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return [];
        }

        var players = await _dbContext.Users
            .ActiveUsersByDisplayName()
            .Select(user => new { user.Id, user.Login, user.DisplayName })
            .ToListAsync(cancellationToken);
        var playerIds = players.Select(x => x.Id).ToArray();
        var answeredByPlayer = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => x.GameId == activeGameId.Value
                && (x.AnsweredForUserId.HasValue || x.AnsweredByUserId.HasValue)
                && playerIds.Contains(x.AnsweredForUserId ?? x.AnsweredByUserId!.Value))
            .GroupBy(x => x.AnsweredForUserId ?? x.AnsweredByUserId!.Value)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => (long)(item.AwardedPoints ?? 0)) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);
        var adjustedByPlayer = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == activeGameId.Value && playerIds.Contains(x.AwardedToUserId))
            .GroupBy(x => x.AwardedToUserId)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => (long)item.Points) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);
        var spentByPlayer = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(x => x.GameId == activeGameId.Value && playerIds.Contains(x.ActivatedByUserId))
            .GroupBy(x => x.ActivatedByUserId)
            .Select(x => new
            {
                UserId = x.Key,
                Points = x.Sum(item => (long)item.ActivationCostSnapshot - item.RefundAmount)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        return players.Select(player =>
            {
                var earned = answeredByPlayer.GetValueOrDefault(player.Id)
                    + adjustedByPlayer.GetValueOrDefault(player.Id);
                var spent = spentByPlayer.GetValueOrDefault(player.Id);
                return new ManualQuizAwardPlayer(
                player.Id,
                player.Login,
                player.DisplayName,
                SaturatingInt32.From(earned),
                SaturatingInt32.From(spent),
                SaturatingInt32.From(Math.Max(0L, earned - spent))
                );
            })
            .ToArray();
    }

    private async Task<long> GetEarnedQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var answered = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => x.GameId == gameId
                && (x.AnsweredForUserId == userId
                    || (x.AnsweredForUserId == null && x.AnsweredByUserId == userId)))
            .SumAsync(x => (long)(x.AwardedPoints ?? 0), cancellationToken);
        var adjusted = await _dbContext.GameQuizManualAwards
            .AsNoTracking()
            .Where(x => x.GameId == gameId && x.AwardedToUserId == userId)
            .SumAsync(x => (long)x.Points, cancellationToken);
        return answered + adjusted;
    }

    private async Task<long> GetSpentQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    ) => await _dbContext.GameModifierActivations
        .AsNoTracking()
        .Where(x => x.GameId == gameId && x.ActivatedByUserId == userId)
        .SumAsync(x => (long)x.ActivationCostSnapshot - x.RefundAmount, cancellationToken);

    private static int ResolvePointsDelta(ManualQuizAwardInput input) =>
        input.OperationType == GameQuizManualAdjustmentOperationValue.Deduct
            ? -input.Points
            : input.Points;

    private static ManualQuizAwardSummary MapManualAdjustmentSummary(
        GameQuizManualAward award,
        string awardedToDisplayName,
        string awardedByDisplayName
    ) => new(
        award.Id,
        award.GameId,
        award.AwardedToUserId,
        awardedToDisplayName,
        award.AwardedByUserId,
        awardedByDisplayName,
        award.OperationType,
        award.Points,
        award.Reason ?? string.Empty,
        award.AvailablePointsBefore ?? 0,
        award.AvailablePointsAfter ?? 0,
        award.RequestId ?? Guid.Empty,
        award.AwardedAtUtc
    );

    public async Task<GameQuizRoundSummary?> GetQuizRoundAsync(
        Guid roundId,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(
                x =>
                    x.Id == roundId
                    && x.Game != null
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted
            )
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
