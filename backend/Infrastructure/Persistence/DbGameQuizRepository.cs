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
    private readonly TimeProvider _timeProvider;

    public DbGameQuizRepository(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
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
        GameQuizQuestionDelivery delivery,
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
        var answerDurationSeconds = await _dbContext.Games.AsNoTracking()
            .Where(x => x.Id == gameId && x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => (int?)x.QuizAnswerDurationSeconds)
            .FirstOrDefaultAsync(cancellationToken);
        if (!answerDurationSeconds.HasValue)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var openRound = await _dbContext.GameQuizRounds
            .FirstOrDefaultAsync(
                round => round.GameId == gameId && round.Status == GameQuizRoundStatusValue.Asked,
                cancellationToken
            );
        if (openRound is not null)
        {
            if (openRound.ClosesAtUtc > now)
            {
                return null;
            }

            openRound.Status = GameQuizRoundStatusValue.Timeout;
            openRound.ClosedAtUtc = openRound.ClosesAtUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var candidates = await _dbContext.GameEnabledQuestions
            .AsNoTracking()
            .Where(
                enabledQuestion =>
                    enabledQuestion.GameId == gameId
                    && !_dbContext.GameQuizRounds.Any(
                        round =>
                            round.GameId == gameId
                            && round.QuestionId == enabledQuestion.QuestionId
                    )
            )
            .Select(enabledQuestion => new
            {
                EnabledQuestion = enabledQuestion,
                AskedTotalCount = _dbContext.GameQuizRounds.Count(
                    round => round.QuestionId == enabledQuestion.QuestionId
                )
            })
            .ToArrayAsync(cancellationToken);

        if (candidates.Length == 0)
        {
            return null;
        }

        var minimumAskedTotalCount = candidates.Min(candidate => candidate.AskedTotalCount);
        var maximumPriority = candidates
            .Where(candidate => candidate.AskedTotalCount == minimumAskedTotalCount)
            .Max(candidate => candidate.EnabledQuestion.PrioritySnapshot);
        var prioritizedCandidates = candidates
            .Where(candidate =>
                candidate.AskedTotalCount == minimumAskedTotalCount
                && candidate.EnabledQuestion.PrioritySnapshot == maximumPriority)
            .ToArray();
        var selectedQuestion = prioritizedCandidates[Random.Shared.Next(prioritizedCandidates.Length)]
            .EnabledQuestion;
        var nextAskOrder =
            (await _dbContext.GameQuizRounds
                .Where(x => x.GameId == gameId)
                .MaxAsync(x => (int?)x.AskOrder, cancellationToken)
                ?? 0) + 1;

        var manualDelivery = delivery as ManualGameQuizQuestionDelivery;
        var twitchDelivery = delivery as TwitchGameQuizQuestionDelivery;
        var round = new GameQuizRound
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            QuestionId = selectedQuestion.QuestionId,
            AskOrder = nextAskOrder,
            AskedAtUtc = now,
            ClosesAtUtc = now.AddSeconds(answerDurationSeconds.Value),
            AskedByUserId = manualDelivery?.AskedByUserId,
            Status = GameQuizRoundStatusValue.Asked,
            QuestionRevisionSnapshot = selectedQuestion.QuestionRevisionSnapshot,
            QuestionCodeSnapshot = selectedQuestion.QuestionCodeSnapshot,
            CategoryNameSnapshot = selectedQuestion.CategoryNameSnapshot,
            QuestionTextSnapshot = selectedQuestion.QuestionTextSnapshot,
            AcceptedAnswersSnapshot = selectedQuestion.AcceptedAnswersSnapshot.ToArray(),
            NormalizedAnswersSnapshot = selectedQuestion.NormalizedAnswersSnapshot.ToArray(),
            RewardSnapshot = selectedQuestion.RewardSnapshot,
            DeliveryKind = twitchDelivery is null
                ? GameQuizDeliveryKindValue.Manual
                : GameQuizDeliveryKindValue.Twitch,
            SourceChannelId = twitchDelivery?.SourceChannelId.Trim(),
            SourceMessageId = NormalizeOptionalValue(twitchDelivery?.SourceMessageId)
        };

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
            selectedQuestion.QuestionId,
            selectedQuestion.QuestionCodeSnapshot,
            selectedQuestion.CategoryNameSnapshot,
            selectedQuestion.QuestionTextSnapshot,
            selectedQuestion.RewardSnapshot,
            now,
            round.ClosesAtUtc
        );
    }

    public async Task<SubmitQuizAnswerRepositoryResult> AnswerQuizRoundAsync(
        Guid roundId,
        SubmitGameQuizAnswerInput input,
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
            return new SubmitQuizAnswerRepositoryResult(
                SubmitQuizAnswerRepositoryOutcome.RoundNotFound
            );
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
            .Include(x => x.CorrectAnswer)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == roundId
                    && x.Game != null
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted,
                cancellationToken
            );
        if (round is null)
        {
            return new SubmitQuizAnswerRepositoryResult(
                SubmitQuizAnswerRepositoryOutcome.RoundNotFound
            );
        }

        if (round.Status != GameQuizRoundStatusValue.Asked)
        {
            return new SubmitQuizAnswerRepositoryResult(
                SubmitQuizAnswerRepositoryOutcome.RoundNotPending,
                MapRoundSummary(round)
            );
        }

        var normalizedSubmittedAnswer = NormalizeAnswer(input.SubmittedAnswer);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (now >= round.ClosesAtUtc)
        {
            round.Status = GameQuizRoundStatusValue.Timeout;
            round.ClosedAtUtc = round.ClosesAtUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new SubmitQuizAnswerRepositoryResult(
                SubmitQuizAnswerRepositoryOutcome.RoundNotPending,
                MapRoundSummary(round)
            );
        }

        var isCorrect = round.NormalizedAnswersSnapshot.Contains(
            normalizedSubmittedAnswer,
            StringComparer.Ordinal
        );
        if (!isCorrect)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new SubmitQuizAnswerRepositoryResult(
                SubmitQuizAnswerRepositoryOutcome.Incorrect,
                MapRoundSummary(round)
            );
        }

        var attribution = await ResolveAnswerAttributionAsync(
            input.Source,
            now,
            cancellationToken
        );
        if (attribution is null)
        {
            return new SubmitQuizAnswerRepositoryResult(
                SubmitQuizAnswerRepositoryOutcome.PlayerNotFound
            );
        }

        var correctAnswer = new GameQuizCorrectAnswer
        {
            Id = Guid.NewGuid(),
            GameId = round.GameId,
            QuizRoundId = round.Id,
            AwardedToUserId = attribution.AwardedToUserId,
            CapturedByUserId = attribution.CapturedByUserId,
            TwitchUserIdSnapshot = attribution.TwitchUserId,
            LoginSnapshot = attribution.Login,
            DisplayNameSnapshot = attribution.DisplayName,
            SubmittedAnswer = input.SubmittedAnswer.Trim(),
            NormalizedAnswer = normalizedSubmittedAnswer,
            SourceProvider = attribution.SourceProvider,
            SourceChannelId = attribution.SourceChannelId,
            SourceMessageId = attribution.SourceMessageId,
            AnsweredAtUtc = now
        };

        round.ClosedAtUtc = now;
        round.Status = GameQuizRoundStatusValue.AnsweredCorrect;
        round.CorrectAnswer = correctAnswer;
        _dbContext.GameQuizCorrectAnswers.Add(correctAnswer);

        if (round.RewardSnapshot > 0)
        {
            var earnedPoints = await GetEarnedQuizPointsAsync(
                round.GameId,
                attribution.AwardedToUserId,
                cancellationToken
            );
            var spentPoints = await GetSpentQuizPointsAsync(
                round.GameId,
                attribution.AwardedToUserId,
                cancellationToken
            );
            var availableBefore = Math.Max(0L, earnedPoints - spentPoints);
            var availableAfter = availableBefore + round.RewardSnapshot;
            _dbContext.GameQuizPointLedgerEntries.Add(
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = round.GameId,
                    UserId = attribution.AwardedToUserId,
                    EntryType = GameQuizPointEntryTypeValue.QuizReward,
                    PointsDelta = round.RewardSnapshot,
                    CorrectAnswerId = correctAnswer.Id,
                    AvailablePointsBefore = availableBefore,
                    AvailablePointsAfter = availableAfter,
                    OccurredAtUtc = now
                }
            );
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return new SubmitQuizAnswerRepositoryResult(
            SubmitQuizAnswerRepositoryOutcome.Correct,
            MapRoundSummary(round)
        );
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

        var existing = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x => x.ManualRequestId == input.RequestId)
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.GameId != activeGameId.Value
                || existing.UserId != input.AwardedToUserId
                || existing.CreatedByUserId != awardedByUserId
                || existing.PointsDelta != ResolvePointsDelta(input)
                || existing.Reason != input.Reason)
            {
                return new ManualQuizAwardResult(
                    ManualQuizAwardOutcome.DuplicateRequestConflict
                );
            }

            var existingDisplayNames = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == existing.UserId || x.Id == existing.CreatedByUserId)
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);
            return new ManualQuizAwardResult(
                ManualQuizAwardOutcome.Awarded,
                MapManualAdjustmentSummary(
                    existing,
                    existingDisplayNames.GetValueOrDefault(existing.UserId)
                        ?? existing.UserId.ToString(),
                    existingDisplayNames.GetValueOrDefault(existing.CreatedByUserId!.Value)
                        ?? existing.CreatedByUserId.Value.ToString()
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var award = new GameQuizPointLedgerEntry
        {
            Id = Guid.NewGuid(),
            GameId = activeGameId.Value,
            UserId = input.AwardedToUserId,
            EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
            PointsDelta = pointsDelta,
            ManualRequestId = input.RequestId,
            CreatedByUserId = awardedByUserId,
            Reason = input.Reason,
            AvailablePointsBefore = availableBefore,
            AvailablePointsAfter = availableAfter,
            OccurredAtUtc = now
        };
        _dbContext.GameQuizPointLedgerEntries.Add(award);
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
        var earnedByPlayer = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == activeGameId.Value
                && playerIds.Contains(x.UserId)
                && (x.EntryType == GameQuizPointEntryTypeValue.QuizReward
                    || x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment))
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Points = x.Sum(item => (long)item.PointsDelta) })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);
        var spentByPlayer = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == activeGameId.Value
                && playerIds.Contains(x.UserId)
                && (x.EntryType == GameQuizPointEntryTypeValue.ModifierPurchase
                    || x.EntryType == GameQuizPointEntryTypeValue.ModifierRefund))
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                UserId = x.Key,
                Points = -x.Sum(item => (long)item.PointsDelta)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Points, cancellationToken);

        return players.Select(player =>
            {
                var earned = earnedByPlayer.GetValueOrDefault(player.Id);
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
        return await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.UserId == userId
                && (x.EntryType == GameQuizPointEntryTypeValue.QuizReward
                    || x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment))
            .SumAsync(x => (long)x.PointsDelta, cancellationToken);
    }

    private async Task<long> GetSpentQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    ) => -await _dbContext.GameQuizPointLedgerEntries
        .AsNoTracking()
        .Where(x =>
            x.GameId == gameId
            && x.UserId == userId
            && (x.EntryType == GameQuizPointEntryTypeValue.ModifierPurchase
                || x.EntryType == GameQuizPointEntryTypeValue.ModifierRefund))
        .SumAsync(x => (long)x.PointsDelta, cancellationToken);

    private static int ResolvePointsDelta(ManualQuizAwardInput input) =>
        input.OperationType == GameQuizManualAdjustmentOperationValue.Deduct
            ? -input.Points
            : input.Points;

    private static ManualQuizAwardSummary MapManualAdjustmentSummary(
        GameQuizPointLedgerEntry award,
        string awardedToDisplayName,
        string awardedByDisplayName
    ) => new(
        award.Id,
        award.GameId,
        award.UserId,
        awardedToDisplayName,
        award.CreatedByUserId!.Value,
        awardedByDisplayName,
        award.PointsDelta < 0
            ? GameQuizManualAdjustmentOperationValue.Deduct
            : GameQuizManualAdjustmentOperationValue.Award,
        SaturatingInt32.From(award.PointsDelta),
        award.Reason ?? string.Empty,
        SaturatingInt32.From(award.AvailablePointsBefore),
        SaturatingInt32.From(award.AvailablePointsAfter),
        award.ManualRequestId ?? Guid.Empty,
        award.OccurredAtUtc
    );

    public async Task<GameQuizRoundSummary?> GetQuizRoundAsync(
        Guid roundId,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Include(x => x.CorrectAnswer)
            .Where(
                x =>
                    x.Id == roundId
                    && x.Game != null
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (round is null)
        {
            return null;
        }

        return MapRoundSummary(round);
    }

    private static GameQuizRoundSummary MapRoundSummary(GameQuizRound round)
    {
        var answer = round.CorrectAnswer;
        return GameQuizRoundSummaryFactory.Create(
            round.Id,
            round.GameId,
            round.AskOrder,
            round.QuestionId,
            round.QuestionTextSnapshot,
            round.CategoryNameSnapshot,
            round.RewardSnapshot,
            round.Status,
            round.AskedAtUtc,
            round.ClosesAtUtc,
            answer?.AnsweredAtUtc,
            answer?.DisplayNameSnapshot,
            answer?.CapturedByUserId,
            answer?.AwardedToUserId,
            answer?.SubmittedAnswer,
            answer is null ? null : true,
            answer is null ? null : round.RewardSnapshot
        );
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private async Task<ResolvedQuizAnswerAttribution?> ResolveAnswerAttributionAsync(
        GameQuizAnswerSource source,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        if (source is ManualGameQuizAnswerSource manual)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(candidate => candidate.Id == manual.AwardedToUserId && candidate.IsActive)
                .Select(candidate => new
                {
                    candidate.Id,
                    candidate.TwitchUserId,
                    candidate.Login,
                    candidate.DisplayName
                })
                .FirstOrDefaultAsync(cancellationToken);
            return user is null
                ? null
                : new ResolvedQuizAnswerAttribution(
                    user.Id,
                    manual.CapturedByUserId,
                    user.TwitchUserId,
                    user.Login,
                    NormalizeDisplayName(manual.ReportedDisplayName) ?? user.DisplayName,
                    GameQuizAnswerSourceValue.Manual,
                    null,
                    null
                );
        }

        if (source is not TwitchGameQuizAnswerSource twitch)
        {
            return null;
        }

        var twitchUserId = twitch.TwitchUserId.Trim();
        var userEntity = await _dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.TwitchUserId == twitchUserId,
            cancellationToken
        );
        if (userEntity is null)
        {
            userEntity = new User
            {
                Id = Guid.NewGuid(),
                TwitchUserId = twitchUserId,
                Login = twitch.Login.Trim(),
                DisplayName = twitch.DisplayName.Trim(),
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _dbContext.Users.Add(userEntity);
        }
        else
        {
            if (!userEntity.IsActive)
            {
                return null;
            }

            userEntity.Login = twitch.Login.Trim();
            userEntity.DisplayName = twitch.DisplayName.Trim();
            userEntity.UpdatedAtUtc = now;
        }

        return new ResolvedQuizAnswerAttribution(
            userEntity.Id,
            null,
            userEntity.TwitchUserId,
            userEntity.Login,
            userEntity.DisplayName,
            GameQuizAnswerSourceValue.Twitch,
            twitch.SourceChannelId.Trim(),
            twitch.SourceMessageId.Trim()
        );
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string NormalizeAnswer(string answer)
    {
        return QuestionAnswerNormalizer.Normalize(answer);
    }

    private sealed record ResolvedQuizAnswerAttribution(
        Guid AwardedToUserId,
        Guid? CapturedByUserId,
        string TwitchUserId,
        string Login,
        string DisplayName,
        string SourceProvider,
        string? SourceChannelId,
        string? SourceMessageId
    );
}
