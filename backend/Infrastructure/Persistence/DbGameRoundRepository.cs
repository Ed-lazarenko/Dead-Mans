using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameRounds;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository : IGameRoundRepository
{
    private static readonly string[] ActiveRoundStatuses =
    [
        GameRoundStatusValue.AwaitingModifiers,
        GameRoundStatusValue.Preparing,
        GameRoundStatusValue.InProgress,
        GameRoundStatusValue.ReviewingResults
    ];

    private readonly ApplicationDbContext _dbContext;

    public DbGameRoundRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameRoundTeamOption>> GetEligibleTeamsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return Array.Empty<GameRoundTeamOption>();
        }

        var rosters = await _dbContext.LoadConfirmedTeamRostersAsync(
            activeGameId.Value,
            cancellationToken
        );
        if (rosters.Count == 0)
        {
            return Array.Empty<GameRoundTeamOption>();
        }

        return rosters
            .Select(roster =>
                new GameRoundTeamOption(
                    roster.TeamId,
                    roster.TeamName,
                    roster.TeamSlotIndex,
                    roster.Participants
                        .Select(participant => new GameRoundParticipantSnapshot(
                            participant.UserId,
                            participant.DisplayName
                        ))
                        .ToArray()
                )
            )
            .ToArray();
    }

    public async Task<GameRoundDetails?> GetActiveAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeRoundId = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(x => !x.Game.IsDeleted
                && x.Game.Status == GameStatusValue.Active
                && ActiveRoundStatuses.Contains(x.Status))
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeRoundId.HasValue)
        {
            return null;
        }

        return await LoadRoundDetailsAsync(activeRoundId.Value, cancellationToken);
    }

    public async Task<StartGameRoundResult> StartAsync(
        StartGameRoundInput input,
        Guid startedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await using (transaction)
        {
            var activeGameId = await _dbContext.Games
                .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
                .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (!activeGameId.HasValue)
            {
                return new StartGameRoundResult(StartGameRoundOutcome.NoActiveGame, null);
            }

            var activeRound = await _dbContext.GameRounds
                .Where(x => x.GameId == activeGameId.Value
                    && ActiveRoundStatuses.Contains(x.Status))
                .OrderByDescending(x => x.StartedAtUtc)
                .Select(x => new { x.Id, x.BoardCellId, x.TeamId, x.Status })
                .FirstOrDefaultAsync(cancellationToken);
            if (activeRound is not null && activeRound.Status != GameRoundStatusValue.AwaitingModifiers)
            {
                return new StartGameRoundResult(StartGameRoundOutcome.RoundAlreadyInProgress, null);
            }

            if (activeRound is not null
                && (activeRound.BoardCellId != input.CellId || activeRound.TeamId != input.TeamId))
            {
                return new StartGameRoundResult(StartGameRoundOutcome.RoundAlreadyInProgress, null);
            }

            var cellRow = await _dbContext.BoardCells
                .Where(x => x.Id == input.CellId && x.Board.GameId == activeGameId.Value)
                .Select(
                    x =>
                        new
                        {
                            x.Id,
                            x.Board.GameId,
                            x.State,
                            x.RowIndex,
                            x.ColIndex,
                            x.Title,
                            x.Cost
                        }
                )
                .FirstOrDefaultAsync(cancellationToken);
            if (cellRow is null)
            {
                return new StartGameRoundResult(StartGameRoundOutcome.CellNotFound, null);
            }

            if (cellRow.State != BoardCellState.Open)
            {
                return new StartGameRoundResult(StartGameRoundOutcome.CellNotOpen, null);
            }

            var teamRow = await _dbContext.GameTeams
                .Where(x => x.Id == input.TeamId && x.GameId == activeGameId.Value)
                .Select(
                    x =>
                        new
                        {
                            x.Id,
                            x.Status,
                            SlotIndex = x.Slot != null ? x.Slot.SlotIndex : (int?)null
                        }
                )
                .FirstOrDefaultAsync(cancellationToken);
            if (teamRow is null)
            {
                return new StartGameRoundResult(StartGameRoundOutcome.TeamNotFound, null);
            }

            if (teamRow.Status != TeamStatusValue.Confirmed || !teamRow.SlotIndex.HasValue)
            {
                return new StartGameRoundResult(StartGameRoundOutcome.TeamNotConfirmed, null);
            }

            var participants = await _dbContext.GameTeamMembers
                .Where(x => x.TeamId == input.TeamId && x.GameId == activeGameId.Value && x.LeftAtUtc == null)
                .OrderBy(x => x.JoinedAtUtc)
                .Select(
                    x =>
                        new
                        {
                            x.UserId,
                            DisplayName = x.User != null ? x.User.DisplayName : string.Empty
                        }
                )
                .ToArrayAsync(cancellationToken);
            if (participants.Length == 0)
            {
                return new StartGameRoundResult(
                    StartGameRoundOutcome.TeamHasNoActiveMembers,
                    null
                );
            }

            if (activeRound is null)
            {
                return new StartGameRoundResult(
                    StartGameRoundOutcome.AwaitingModifiersRequired,
                    null
                );
            }

            var awaitingRound = await _dbContext.GameRounds.FirstAsync(
                x => x.Id == activeRound.Id,
                cancellationToken
            );
            awaitingRound.Status = GameRoundStatusValue.InProgress;
            awaitingRound.StartedAtUtc = now;
            awaitingRound.PreparedAtUtc = now;
            awaitingRound.GameplayStartedAtUtc = now;
            awaitingRound.Version += 1;
            awaitingRound.UpdatedAtUtc = now;

            await AddModifierSnapshotsAsync(activeGameId.Value, awaitingRound.Id, now, cancellationToken);
            await AddTransitionAuditAsync(
                awaitingRound,
                GameRoundStatusValue.AwaitingModifiers,
                GameRoundStatusValue.InProgress,
                GameRoundTransitionActionValue.BeginGameplay,
                startedByUserId,
                null,
                now,
                cancellationToken
            );

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new StartGameRoundResult(
                StartGameRoundOutcome.Started,
                await LoadRoundDetailsAsync(awaitingRound.Id, cancellationToken)
            );
        }
    }

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
                round.StartedAtUtc = now;
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

    public async Task<TransitionGameRoundResult> RebuildAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await LockRoundAsync(roundId, cancellationToken);

        var round = await _dbContext.GameRounds.FirstOrDefaultAsync(
            x => x.Id == roundId,
            cancellationToken
        );
        if (round is null)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.NotFound, null);
        }

        if (round.Status == GameRoundStatusValue.AwaitingModifiers
            && await IsLatestAuditActionAsync(
                roundId,
                GameRoundTransitionActionValue.Rebuild,
                cancellationToken
            ))
        {
            return new TransitionGameRoundResult(
                TransitionGameRoundOutcome.Transitioned,
                await LoadRoundDetailsAsync(roundId, cancellationToken)
            );
        }

        if (round.Version != input.ExpectedRoundVersion)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.StaleVersion, null);
        }

        if (round.Status != GameRoundStatusValue.Preparing)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.InvalidState, null);
        }

        var hasResults = await _dbContext.GameRoundModifierResults.AnyAsync(
            x => x.RoundId == roundId,
            cancellationToken
        );
        if (hasResults)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.InvalidState, null);
        }

        var now = DateTime.UtcNow;
        await RefundRoundActivationsAsync(
            roundId,
            initiatedByUserId,
            "round_rebuild",
            now,
            cancellationToken
        );

        var fromStatus = round.Status;
        round.Status = GameRoundStatusValue.AwaitingModifiers;
        round.PreparedAtUtc = null;
        round.Version += 1;
        round.UpdatedAtUtc = now;
        await AddTransitionAuditAsync(
            round,
            fromStatus,
            round.Status,
            GameRoundTransitionActionValue.Rebuild,
            initiatedByUserId,
            "round_rebuild",
            now,
            cancellationToken
        );
        await IncrementBoardVersionAsync(round.GameId, cancellationToken);

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

    public async Task<TransitionGameRoundResult> TechnicalCancelAsync(
        Guid roundId,
        TechnicalCancelGameRoundInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var reasonCode = input.ReasonCode?.Trim().ToLowerInvariant() ?? string.Empty;
        var internalDetail = input.InternalDetail?.Trim() ?? string.Empty;
        var publicSummary = string.IsNullOrWhiteSpace(input.PublicSummary)
            ? null
            : input.PublicSummary.Trim();
        if (!GameRoundTechnicalCancellationReasonValue.Allowed.Contains(reasonCode)
            || internalDetail.Length is < 1 or > 2000
            || publicSummary?.Length > 500
            || (reasonCode == GameRoundTechnicalCancellationReasonValue.Other
                && publicSummary is null))
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.InvalidRequest, null);
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await LockRoundAsync(roundId, cancellationToken);

        var round = await _dbContext.GameRounds
            .Include(x => x.ModifierResults)
            .FirstOrDefaultAsync(x => x.Id == roundId, cancellationToken);
        if (round is null)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.NotFound, null);
        }

        if (round.Status == GameRoundStatusValue.Cancelled)
        {
            return new TransitionGameRoundResult(
                TransitionGameRoundOutcome.Transitioned,
                await LoadRoundDetailsAsync(roundId, cancellationToken)
            );
        }

        if (round.Version != input.ExpectedRoundVersion)
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.StaleVersion, null);
        }

        if (!ActiveRoundStatuses.Contains(round.Status))
        {
            return new TransitionGameRoundResult(TransitionGameRoundOutcome.InvalidState, null);
        }

        var now = DateTime.UtcNow;
        var fromStatus = round.Status;
        await RefundRoundActivationsAsync(
            roundId,
            initiatedByUserId,
            $"technical_cancel:{reasonCode}",
            now,
            cancellationToken
        );

        foreach (var result in round.ModifierResults)
        {
            result.OutcomeStatus = GameRoundModifierOutcomeValue.Cancelled;
            result.ScoreDelta = 0;
            result.KillDelta = 0;
            result.MultiplierApplied = null;
            result.ResolutionDataJson = null;
            result.ResolvedByUserId = initiatedByUserId;
            result.ResolvedAtUtc = now;
            result.UpdatedAtUtc = now;
        }

        round.Status = GameRoundStatusValue.Cancelled;
        round.FinishedAtUtc = now;
        round.FinalScore = 0;
        round.EmptyCardPenaltyApplied = false;
        round.ResolvedByUserId = initiatedByUserId;
        round.TechnicalCancellationReasonCode = reasonCode;
        round.PublicCancellationSummary = publicSummary;
        round.InternalCancellationDetail = internalDetail;
        round.Version += 1;
        round.UpdatedAtUtc = now;

        var cell = await _dbContext.BoardCells.FirstAsync(
            x => x.Id == round.BoardCellId,
            cancellationToken
        );
        cell.State = BoardCellState.Cancelled;
        var game = await _dbContext.Games.FirstAsync(x => x.Id == round.GameId, cancellationToken);
        game.ActiveTeamId = null;
        await IncrementBoardVersionAsync(round.GameId, cancellationToken);
        await AddTransitionAuditAsync(
            round,
            fromStatus,
            round.Status,
            GameRoundTransitionActionValue.TechnicalCancel,
            initiatedByUserId,
            internalDetail,
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
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

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

        var now = DateTime.UtcNow;
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

        foreach (var activation in activations)
        {
            activation.Status = GameModifierActivationStatusValue.Cancelled;
            activation.CancelledByUserId = cancelledByUserId;
            activation.CancelledAtUtc = now;
            activation.CancellationReason = reason;
            activation.RefundAmount = activation.ActivationCostSnapshot;
            activation.ArchivedAtUtc = now;
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
