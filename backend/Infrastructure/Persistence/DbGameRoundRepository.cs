using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameModifiers;
using backend.Application.Features.GameRounds;
using backend.Application.Features.Scoring;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameRoundRepository : IGameRoundRepository
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

    public async Task<FinalizeGameRoundResult> FinalizeAsync(
        Guid roundId,
        FinalizeGameRoundInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedStatus = input.Status?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!FinalizeGameRoundResult.AllowedFinalizeStatuses.Contains(normalizedStatus))
        {
            return new FinalizeGameRoundResult(FinalizeGameRoundOutcome.InvalidStatus, null);
        }

        var now = DateTime.UtcNow;
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await using (transaction)
        {
            await LockRoundAsync(roundId, cancellationToken);
            var round = await _dbContext.GameRounds
                .Include(x => x.ModifierResults)
                .FirstOrDefaultAsync(x => x.Id == roundId, cancellationToken);
            if (round is null)
            {
                return new FinalizeGameRoundResult(FinalizeGameRoundOutcome.NotFound, null);
            }

            if (input.ExpectedRoundVersion.HasValue
                && input.ExpectedRoundVersion.Value != round.Version)
            {
                return new FinalizeGameRoundResult(
                    FinalizeGameRoundOutcome.StaleVersion,
                    null
                );
            }

            if (round.Status != GameRoundStatusValue.ReviewingResults)
            {
                return new FinalizeGameRoundResult(
                    FinalizeGameRoundOutcome.NotInProgress,
                    null
                );
            }

            if (!TryCreateModifierInputsById(
                    input.ModifierResults,
                    out var modifierInputsById,
                    out var modifierInputErrorCode
                ))
            {
                return new FinalizeGameRoundResult(
                    FinalizeGameRoundOutcome.InvalidModifierResults,
                    null,
                    modifierInputErrorCode
                );
            }

            foreach (var modifierResultId in modifierInputsById.Keys)
            {
                if (round.ModifierResults.All(x => x.Id != modifierResultId))
                {
                    return new FinalizeGameRoundResult(
                        FinalizeGameRoundOutcome.InvalidModifierResults,
                        null,
                        "modifier_resolution.result_set_mismatch"
                    );
                }
            }

            round.Status = normalizedStatus;
            round.FinishedAtUtc = now;
            round.ResolvedByUserId = resolvedByUserId;
            round.UpdatedAtUtc = now;
            round.KillsCount = input.KillsCount;
            round.BountyCount = input.BountyCount;
            round.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            if (!TryApplyModifierScoring(
                    round,
                    modifierInputsById,
                    input.RuleGroups,
                    resolvedByUserId,
                    now,
                    out var scoringErrorCode,
                    out var isConfigurationError
                ))
            {
                return new FinalizeGameRoundResult(
                    isConfigurationError
                        ? FinalizeGameRoundOutcome.CalculationFailed
                        : FinalizeGameRoundOutcome.InvalidModifierResults,
                    null,
                    scoringErrorCode
                );
            }
            var scoreBreakdown = CalculateRoundScore(round, normalizedStatus);
            round.FinalScore = scoreBreakdown.FinalScore;
            round.EmptyCardPenaltyApplied = scoreBreakdown.EmptyCardPenaltyApplied;
            round.Version += 1;
            await AddTransitionAuditAsync(
                round,
                GameRoundStatusValue.ReviewingResults,
                GameRoundStatusValue.Completed,
                GameRoundTransitionActionValue.Finalize,
                resolvedByUserId,
                null,
                now,
                cancellationToken
            );

            var activeGameModifiers = await _dbContext.GameModifierActivations
                .Where(
                    x =>
                        x.RoundId == round.Id
                        && x.Status == GameModifierActivationStatusValue.Consumed
                        && x.ArchivedAtUtc == null
                )
                .ToArrayAsync(cancellationToken);
            foreach (var modifier in activeGameModifiers)
            {
                modifier.ArchivedAtUtc = now;
            }

            var game = await _dbContext.Games.FirstAsync(x => x.Id == round.GameId, cancellationToken);
            game.ActiveTeamId = null;

            var board = await _dbContext.GameBoards.FirstAsync(x => x.GameId == round.GameId, cancellationToken);
            board.Version += 1;

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new FinalizeGameRoundResult(
                FinalizeGameRoundOutcome.Completed,
                await LoadRoundDetailsAsync(roundId, cancellationToken)
            );
        }
    }

    public async Task<PreviewGameRoundScoreResult> PreviewScoreAsync(
        Guid roundId,
        FinalizeGameRoundInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedStatus = input.Status?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!FinalizeGameRoundResult.AllowedFinalizeStatuses.Contains(normalizedStatus))
        {
            return new PreviewGameRoundScoreResult(
                FinalizeGameRoundOutcome.InvalidStatus,
                null,
                Array.Empty<GameRoundModifierSnapshot>()
            );
        }

        var round = await _dbContext.GameRounds
            .AsNoTracking()
            .Include(x => x.ModifierResults)
            .FirstOrDefaultAsync(x => x.Id == roundId, cancellationToken);
        if (round is null)
        {
            return new PreviewGameRoundScoreResult(
                FinalizeGameRoundOutcome.NotFound,
                null,
                Array.Empty<GameRoundModifierSnapshot>()
            );
        }

        if (input.ExpectedRoundVersion.HasValue
            && input.ExpectedRoundVersion.Value != round.Version)
        {
            return new PreviewGameRoundScoreResult(
                FinalizeGameRoundOutcome.StaleVersion,
                null,
                []
            );
        }

        if (round.Status != GameRoundStatusValue.ReviewingResults)
        {
            return new PreviewGameRoundScoreResult(
                FinalizeGameRoundOutcome.NotInProgress,
                null,
                Array.Empty<GameRoundModifierSnapshot>()
            );
        }

        if (!TryCreateModifierInputsById(
                input.ModifierResults,
                out var modifierInputsById,
                out var modifierInputErrorCode
            ))
        {
            return new PreviewGameRoundScoreResult(
                FinalizeGameRoundOutcome.InvalidModifierResults,
                null,
                Array.Empty<GameRoundModifierSnapshot>(),
                ErrorCode: modifierInputErrorCode
            );
        }

        foreach (var modifierResultId in modifierInputsById.Keys)
        {
            if (round.ModifierResults.All(x => x.Id != modifierResultId))
            {
                return new PreviewGameRoundScoreResult(
                    FinalizeGameRoundOutcome.InvalidModifierResults,
                    null,
                    Array.Empty<GameRoundModifierSnapshot>(),
                    ErrorCode: "modifier_resolution.result_set_mismatch"
                );
            }
        }

        round.Status = normalizedStatus;
        round.KillsCount = input.KillsCount;
        round.BountyCount = input.BountyCount;
        if (!TryApplyModifierScoring(
                round,
                modifierInputsById,
                input.RuleGroups,
                resolvedByUserId,
                DateTime.UtcNow,
                out var scoringErrorCode,
                out var isConfigurationError
            ))
        {
            return new PreviewGameRoundScoreResult(
                isConfigurationError
                    ? FinalizeGameRoundOutcome.CalculationFailed
                    : FinalizeGameRoundOutcome.InvalidModifierResults,
                null,
                Array.Empty<GameRoundModifierSnapshot>(),
                ErrorCode: scoringErrorCode
            );
        }
        var scoreBreakdown = CalculateRoundScore(round, normalizedStatus);

        return new PreviewGameRoundScoreResult(
            FinalizeGameRoundOutcome.Completed,
            ToScoreDetails(scoreBreakdown),
            round.ModifierResults
                .OrderBy(x => x.CreatedAtUtc)
                .Select(ToModifierSnapshot)
                .ToArray(),
            round.Version,
            CalculateNormalizedInputHash(input),
            round.ModifierResults
                .Where(
                    result => ModifierBehaviorV2Json.TryDeserialize(
                        result.ModifierBehaviorV2SnapshotJson,
                        out _
                    )
                )
                .OrderBy(result => result.CreatedAtUtc)
                .Select(CreateCalculationTrace)
                .ToArray()
        );
    }

    private static GameRoundModifierCalculationTrace CreateCalculationTrace(
        GameRoundModifierResult result
    )
    {
        var behavior = ModifierBehaviorV2Json.Deserialize(
            result.ModifierBehaviorV2SnapshotJson!
        );
        return new GameRoundModifierCalculationTrace(
            result.Id,
            result.GameModifierActivationId,
            behavior.FormulaReference?.Code,
            behavior.FormulaReference?.Version,
            result.ResolutionKind ?? "ruleStatus",
            result.ScoreDelta,
            result.KillDelta
        );
    }

    private static string CalculateNormalizedInputHash(FinalizeGameRoundInput input)
    {
        var canonical = JsonSerializer.Serialize(
            new
            {
                Status = input.Status.Trim().ToLowerInvariant(),
                input.KillsCount,
                input.BountyCount,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                ModifierResults = input.ModifierResults
                    .OrderBy(value => value.ModifierResultId)
                    .Select(
                        value => new
                        {
                            value.ModifierResultId,
                            value.CountValue,
                            value.IsConditionMet
                        }
                    ),
                RuleGroups = input.RuleGroups
                    .OrderBy(value => value.ResolutionGroupId)
                    .Select(
                        value => new
                        {
                            value.ResolutionGroupId,
                            MemberResultIds = value.MemberResultIds.Order().ToArray(),
                            OutcomeStatus = value.OutcomeStatus.Trim().ToLowerInvariant(),
                            ViolationComment = string.IsNullOrWhiteSpace(value.ViolationComment)
                                ? null
                                : value.ViolationComment.Trim()
                        }
                    )
            },
            JsonOptions
        );
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
    }

    private async Task<GameRoundDetails> LoadRoundDetailsAsync(
        Guid roundId,
        CancellationToken cancellationToken
    )
    {
        var round = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(x => x.Id == roundId)
            .Select(
                x =>
                    new
                    {
                        x.Id,
                        x.GameId,
                        CellId = x.BoardCellId,
                        CellTitle = x.CellTitleSnapshot,
                        CellDescription = x.CellDescriptionSnapshot,
                        x.TeamId,
                        TeamName = x.Team.Name,
                        TeamSlotIndex = x.TeamSlotIndexSnapshot,
                        x.Status,
                        x.Version,
                        x.StartedAtUtc,
                        x.PreparedAtUtc,
                        x.GameplayStartedAtUtc,
                        x.ReviewedAtUtc,
                        x.FinishedAtUtc,
                        x.BaseScore,
                        x.FinalScore,
                        x.EmptyCardPenaltyApplied,
                        x.KillsCount,
                        x.BountyCount,
                        x.Notes,
                        x.TechnicalCancellationReasonCode,
                        x.PublicCancellationSummary
                    }
            )
            .SingleAsync(cancellationToken);

        var participants = await _dbContext.GameRoundParticipants
            .AsNoTracking()
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new GameRoundParticipantSnapshot(x.UserId, x.DisplayNameSnapshot))
            .ToArrayAsync(cancellationToken);

        var modifierRows = await _dbContext.GameRoundModifierResults
            .AsNoTracking()
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(
                x =>
                    new
                    {
                        x.Id,
                        x.ModifierId,
                        x.ModifierNameSnapshot,
                        x.ModifierCategorySnapshot,
                        x.ModifierDescriptionSnapshot,
                        x.OutcomeStatus,
                        x.ScoreDelta,
                        x.KillDelta,
                        x.MultiplierApplied,
                        x.ResolutionDataJson,
                        x.ResolvedByUserId,
                        x.ResolvedAtUtc,
                        x.GameModifierActivationId,
                        x.DefinitionRevisionSnapshot,
                        x.ResolutionGroupId,
                        x.ResolutionKind,
                        x.ViolationComment,
                        x.ModifierBehaviorV2SnapshotJson
                    }
            )
            .ToArrayAsync(cancellationToken);
        var modifiers = modifierRows
            .Select(
                x =>
                    new GameRoundModifierSnapshot(
                        x.Id,
                        x.ModifierId,
                        x.ModifierNameSnapshot,
                        x.ModifierCategorySnapshot,
                        x.ModifierDescriptionSnapshot,
                        x.OutcomeStatus,
                        x.ScoreDelta,
                        x.KillDelta,
                        x.MultiplierApplied,
                        x.ResolutionDataJson,
                        x.ResolvedByUserId,
                        x.ResolvedAtUtc,
                        x.GameModifierActivationId,
                        x.DefinitionRevisionSnapshot,
                        x.ResolutionGroupId,
                        x.ResolutionKind,
                        x.ViolationComment,
                        string.IsNullOrWhiteSpace(x.ModifierBehaviorV2SnapshotJson)
                            ? null
                            : ModifierBehaviorV2Json.Deserialize(
                                x.ModifierBehaviorV2SnapshotJson
                            )
                    )
            )
            .ToArray();

        var scoreDetails = ToScoreDetails(CalculateRoundScoreSnapshot(
            round.Status,
            round.BaseScore,
            round.KillsCount,
            round.BountyCount,
            modifiers
        ));

        return new GameRoundDetails(
            round.Id,
            round.GameId,
            round.CellId,
            round.CellTitle,
            round.CellDescription,
            round.TeamId,
            round.TeamName,
            round.TeamSlotIndex,
            round.Status,
            round.Version,
            round.StartedAtUtc,
            round.PreparedAtUtc,
            round.GameplayStartedAtUtc,
            round.ReviewedAtUtc,
            round.FinishedAtUtc,
            round.BaseScore,
            round.FinalScore,
            round.EmptyCardPenaltyApplied,
            scoreDetails,
            round.KillsCount,
            round.BountyCount,
            round.Notes,
            round.TechnicalCancellationReasonCode,
            round.PublicCancellationSummary,
            DateTime.UtcNow,
            participants,
            modifiers
        );
    }

    private async Task AddModifierSnapshotsAsync(
        Guid gameId,
        Guid roundId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var activeModifiers = await _dbContext.GameModifierActivations
            .Include(x => x.ModifierDefinition)
            .Where(
                x =>
                    x.GameId == gameId
                    && x.RoundId == roundId
                    && x.Status == GameModifierActivationStatusValue.Active
            )
            .ToArrayAsync(cancellationToken);

        var resolutionGroupIds = activeModifiers
            .Select(
                activation => new
                {
                    activation.ModifierId,
                    BehaviorJson = ResolveBehaviorSnapshotJson(activation)
                }
            )
            .Where(
                item => ModifierBehaviorV2Json.TryDeserialize(item.BehaviorJson, out var behavior)
                    && behavior is
                    {
                        Kind: ModifierBehaviorKind.Rule,
                        StackingPolicy: ModifierStackingPolicy.AggregateParameters
                    }
            )
            .Select(item => item.ModifierId)
            .Distinct()
            .ToDictionary(modifierId => modifierId, _ => Guid.NewGuid());

        _dbContext.GameRoundModifierResults.AddRange(
            activeModifiers.Select(
                x =>
                    new GameRoundModifierResult
                    {
                        Id = Guid.NewGuid(),
                        RoundId = roundId,
                        GameModifierActivationId = x.Id,
                        ModifierId = x.ModifierId,
                        ModifierNameSnapshot = string.IsNullOrWhiteSpace(x.ModifierNameSnapshot)
                            ? x.ModifierDefinition.Name
                            : x.ModifierNameSnapshot,
                        ModifierCategorySnapshot = string.IsNullOrWhiteSpace(x.ModifierCategorySnapshot)
                            ? x.ModifierDefinition.Category
                            : x.ModifierCategorySnapshot,
                        ModifierDescriptionSnapshot = string.IsNullOrWhiteSpace(x.ModifierDescriptionSnapshot)
                            ? x.ModifierDefinition.Description
                            : x.ModifierDescriptionSnapshot,
                        DefinitionRevisionSnapshot = x.DefinitionRevisionSnapshot > 0
                            ? x.DefinitionRevisionSnapshot
                            : Math.Max(1, x.ModifierDefinition.Revision),
                        ModifierActivationCommandSnapshot = x.ActivationCommandSnapshot
                            ?? x.ModifierDefinition.ActivationCommand,
                        ModifierNormalizedTagsSnapshot = x.NormalizedTagsSnapshot.Length > 0
                            ? x.NormalizedTagsSnapshot.ToArray()
                            : x.ModifierDefinition.NormalizedTags.ToArray(),
                        ModifierBehaviorV2SnapshotJson = ResolveBehaviorSnapshotJson(x),
                        ResolutionGroupId = resolutionGroupIds.GetValueOrDefault(x.ModifierId),
                        ResolutionKind = ResolveResolutionKind(ResolveBehaviorSnapshotJson(x)),
                        OutcomeStatus = GameRoundModifierOutcomeValue.Pending,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    }
            )
        );

        foreach (var activation in activeModifiers)
        {
            activation.Status = GameModifierActivationStatusValue.Consumed;
        }
    }

    private static GameRoundScoreBreakdown CalculateRoundScore(GameRound round, string normalizedStatus)
    {
        return GameRoundScoreCalculator.Calculate(CreateScoreInput(
            normalizedStatus,
            round.BaseScore,
            round.KillsCount,
            round.BountyCount,
            round.ModifierResults.Select(x => new GameRoundScoreModifierInput(x.ScoreDelta, x.KillDelta))
        ));
    }

    private static GameRoundScoreBreakdown CalculateRoundScoreSnapshot(
        string status,
        int baseScore,
        int killsCount,
        int bountyCount,
        IReadOnlyList<GameRoundModifierSnapshot> modifiers
    )
    {
        return GameRoundScoreCalculator.Calculate(CreateScoreInput(
            status,
            baseScore,
            killsCount,
            bountyCount,
            modifiers.Select(x => new GameRoundScoreModifierInput(x.ScoreDelta, x.KillDelta))
        ));
    }

    private static GameRoundScoreInput CreateScoreInput(
        string status,
        int baseScore,
        int killsCount,
        int bountyCount,
        IEnumerable<GameRoundScoreModifierInput> modifiers
    )
    {
        return new GameRoundScoreInput(
            status,
            baseScore,
            killsCount,
            bountyCount,
            modifiers.ToArray()
        );
    }

    private static GameRoundScoreDetails ToScoreDetails(GameRoundScoreBreakdown breakdown)
    {
        return new GameRoundScoreDetails(
            breakdown.ScoreUnit,
            breakdown.KillsScore,
            breakdown.BountyScore,
            breakdown.ModifierKillDelta,
            breakdown.ModifierKillScore,
            breakdown.ModifierScoreDelta,
            breakdown.EmptyCardPenaltyApplied,
            breakdown.EmptyCardPenaltyScore,
            breakdown.PenaltyTotal,
            breakdown.BonusDelta,
            breakdown.TotalKillCount,
            breakdown.FinalScore
        );
    }

    private static bool TryCreateModifierInputsById(
        IReadOnlyList<FinalizeGameRoundModifierInput> modifierInputs,
        out IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        out string? errorCode
    )
    {
        var dictionary = new Dictionary<Guid, FinalizeGameRoundModifierInput>();
        foreach (var input in modifierInputs)
        {
            if (!dictionary.TryAdd(input.ModifierResultId, input))
            {
                modifierInputsById = dictionary;
                errorCode = "modifier_resolution.duplicate_result";
                return false;
            }
        }

        modifierInputsById = dictionary;
        errorCode = null;
        return true;
    }

    private static void ApplyModifierScoring(
        GameRound round,
        IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroupInputs,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc
    )
    {
        var v2Results = new List<(GameRoundModifierResult Result, ModifierBehaviorV2 Behavior)>();
        foreach (var result in round.ModifierResults)
        {
            if (string.IsNullOrWhiteSpace(result.ModifierBehaviorV2SnapshotJson))
            {
                throw new ModifierScoringException("behavior.invalid", true);
            }

            try
            {
                v2Results.Add(
                    (
                        result,
                        ModifierBehaviorV2Json.Deserialize(
                            result.ModifierBehaviorV2SnapshotJson
                        )
                    )
                );
            }
            catch (JsonException)
            {
                throw new ModifierScoringException("behavior.invalid", true);
            }
        }
        ApplyBehaviorV2Scoring(
            round,
            v2Results,
            modifierInputsById,
            ruleGroupInputs,
            resolvedByUserId,
            resolvedAtUtc
        );
    }

    private static void ApplyBehaviorV2Scoring(
        GameRound round,
        IReadOnlyList<(GameRoundModifierResult Result, ModifierBehaviorV2 Behavior)> results,
        IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroupInputs,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc
    )
    {
        var calculations = new List<ModifierInstanceCalculationInput>(results.Count);
        var inputByActivationId = new Dictionary<Guid, ModifierResolutionInput>();
        var violationCommentByActivationId = new Dictionary<Guid, string>();
        var ruleGroups = results
            .Where(item => item.Behavior.Kind == ModifierBehaviorKind.Rule)
            .GroupBy(item => item.Result.ResolutionGroupId ?? item.Result.ModifierId)
            .ToArray();
        var ruleGroupInputsById = new Dictionary<Guid, FinalizeGameRoundRuleGroupInput>();
        foreach (var groupInput in ruleGroupInputs)
        {
            if (!ruleGroupInputsById.TryAdd(groupInput.ResolutionGroupId, groupInput))
            {
                throw new ModifierScoringException("modifier_resolution.duplicate_group");
            }
        }
        if (ruleGroupInputsById.Count != ruleGroups.Length)
        {
            throw new ModifierScoringException("modifier_resolution.group_set_mismatch");
        }

        foreach (var group in ruleGroups)
        {
            if (!ruleGroupInputsById.TryGetValue(group.Key, out var provided)
                || !TryMapRuleOutcome(provided.OutcomeStatus, out var ruleOutcome))
            {
                throw new ModifierScoringException("modifier_resolution.group_missing");
            }

            var expectedMemberIds = group.Select(item => item.Result.Id).Order().ToArray();
            var providedMemberIds = provided.MemberResultIds.Distinct().Order().ToArray();
            if (provided.MemberResultIds.Count != providedMemberIds.Length
                || !expectedMemberIds.SequenceEqual(providedMemberIds)
                || expectedMemberIds.Any(modifierInputsById.ContainsKey))
            {
                throw new ModifierScoringException("modifier_resolution.group_members_mismatch");
            }

            var comment = string.IsNullOrWhiteSpace(provided.ViolationComment)
                ? null
                : provided.ViolationComment!.Trim();
            if (ruleOutcome == ModifierRuleOutcome.Violated
                && comment is not { Length: >= 1 and <= 1000 })
            {
                throw new ModifierScoringException("modifier_resolution.violation_comment_required");
            }

            foreach (var item in group)
            {
                inputByActivationId[item.Result.GameModifierActivationId] =
                    new RuleStatusInput(ruleOutcome);
                if (ruleOutcome == ModifierRuleOutcome.Violated)
                {
                    violationCommentByActivationId[item.Result.GameModifierActivationId] = comment!;
                }
            }
        }

        foreach (var item in results.Where(item => item.Behavior.Kind == ModifierBehaviorKind.Scoring))
        {
            var hasInput = modifierInputsById.TryGetValue(item.Result.Id, out var input);
            ModifierResolutionInput resolutionInput;
            switch (item.Behavior.Resolution)
            {
                case AutomaticRoundMetricResolution:
                    if (hasInput)
                    {
                        throw new ModifierScoringException("modifier_resolution.automatic_input_forbidden");
                    }
                    resolutionInput = new AutomaticRoundMetricInput();
                    break;
                case BooleanResolution:
                    if (!hasInput || !input!.IsConditionMet.HasValue)
                    {
                        throw new ModifierScoringException("modifier_resolution.boolean_required");
                    }
                    resolutionInput = new BooleanInput(input.IsConditionMet.Value);
                    break;
                case NonNegativeCountResolution:
                    if (!hasInput || input!.CountValue is null or < 0)
                    {
                        throw new ModifierScoringException("modifier_resolution.non_negative_count_required");
                    }
                    resolutionInput = new NonNegativeCountInput(input.CountValue.Value);
                    break;
                default:
                    throw new ModifierScoringException("modifier_resolution.unsupported");
            }

            inputByActivationId[item.Result.GameModifierActivationId] = resolutionInput;
        }

        foreach (var item in results)
        {
            if (!inputByActivationId.TryGetValue(
                    item.Result.GameModifierActivationId,
                    out var resolutionInput
                ))
            {
                throw new ModifierScoringException("modifier_resolution.missing");
            }

            calculations.Add(
                new ModifierInstanceCalculationInput(
                    new ModifierActivationSnapshotV2(
                        item.Result.GameModifierActivationId,
                        item.Result.ModifierId,
                        item.Result.DefinitionRevisionSnapshot,
                        item.Result.ModifierNameSnapshot,
                        item.Behavior
                    ),
                    resolutionInput
                )
            );
        }

        var calculation = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(round.BaseScore, round.KillsCount, round.BountyCount),
            calculations
        );
        if (!calculation.IsSuccess)
        {
            throw new ModifierScoringException(
                calculation.Errors.FirstOrDefault()?.Code ?? "modifier_calculation.failed",
                IsConfigurationError(
                    calculation.Errors.FirstOrDefault()?.Code ?? "modifier_calculation.failed"
                )
            );
        }

        foreach (var item in results)
        {
            var outcome = calculation.Calculation!.Instances.Single(
                value => value.ActivationId == item.Result.GameModifierActivationId
            );
            var input = inputByActivationId[item.Result.GameModifierActivationId];
            item.Result.OutcomeStatus = ToPersistenceOutcome(input, outcome);
            item.Result.ScoreDelta = outcome.PointsDelta;
            item.Result.KillDelta = outcome.BonusKillsDelta;
            item.Result.MultiplierApplied = null;
            item.Result.ResolutionDataJson = SerializeBehaviorV2Resolution(input);
            item.Result.ViolationComment = violationCommentByActivationId.GetValueOrDefault(
                item.Result.GameModifierActivationId
            );
            item.Result.CalculationBreakdownJson = JsonSerializer.Serialize(
                new
                {
                    SchemaVersion = ModifierBehaviorSchemaVersions.V2,
                    FormulaCode = item.Behavior.FormulaReference?.Code,
                    FormulaVersion = item.Behavior.FormulaReference?.Version,
                    outcome.PointsDelta,
                    outcome.BonusKillsDelta,
                    outcome.RuleOutcome,
                    outcome.CountInput,
                    outcome.BooleanInput
                },
                JsonOptions
            );
            item.Result.ResolvedByUserId = resolvedByUserId;
            item.Result.ResolvedAtUtc = resolvedAtUtc;
            item.Result.UpdatedAtUtc = resolvedAtUtc;
        }

    }

    private static bool TryMapRuleOutcome(string value, out ModifierRuleOutcome outcome)
    {
        outcome = value.Trim().ToLowerInvariant() switch
        {
            "completed" => ModifierRuleOutcome.Completed,
            "violated" or "failed" => ModifierRuleOutcome.Violated,
            "nottriggered" or "not_triggered" or "cancelled" =>
                ModifierRuleOutcome.NotTriggered,
            _ => (ModifierRuleOutcome)(-1)
        };
        return Enum.IsDefined(outcome);
    }

    private static string ToPersistenceOutcome(
        ModifierResolutionInput input,
        ModifierInstanceOutcome outcome
    ) => input switch
    {
        RuleStatusInput { Outcome: ModifierRuleOutcome.Completed } =>
            GameRoundModifierOutcomeValue.Completed,
        RuleStatusInput { Outcome: ModifierRuleOutcome.Violated } =>
            GameRoundModifierOutcomeValue.Violated,
        RuleStatusInput => GameRoundModifierOutcomeValue.NotTriggered,
        BooleanInput { Succeeded: true } => GameRoundModifierOutcomeValue.Succeeded,
        BooleanInput => GameRoundModifierOutcomeValue.NotSucceeded,
        NonNegativeCountInput or AutomaticRoundMetricInput =>
            GameRoundModifierOutcomeValue.Calculated,
        _ => throw new ArgumentOutOfRangeException(nameof(input))
    };

    private static string SerializeBehaviorV2Resolution(ModifierResolutionInput input)
    {
        object payload = input switch
        {
            RuleStatusInput value => new
            {
                Type = "ruleStatus",
                Outcome = value.Outcome.ToString()
            },
            BooleanInput value => new { Type = "boolean", value.Succeeded },
            NonNegativeCountInput value => new { Type = "nonNegativeCount", value.Count },
            AutomaticRoundMetricInput => new { Type = "automaticRoundMetric" },
            _ => throw new ArgumentOutOfRangeException(nameof(input))
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static bool TryApplyModifierScoring(
        GameRound round,
        IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroupInputs,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        out string? errorCode,
        out bool isConfigurationError
    )
    {
        try
        {
            ApplyModifierScoring(
                round,
                modifierInputsById,
                ruleGroupInputs,
                resolvedByUserId,
                resolvedAtUtc
            );
            errorCode = null;
            isConfigurationError = false;
            return true;
        }
        catch (ModifierScoringException exception)
        {
            errorCode = exception.Code;
            isConfigurationError = exception.IsConfigurationError;
            return false;
        }
        catch (InvalidOperationException)
        {
            errorCode = "modifier_calculation.failed";
            isConfigurationError = true;
            return false;
        }
    }

    private static bool IsConfigurationError(string code) => code is
        "behavior.invalid"
        or "behavior.rule_incompatible"
        or "formula.unsupported"
        or "formula.incompatible"
        or "round_facts.invalid"
        or "activation.duplicate"
        or "modifier_calculation.failed";

    private static GameRoundModifierSnapshot ToModifierSnapshot(GameRoundModifierResult modifier)
    {
        return new GameRoundModifierSnapshot(
            modifier.Id,
            modifier.ModifierId,
            modifier.ModifierNameSnapshot,
            modifier.ModifierCategorySnapshot,
            modifier.ModifierDescriptionSnapshot,
            modifier.OutcomeStatus,
            modifier.ScoreDelta,
            modifier.KillDelta,
            modifier.MultiplierApplied,
            modifier.ResolutionDataJson,
            modifier.ResolvedByUserId,
            modifier.ResolvedAtUtc,
            modifier.GameModifierActivationId,
            modifier.DefinitionRevisionSnapshot,
            modifier.ResolutionGroupId,
            modifier.ResolutionKind,
            modifier.ViolationComment,
            string.IsNullOrWhiteSpace(modifier.ModifierBehaviorV2SnapshotJson)
                ? null
                : ModifierBehaviorV2Json.Deserialize(modifier.ModifierBehaviorV2SnapshotJson)
        );
    }

    private static string? ResolveResolutionKind(string? behaviorJson)
    {
        if (string.IsNullOrWhiteSpace(behaviorJson))
        {
            return null;
        }

        var behavior = ModifierBehaviorV2Json.Deserialize(behaviorJson);
        return behavior.Resolution switch
        {
            RuleStatusResolution => "ruleStatus",
            BooleanResolution => "boolean",
            NonNegativeCountResolution => "nonNegativeCount",
            AutomaticRoundMetricResolution => "automaticRoundMetric",
            _ => throw new InvalidOperationException("Unsupported modifier resolution type.")
        };
    }

    private static string ResolveBehaviorSnapshotJson(
        Data.Entities.GameModifierActivation activation
    )
        => ModifierBehaviorV2Json.Serialize(
            ModifierBehaviorV2Json.Deserialize(activation.BehaviorV2SnapshotJson)
        );

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class ModifierScoringException(
        string code,
        bool isConfigurationError = false
    ) : Exception(code)
    {
        public string Code { get; } = code;
        public bool IsConfigurationError { get; } = isConfigurationError;
    }

}
