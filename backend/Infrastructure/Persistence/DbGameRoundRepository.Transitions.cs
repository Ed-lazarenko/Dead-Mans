using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
    public Task<TransitionGameRoundResult> PrepareAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return TransitionRoundAsync(
            roundId,
            input.ExpectedRoundVersion,
            GameRoundStatusValue.AwaitingModifiers,
            GameRoundStatusValue.Preparing,
            GameRoundTransitionActionValue.Prepare,
            initiatedByUserId,
            (round, now, _) =>
            {
                round.PreparedAtUtc = now;
                return Task.CompletedTask;
            },
            cancellationToken
        );
    }

    public Task<TransitionGameRoundResult> BeginGameplayAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return TransitionRoundAsync(
            roundId,
            input.ExpectedRoundVersion,
            GameRoundStatusValue.Preparing,
            GameRoundStatusValue.InProgress,
            GameRoundTransitionActionValue.BeginGameplay,
            initiatedByUserId,
            async (round, now, token) =>
            {
                round.GameplayStartedAtUtc = now;
                await AddModifierSnapshotsAsync(round.GameId, round.Id, now, token);
            },
            cancellationToken
        );
    }

    public Task<TransitionGameRoundResult> ReviewAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return TransitionRoundAsync(
            roundId,
            input.ExpectedRoundVersion,
            GameRoundStatusValue.InProgress,
            GameRoundStatusValue.ReviewingResults,
            GameRoundTransitionActionValue.Review,
            reviewedByUserId,
            (round, now, _) =>
            {
                round.ReviewedAtUtc = now;
                return Task.CompletedTask;
            },
            cancellationToken
        );
    }

    public Task<TransitionGameRoundResult> ResumeGameplayAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return TransitionRoundAsync(
            roundId,
            input.ExpectedRoundVersion,
            GameRoundStatusValue.ReviewingResults,
            GameRoundStatusValue.InProgress,
            GameRoundTransitionActionValue.ResumeGameplay,
            initiatedByUserId,
            (round, _, _) =>
            {
                round.ReviewedAtUtc = null;
                return Task.CompletedTask;
            },
            cancellationToken
        );
    }

    private async Task<TransitionGameRoundResult> TransitionRoundAsync(
        Guid roundId,
        int expectedRoundVersion,
        string expectedStatus,
        string targetStatus,
        string actionCode,
        Guid initiatedByUserId,
        Func<GameRound, DateTime, CancellationToken, Task> applyTransition,
        CancellationToken cancellationToken
    )
    {
        var gameId = await ResolveRoundGameIdAsync(roundId, cancellationToken);
        if (!gameId.HasValue)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.NotFound, null);
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
        }

        if (!await _dbContext.Games.AsNoTracking().AnyAsync(
                x => x.Id == gameId.Value
                    && x.Status == GameStatusValue.Active
                    && !x.IsDeleted,
                cancellationToken
            ))
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.InvalidState, null);
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM game_rounds WHERE id = {roundId} FOR UPDATE",
                cancellationToken
            );
        }

        var round = await _dbContext.GameRounds.FirstOrDefaultAsync(
            x => x.Id == roundId,
            cancellationToken
        );
        if (round is null)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.NotFound, null);
        }

        if (round.Status == targetStatus)
        {
            return new TransitionGameRoundResult(
                TransitionGameRoundOutcome.Transitioned,
                await LoadRoundDetailsAsync(roundId, cancellationToken)
            );
        }

        if (round.Version != expectedRoundVersion)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.StaleVersion, null);
        }

        if (round.Status != expectedStatus)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.InvalidState, null);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await applyTransition(round, now, cancellationToken);
        round.Status = targetStatus;
        round.Version += 1;
        round.UpdatedAtUtc = now;
        await AddTransitionAuditAsync(
            round,
            expectedStatus,
            targetStatus,
            actionCode,
            initiatedByUserId,
            null,
            now,
            cancellationToken
        );

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new TransitionGameRoundResult(
            TransitionGameRoundOutcome.Transitioned,
            await LoadRoundDetailsAsync(roundId, cancellationToken)
        );
    }

    private async Task LockRoundAsync(Guid roundId, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM game_rounds WHERE id = {roundId} FOR UPDATE",
                cancellationToken
            );
        }
    }

    private Task<Guid?> ResolveRoundGameIdAsync(Guid roundId, CancellationToken cancellationToken) =>
        _dbContext.GameRounds
            .AsNoTracking()
            .Where(x => x.Id == roundId)
            .Select(x => (Guid?)x.GameId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> LockAndValidateActiveGameAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {gameId} FOR UPDATE",
                cancellationToken
            );
        }

        return await _dbContext.Games.AsNoTracking().AnyAsync(
            x => x.Id == gameId && x.Status == GameStatusValue.Active && !x.IsDeleted,
            cancellationToken
        );
    }

    private Task<bool> IsLatestAuditActionAsync(
        Guid roundId,
        string actionCode,
        CancellationToken cancellationToken
    )
    {
        return _dbContext.GameRoundTransitionAudits
            .Where(x => x.RoundId == roundId)
            .OrderByDescending(x => x.Sequence)
            .Select(x => x.ActionCode == actionCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task AddTransitionAuditAsync(
        GameRound round,
        string? fromStatus,
        string toStatus,
        string actionCode,
        Guid initiatedByUserId,
        string? reason,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken
    )
    {
        var lastSequence = await _dbContext.GameRoundTransitionAudits
            .Where(x => x.RoundId == round.Id)
            .Select(x => (int?)x.Sequence)
            .MaxAsync(cancellationToken) ?? 0;
        _dbContext.GameRoundTransitionAudits.Add(
            new GameRoundTransitionAudit
            {
                RoundId = round.Id,
                Sequence = lastSequence + 1,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ActionCode = actionCode,
                InitiatedByUserId = initiatedByUserId,
                OccurredAtUtc = occurredAtUtc,
                Reason = reason,
                ResultingRoundVersion = round.Version
            }
        );
    }
}
