using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
    private async Task RefundRoundActivationsAsync(
        Guid roundId,
        Guid cancelledByUserId,
        string reason,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var activations = await _dbContext.GameModifierActivations
            .Where(
                x =>
                    x.RoundId == roundId
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
            .ToArrayAsync(cancellationToken);

        var availableByUserId = new Dictionary<Guid, long>();
        foreach (var userId in activations
                     .Where(activation => activation.ActivationCostSnapshot > 0)
                     .Select(activation => activation.ActivatedByUserId)
                     .Distinct())
        {
            availableByUserId[userId] = await _dbContext.GameQuizPointLedgerEntries
                .AsNoTracking()
                .Where(entry =>
                    entry.GameId == activations[0].GameId
                    && entry.UserId == userId)
                .SumAsync(entry => (long)entry.PointsDelta, cancellationToken);
        }

        foreach (var activation in activations)
        {
            activation.Status = GameModifierActivationStatusValue.Cancelled;
            activation.CancelledByUserId = cancelledByUserId;
            activation.CancelledAtUtc = now;
            activation.CancellationReason = reason;
            activation.RefundAmount = activation.ActivationCostSnapshot;
            activation.ArchivedAtUtc = now;
            if (activation.RefundAmount > 0)
            {
                var availableBefore = availableByUserId[activation.ActivatedByUserId];
                var availableAfter = availableBefore + activation.RefundAmount;
                _dbContext.GameQuizPointLedgerEntries.Add(
                    new GameQuizPointLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        GameId = activation.GameId,
                        UserId = activation.ActivatedByUserId,
                        EntryType = GameQuizPointEntryTypeValue.ModifierRefund,
                        PointsDelta = activation.RefundAmount,
                        ModifierActivationId = activation.Id,
                        CreatedByUserId = cancelledByUserId,
                        Reason = reason,
                        AvailablePointsBefore = availableBefore,
                        AvailablePointsAfter = availableAfter,
                        OccurredAtUtc = now
                    }
                );
                availableByUserId[activation.ActivatedByUserId] = availableAfter;
            }
        }
    }

    private async Task IncrementBoardVersionAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.GameBoards
                .Where(x => x.GameId == gameId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.Version, x => x.Version + 1),
                    cancellationToken
                );
            return;
        }

        var board = await _dbContext.GameBoards.SingleAsync(
            x => x.GameId == gameId,
            cancellationToken
        );
        board.Version += 1;
    }
}
