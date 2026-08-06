using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameModifiers;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

            if (activeRun is null)
            {
                return new StartGameCardRunResult(
                    StartGameCardRunOutcome.AwaitingModifiersRequired,
                    null
                );
            }

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
                    var normalizedOutcomeStatus = update.OutcomeStatus.Trim().ToLowerInvariant();
                    if (!AllowedModifierOutcomeStatuses.Contains(normalizedOutcomeStatus))
                    {
                        return new FinalizeGameCardRunResult(
                            FinalizeGameCardRunOutcome.InvalidStatus,
                            null
                        );
                    }

                    modifier.OutcomeStatus = normalizedOutcomeStatus;
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
            run.KillsCount = input.KillsCount;
            run.BountyCount = input.BountyCount;
            run.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            ApplyAutomaticModifierScoring(run, resolvedByUserId, now);
            run.FinalScore = ComputeFinalScore(run, normalizedStatus);

            var activeGameModifiers = await _dbContext.GameActiveModifiers
                .Where(x => x.GameId == run.GameId && x.ArchivedAtUtc == null)
                .ToArrayAsync(cancellationToken);
            foreach (var modifier in activeGameModifiers)
            {
                modifier.ArchivedAtUtc = now;
            }

            var game = await _dbContext.Games.FirstAsync(x => x.Id == run.GameId, cancellationToken);
            game.ActiveTeamId = null;

            var board = await _dbContext.GameBoards.FirstAsync(x => x.GameId == run.GameId, cancellationToken);
            board.Version += 1;

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
                        x.KillsCount,
                        x.BountyCount,
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
                        x.ModifierDescriptionSnapshot,
                        x.ModifierScoringTypeSnapshot,
                        DeserializeEffectSnapshot(x.ModifierEffectSnapshotJson),
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
            run.KillsCount,
            run.BountyCount,
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
            .Where(x => x.GameId == gameId && x.ArchivedAtUtc == null)
            .Select(
                x =>
                    new
                    {
                        x.Id,
                        x.ModifierId,
                        x.ModifierDefinition.Name,
                        x.ModifierDefinition.Description,
                        x.ModifierDefinition.Category,
                        x.ModifierDefinition.ScoringType,
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
                        ModifierDescriptionSnapshot = x.Description,
                        ModifierScoringTypeSnapshot = x.ScoringType,
                        ModifierEffectSnapshotJson = SerializeEffectSnapshot(
                            ResolveEffectSnapshot(x.ScoringType, x.MetadataJson)
                        ),
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

        var modifierKillDelta = run.ModifierResults.Sum(x => x.KillDelta);
        var modifierScoreDelta = run.ModifierResults.Sum(x => x.ScoreDelta);
        var baseActionsCount = run.KillsCount + run.BountyCount + modifierKillDelta;

        return (baseActionsCount * run.BaseScore) + modifierScoreDelta;
    }

    private static void ApplyAutomaticModifierScoring(
        GameCardRun run,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc
    )
    {
        foreach (var modifierGroup in run.ModifierResults.GroupBy(x => x.ModifierId))
        {
            var modifiers = modifierGroup.ToList();
            var modifier = modifiers[0];
            var effect = DeserializeEffectSnapshot(modifier.ModifierEffectSnapshotJson);
            var scoreImpact = effect?.ScoreImpact;
            if (!IsAutomaticScoreImpact(scoreImpact))
            {
                continue;
            }

            var formula = scoreImpact!.ScoreFormula
                ?? new GameModifierScoreFormula(
                    GameModifierScoreFormulaModes.FlatPerKill,
                    null,
                    null
                );

            if (formula.Mode == GameModifierScoreFormulaModes.CustomExpression)
            {
                ApplyAggregateAutomaticModifierScoring(
                    modifiers,
                    run,
                    resolvedByUserId,
                    resolvedAtUtc,
                    scoreImpact,
                    formula
                );
                continue;
            }

            foreach (var activation in modifiers)
            {
                ApplySingleAutomaticModifierScoring(
                    activation,
                    run,
                    resolvedByUserId,
                    resolvedAtUtc,
                    scoreImpact,
                    formula
                );
            }
        }
    }

    private static void ApplySingleAutomaticModifierScoring(
        GameCardRunModifierResult modifier,
        GameCardRun run,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        GameModifierScoreImpact scoreImpact,
        GameModifierScoreFormula formula
    )
    {
        var context = CreateFormulaContext(run, scoreImpact, activationCount: 1);

        if (run.KillsCount > 0 && (scoreImpact.PerKillBonus ?? 0) > 0)
        {
            CompleteAutomaticModifier(
                modifier,
                GameCardRunModifierOutcomeValue.Completed,
                RoundScore(GameModifierScoreFormulaSyntaxValidator.EvaluateSuccess(formula, context)),
                resolvedByUserId,
                resolvedAtUtc,
                SerializeAutomaticResolution("round_kills", run, scoreImpact, formula, "success", 1)
            );
            return;
        }

        if (run.KillsCount == 0 && (scoreImpact.FailurePenaltyPoints ?? 0) > 0)
        {
            CompleteAutomaticModifier(
                modifier,
                GameCardRunModifierOutcomeValue.Failed,
                -1 * (scoreImpact.FailurePenaltyPoints ?? 0),
                resolvedByUserId,
                resolvedAtUtc,
                SerializeAutomaticResolution("round_kills", run, scoreImpact, formula, "failure", 1)
            );
            return;
        }

        CompleteAutomaticModifier(
            modifier,
            GameCardRunModifierOutcomeValue.Cancelled,
            0,
            resolvedByUserId,
            resolvedAtUtc,
            SerializeAutomaticResolution("round_kills", run, scoreImpact, formula, "none", 1)
        );
    }

    private static void ApplyAggregateAutomaticModifierScoring(
        IReadOnlyList<GameCardRunModifierResult> modifiers,
        GameCardRun run,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        GameModifierScoreImpact scoreImpact,
        GameModifierScoreFormula formula
    )
    {
        var activationCount = modifiers.Count;
        var context = CreateFormulaContext(run, scoreImpact, activationCount);
        var outcomeStatus = GameCardRunModifierOutcomeValue.Cancelled;
        var totalScoreDelta = 0;
        var effect = "none";

        if (run.KillsCount > 0)
        {
            outcomeStatus = GameCardRunModifierOutcomeValue.Completed;
            totalScoreDelta = RoundScore(
                GameModifierScoreFormulaSyntaxValidator.EvaluateSuccess(formula, context)
            );
            effect = "success";
        }
        else if ((scoreImpact.FailurePenaltyPoints ?? 0) > 0
            || !string.IsNullOrWhiteSpace(formula.FailureExpression))
        {
            outcomeStatus = GameCardRunModifierOutcomeValue.Failed;
            totalScoreDelta = RoundScore(
                GameModifierScoreFormulaSyntaxValidator.EvaluateFailure(formula, context)
                ?? -1 * (scoreImpact.FailurePenaltyPoints ?? 0)
            );
            effect = "failure";
        }

        var shares = SplitScoreAcrossActivations(totalScoreDelta, activationCount);
        for (var i = 0; i < modifiers.Count; i += 1)
        {
            CompleteAutomaticModifier(
                modifiers[i],
                outcomeStatus,
                shares[i],
                resolvedByUserId,
                resolvedAtUtc,
                SerializeAutomaticResolution(
                    "round_kills",
                    run,
                    scoreImpact,
                    formula,
                    effect,
                    activationCount
                )
            );
        }
    }

    private static GameModifierScoreFormulaSyntaxValidator.ModifierScoreFormulaContext CreateFormulaContext(
        GameCardRun run,
        GameModifierScoreImpact scoreImpact,
        int activationCount
    ) =>
        new(
            run.KillsCount,
            run.BountyCount,
            run.BaseScore,
            run.BaseScore,
            scoreImpact.PerKillBonus ?? 0,
            scoreImpact.FailurePenaltyPoints ?? 0,
            activationCount,
            run.KillsCount + run.BountyCount
        );

    private static IReadOnlyList<int> SplitScoreAcrossActivations(int totalScore, int activationCount)
    {
        var shares = Enumerable.Repeat(totalScore / activationCount, activationCount).ToArray();
        var remainder = totalScore - shares.Sum();
        for (var i = 0; remainder != 0 && i < shares.Length; i += 1)
        {
            var delta = Math.Sign(remainder);
            shares[i] += delta;
            remainder -= delta;
        }

        return shares;
    }

    private static void CompleteAutomaticModifier(
        GameCardRunModifierResult modifier,
        string outcomeStatus,
        int scoreDelta,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        string resolutionDataJson
    )
    {
        modifier.OutcomeStatus = outcomeStatus;
        modifier.ScoreDelta = scoreDelta;
        modifier.KillDelta = 0;
        modifier.MultiplierApplied = null;
        modifier.ResolutionDataJson = resolutionDataJson;
        modifier.ResolvedByUserId = resolvedByUserId;
        modifier.ResolvedAtUtc = resolvedAtUtc;
        modifier.UpdatedAtUtc = resolvedAtUtc;
    }

    private static bool IsAutomaticScoreImpact(GameModifierScoreImpact? scoreImpact)
    {
        return scoreImpact is not null
            && (scoreImpact.ScoreFormula is not null
                || scoreImpact.PerKillBonus.HasValue
                || scoreImpact.FailurePenaltyPoints.HasValue);
    }

    private static int RoundScore(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (rounded < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)rounded;
    }

    private static string SerializeAutomaticResolution(
        string source,
        GameCardRun run,
        GameModifierScoreImpact scoreImpact,
        GameModifierScoreFormula formula,
        string effect,
        int activationCount
    )
    {
        return JsonSerializer.Serialize(
            new
            {
                source,
                effect,
                run.KillsCount,
                run.BountyCount,
                ActivationCount = activationCount,
                PerKillBonus = scoreImpact.PerKillBonus,
                FailurePenaltyPoints = scoreImpact.FailurePenaltyPoints,
                AutoResultFormula = formula.Mode,
                AutoResultSuccessExpression = formula.SuccessExpression,
                AutoResultFailureExpression = formula.FailureExpression
            },
            JsonOptions
        );
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

    private static string? SerializeEffectSnapshot(GameModifierEffect effect)
    {
        return JsonSerializer.Serialize(effect, JsonOptions);
    }

    private static GameModifierEffect? DeserializeEffectSnapshot(string? effectJson)
    {
        if (string.IsNullOrWhiteSpace(effectJson))
        {
            return null;
        }

        try
        {
            var effect = JsonSerializer.Deserialize<GameModifierEffect>(effectJson, JsonOptions);
            return effect is null ? null : NormalizeEffectSnapshot(effect);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static GameModifierEffect ResolveEffectSnapshot(string scoringType, string? metadataJson)
    {
        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<ModifierMetadata>(metadataJson, JsonOptions);
                if (metadata?.Effect is not null)
                {
                    return NormalizeEffectSnapshot(metadata.Effect);
                }
            }
            catch (JsonException)
            {
                // Some legacy metadata payloads use older shapes. Fall back to a safe translation.
            }
        }

        return scoringType switch
        {
            GameModifierScoringTypes.Multiplier => new GameModifierEffect(
                GameModifierMechanicTypes.Multiplier,
                ["requires_manual_resolution"],
                null,
                null,
                null,
                [],
                ["killsDuringWindow"],
                null,
                new GameModifierMultiplierEffect(
                    "kills",
                    TryReadDecimal(metadataJson, "killMultiplierDelta"),
                    "until_condition",
                    "health_restored"
                ),
                null
            ),
            GameModifierScoringTypes.ConditionalBonusPenalty => new GameModifierEffect(
                GameModifierMechanicTypes.RestrictionWithReward,
                ["requires_manual_resolution"],
                null,
                null,
                new GameModifierScoreImpact(
                    null,
                    TryReadInt(metadataJson, "bonusPerKill"),
                    TryReadInt(metadataJson, "missionFailurePenalty"),
                    null,
                    null,
                    null
                ),
                [new GameModifierCondition("at_least_one_kill", "manual_input")],
                ["kills"],
                null,
                null,
                null
            ),
            GameModifierScoringTypes.ConditionalBonus => new GameModifierEffect(
                GameModifierMechanicTypes.KillCounter,
                ["requires_manual_resolution"],
                null,
                null,
                new GameModifierScoreImpact(
                    null,
                    null,
                    null,
                    null,
                    TryReadInt(metadataJson, "bonusKills"),
                    null
                ),
                [],
                ["kills"],
                new GameModifierKillEffect(
                    "conditional_bonus_kill",
                    TryReadInt(metadataJson, "bonusKills") ?? 1,
                    null,
                    []
                ),
                null,
                null
            ),
            _ => new GameModifierEffect(
                GameModifierMechanicTypes.RuleOnly,
                [],
                null,
                null,
                null,
                [],
                [],
                null,
                null,
                null
            )
        };
    }

    private static GameModifierEffect NormalizeEffectSnapshot(GameModifierEffect effect)
    {
        return new GameModifierEffect(
            string.IsNullOrWhiteSpace(effect.MechanicType)
                ? GameModifierMechanicTypes.RuleOnly
                : effect.MechanicType,
            effect.Traits ?? [],
            effect.DurationSeconds,
            effect.RuleText,
            effect.ScoreImpact,
            effect.Conditions ?? [],
            effect.ResolutionInputs ?? [],
            effect.KillEffect is null
                ? null
                : effect.KillEffect with
                {
                    ExcludedWeapons = effect.KillEffect.ExcludedWeapons ?? []
                },
            effect.MultiplierEffect,
            effect.MentorEffect
        );
    }

    private static int? TryReadInt(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement.TryGetProperty(propertyName, out var value)
            && value.TryGetInt32(out var parsed)
            ? parsed
            : null;
    }

    private static decimal? TryReadDecimal(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement.TryGetProperty(propertyName, out var value)
            && value.TryGetDecimal(out var parsed)
            ? parsed
            : null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlySet<string> AllowedModifierOutcomeStatuses = new HashSet<string>(
        [
            GameCardRunModifierOutcomeValue.Completed,
            GameCardRunModifierOutcomeValue.Failed,
            GameCardRunModifierOutcomeValue.Cancelled
        ],
        StringComparer.Ordinal
    );

    private sealed record ModifierMetadata(
        GameModifierEffect Effect,
        GameModifierActivationLimit? ActivationLimit
    );

    private static bool IsActiveRoundStatus(string status)
    {
        return status == GameCardRunStatusValue.AwaitingModifiers
            || status == GameCardRunStatusValue.InProgress
            || status == GameCardRunStatusValue.ReviewingResults;
    }
}
