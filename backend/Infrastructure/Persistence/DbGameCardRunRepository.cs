using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameCardRunRepository : IGameCardRunRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DbGameCardRunRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameCardRunTeamOption>> GetEligibleTeamsAsync(
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
            return Array.Empty<GameCardRunTeamOption>();
        }

        var rosters = await _dbContext.LoadConfirmedTeamRostersAsync(
            activeGameId.Value,
            cancellationToken
        );
        if (rosters.Count == 0)
        {
            return Array.Empty<GameCardRunTeamOption>();
        }

        return rosters
            .Select(roster =>
                new GameCardRunTeamOption(
                    roster.TeamId,
                    roster.TeamSlotIndex,
                    roster.Participants
                        .Select(participant => new GameCardRunParticipantSnapshot(
                            participant.UserId,
                            participant.DisplayName
                        ))
                        .ToArray()
                )
            )
            .ToArray();
    }

    public async Task<GameCardRunDetails?> GetActiveAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeRunId = await _dbContext.GameCardRuns
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game.IsDeleted
                    && x.Game.Status == GameStatusValue.Active
                    && (x.Status == GameCardRunStatusValue.AwaitingModifiers
                        || x.Status == GameCardRunStatusValue.InProgress
                        || x.Status == GameCardRunStatusValue.ReviewingResults)
            )
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeRunId.HasValue)
        {
            return null;
        }

        return await LoadRunDetailsAsync(activeRunId.Value, cancellationToken);
    }

    public async Task<StartGameCardRunResult> StartAsync(
        StartGameCardRunInput input,
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
                return new StartGameCardRunResult(StartGameCardRunOutcome.NoActiveGame, null);
            }

            var activeRun = await _dbContext.GameCardRuns
                .Where(
                    x =>
                        x.GameId == activeGameId.Value
                        && (x.Status == GameCardRunStatusValue.AwaitingModifiers
                            || x.Status == GameCardRunStatusValue.InProgress
                            || x.Status == GameCardRunStatusValue.ReviewingResults)
                )
                .OrderByDescending(x => x.StartedAtUtc)
                .Select(x => new { x.Id, x.BoardCellId, x.TeamId, x.Status })
                .FirstOrDefaultAsync(cancellationToken);
            if (activeRun is not null && activeRun.Status != GameCardRunStatusValue.AwaitingModifiers)
            {
                return new StartGameCardRunResult(StartGameCardRunOutcome.RunAlreadyInProgress, null);
            }

            if (activeRun is not null
                && (activeRun.BoardCellId != input.CellId || activeRun.TeamId != input.TeamId))
            {
                return new StartGameCardRunResult(StartGameCardRunOutcome.RunAlreadyInProgress, null);
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
                return new StartGameCardRunResult(StartGameCardRunOutcome.CellNotFound, null);
            }

            if (cellRow.State != BoardCellState.Open)
            {
                return new StartGameCardRunResult(StartGameCardRunOutcome.CellNotOpen, null);
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
                return new StartGameCardRunResult(StartGameCardRunOutcome.TeamNotFound, null);
            }

            if (teamRow.Status != TeamStatusValue.Confirmed || !teamRow.SlotIndex.HasValue)
            {
                return new StartGameCardRunResult(StartGameCardRunOutcome.TeamNotConfirmed, null);
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
                return new StartGameCardRunResult(
                    StartGameCardRunOutcome.TeamHasNoActiveMembers,
                    null
                );
            }

            if (activeRun is not null)
            {
                var awaitingRun = await _dbContext.GameCardRuns.FirstAsync(
                    x => x.Id == activeRun.Id,
                    cancellationToken
                );
                awaitingRun.Status = GameCardRunStatusValue.InProgress;
                awaitingRun.StartedAtUtc = now;
                awaitingRun.UpdatedAtUtc = now;

                await AddModifierSnapshotsAsync(activeGameId.Value, awaitingRun.Id, now, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new StartGameCardRunResult(
                    StartGameCardRunOutcome.Started,
                    await LoadRunDetailsAsync(awaitingRun.Id, cancellationToken)
                );
            }

            var run = new GameCardRun
            {
                Id = Guid.NewGuid(),
                GameId = activeGameId.Value,
                BoardCellId = input.CellId,
                TeamId = input.TeamId,
                Status = GameCardRunStatusValue.InProgress,
                StartedAtUtc = now,
                BaseScore = cellRow.Cost,
                TeamSlotIndexSnapshot = teamRow.SlotIndex.Value,
                CellRowIndex = cellRow.RowIndex,
                CellColIndex = cellRow.ColIndex,
                CellTitleSnapshot = cellRow.Title,
                CellCostSnapshot = cellRow.Cost,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            _dbContext.GameCardRuns.Add(run);
            _dbContext.GameCardRunParticipants.AddRange(
                participants.Select(
                    x =>
                        new GameCardRunParticipant
                        {
                            Id = Guid.NewGuid(),
                            CardRunId = run.Id,
                            UserId = x.UserId,
                            DisplayNameSnapshot = string.IsNullOrWhiteSpace(x.DisplayName)
                                ? x.UserId.ToString()
                                : x.DisplayName,
                            CreatedAtUtc = now
                        }
                )
            );

            await AddModifierSnapshotsAsync(activeGameId.Value, run.Id, now, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new StartGameCardRunResult(
                StartGameCardRunOutcome.Started,
                await LoadRunDetailsAsync(run.Id, cancellationToken)
            );
        }
    }

    public async Task<ReviewGameCardRunResult> ReviewAsync(
        Guid cardRunId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var run = await _dbContext.GameCardRuns.FirstOrDefaultAsync(
            x => x.Id == cardRunId,
            cancellationToken
        );
        if (run is null)
        {
            return new ReviewGameCardRunResult(ReviewGameCardRunOutcome.NotFound, null);
        }

        if (run.Status != GameCardRunStatusValue.InProgress)
        {
            return new ReviewGameCardRunResult(ReviewGameCardRunOutcome.NotInProgress, null);
        }

        run.Status = GameCardRunStatusValue.ReviewingResults;
        run.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReviewGameCardRunResult(
            ReviewGameCardRunOutcome.Reviewed,
            await LoadRunDetailsAsync(cardRunId, cancellationToken)
        );
    }

    public async Task<FinalizeGameCardRunResult> FinalizeAsync(
        Guid cardRunId,
        FinalizeGameCardRunInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedStatus = input.Status.Trim().ToLowerInvariant();
        if (!FinalizeGameCardRunResult.AllowedTerminalStatuses.Contains(normalizedStatus))
        {
            return new FinalizeGameCardRunResult(FinalizeGameCardRunOutcome.InvalidStatus, null);
        }

        var now = DateTime.UtcNow;
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await using (transaction)
        {
            var run = await _dbContext.GameCardRuns
                .Include(x => x.ModifierResults)
                .FirstOrDefaultAsync(x => x.Id == cardRunId, cancellationToken);
            if (run is null)
            {
                return new FinalizeGameCardRunResult(FinalizeGameCardRunOutcome.NotFound, null);
            }

            if (run.Status != GameCardRunStatusValue.ReviewingResults)
            {
                return new FinalizeGameCardRunResult(
                    FinalizeGameCardRunOutcome.NotInProgress,
                    null
                );
            }

            var modifierInputsById = input.ModifierResults.ToDictionary(x => x.ModifierResultId);
            foreach (var modifierResultId in modifierInputsById.Keys)
            {
                if (run.ModifierResults.All(x => x.Id != modifierResultId))
                {
                    return new FinalizeGameCardRunResult(
                        FinalizeGameCardRunOutcome.ModifierResultNotFound,
                        null
                    );
                }
            }

            foreach (var modifier in run.ModifierResults)
            {
                if (modifierInputsById.TryGetValue(modifier.Id, out var update))
                {
                    modifier.OutcomeStatus = update.OutcomeStatus.Trim().ToLowerInvariant();
                    modifier.ScoreDelta = update.ScoreDelta;
                    modifier.KillDelta = update.KillDelta;
                    modifier.MultiplierApplied = update.MultiplierApplied;
                    modifier.ResolutionDataJson = update.ResolutionDataJson;
                    modifier.ResolvedByUserId = resolvedByUserId;
                    modifier.ResolvedAtUtc = now;
                    modifier.UpdatedAtUtc = now;
                }
                else if (modifier.OutcomeStatus == GameCardRunModifierOutcomeValue.Pending)
                {
                    modifier.OutcomeStatus = GameCardRunModifierOutcomeValue.Cancelled;
                    modifier.ResolvedByUserId = resolvedByUserId;
                    modifier.ResolvedAtUtc = now;
                    modifier.UpdatedAtUtc = now;
                }
            }

            run.Status = normalizedStatus;
            run.FinishedAtUtc = now;
            run.ResolvedByUserId = resolvedByUserId;
            run.UpdatedAtUtc = now;
            run.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            run.FinalScore = input.FinalScore ?? ComputeFinalScore(run, normalizedStatus);

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new FinalizeGameCardRunResult(
                FinalizeGameCardRunOutcome.Completed,
                await LoadRunDetailsAsync(cardRunId, cancellationToken)
            );
        }
    }

    private async Task<GameCardRunDetails> LoadRunDetailsAsync(
        Guid cardRunId,
        CancellationToken cancellationToken
    )
    {
        var run = await _dbContext.GameCardRuns
            .AsNoTracking()
            .Where(x => x.Id == cardRunId)
            .Select(
                x =>
                    new
                    {
                        x.Id,
                        x.GameId,
                        CellId = x.BoardCellId,
                        x.TeamId,
                        TeamSlotIndex = x.TeamSlotIndexSnapshot,
                        x.Status,
                        x.StartedAtUtc,
                        x.FinishedAtUtc,
                        x.BaseScore,
                        x.FinalScore,
                        x.Notes
                    }
            )
            .SingleAsync(cancellationToken);

        var participants = await _dbContext.GameCardRunParticipants
            .AsNoTracking()
            .Where(x => x.CardRunId == cardRunId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new GameCardRunParticipantSnapshot(x.UserId, x.DisplayNameSnapshot))
            .ToArrayAsync(cancellationToken);

        var modifiers = await _dbContext.GameCardRunModifierResults
            .AsNoTracking()
            .Where(x => x.CardRunId == cardRunId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(
                x =>
                    new GameCardRunModifierSnapshot(
                        x.Id,
                        x.ModifierId,
                        x.ModifierNameSnapshot,
                        x.ModifierCategorySnapshot,
                        x.ModifierMechanicTypeSnapshot,
                        x.OutcomeStatus,
                        x.ScoreDelta,
                        x.KillDelta,
                        x.MultiplierApplied,
                        x.ResolutionDataJson,
                        x.ResolvedByUserId,
                        x.ResolvedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        return new GameCardRunDetails(
            run.Id,
            run.GameId,
            run.CellId,
            run.TeamId,
            run.TeamSlotIndex,
            run.Status,
            run.StartedAtUtc,
            run.FinishedAtUtc,
            run.BaseScore,
            run.FinalScore,
            run.Notes,
            participants,
            modifiers
        );
    }

    private async Task AddModifierSnapshotsAsync(
        Guid gameId,
        Guid cardRunId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var activeModifiers = await _dbContext.GameActiveModifiers
            .Where(x => x.GameId == gameId)
            .Select(
                x =>
                    new
                    {
                        x.Id,
                        x.ModifierId,
                        x.ModifierDefinition.Name,
                        x.ModifierDefinition.Category,
                        x.ModifierDefinition.MetadataJson
                    }
            )
            .ToArrayAsync(cancellationToken);

        _dbContext.GameCardRunModifierResults.AddRange(
            activeModifiers.Select(
                x =>
                    new GameCardRunModifierResult
                    {
                        Id = Guid.NewGuid(),
                        CardRunId = cardRunId,
                        GameActiveModifierId = x.Id,
                        ModifierId = x.ModifierId,
                        ModifierNameSnapshot = x.Name,
                        ModifierCategorySnapshot = x.Category,
                        ModifierMechanicTypeSnapshot = ResolveMechanicType(x.MetadataJson),
                        OutcomeStatus = GameCardRunModifierOutcomeValue.Pending,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    }
            )
        );
    }

    private static int ComputeFinalScore(GameCardRun run, string normalizedStatus)
    {
        if (normalizedStatus == GameCardRunStatusValue.Cancelled)
        {
            return 0;
        }

        return run.BaseScore + run.ModifierResults.Sum(x => x.ScoreDelta);
    }

    private static string ResolveMechanicType(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return string.Empty;
        }

        const string marker = "\"mechanicType\":\"";
        var start = metadataJson.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        start += marker.Length;
        var end = metadataJson.IndexOf('"', start);
        return end > start ? metadataJson[start..end] : string.Empty;
    }

    private static bool IsActiveRoundStatus(string status)
    {
        return status == GameCardRunStatusValue.AwaitingModifiers
            || status == GameCardRunStatusValue.InProgress
            || status == GameCardRunStatusValue.ReviewingResults;
    }
}
