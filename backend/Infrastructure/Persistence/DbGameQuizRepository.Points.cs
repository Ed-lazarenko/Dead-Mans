using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Application.Features.Scoring;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameQuizRepository
{
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
}
