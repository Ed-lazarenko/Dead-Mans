using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameModifiers;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameRoundRepository : IGameRoundRepository
{
    private static readonly string[] ActiveRoundStatuses =
    [
        GameRoundStatusValue.AwaitingModifiers,
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
            awaitingRound.UpdatedAtUtc = now;

            await AddModifierSnapshotsAsync(activeGameId.Value, awaitingRound.Id, now, cancellationToken);

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

    public async Task<ReviewGameRoundResult> ReviewAsync(
        Guid roundId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var round = await _dbContext.GameRounds.FirstOrDefaultAsync(
            x => x.Id == roundId,
            cancellationToken
        );
        if (round is null)
        {
            return new ReviewGameRoundResult(ReviewGameRoundOutcome.NotFound, null);
        }

        if (round.Status != GameRoundStatusValue.InProgress)
        {
            return new ReviewGameRoundResult(ReviewGameRoundOutcome.NotInProgress, null);
        }

        round.Status = GameRoundStatusValue.ReviewingResults;
        round.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReviewGameRoundResult(
            ReviewGameRoundOutcome.Reviewed,
            await LoadRoundDetailsAsync(roundId, cancellationToken)
        );
    }

    public async Task<FinalizeGameRoundResult> FinalizeAsync(
        Guid roundId,
        FinalizeGameRoundInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedStatus = input.Status.Trim().ToLowerInvariant();
        if (!FinalizeGameRoundResult.AllowedTerminalStatuses.Contains(normalizedStatus))
        {
            return new FinalizeGameRoundResult(FinalizeGameRoundOutcome.InvalidStatus, null);
        }

        var now = DateTime.UtcNow;
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await using (transaction)
        {
            var round = await _dbContext.GameRounds
                .Include(x => x.ModifierResults)
                .FirstOrDefaultAsync(x => x.Id == roundId, cancellationToken);
            if (round is null)
            {
                return new FinalizeGameRoundResult(FinalizeGameRoundOutcome.NotFound, null);
            }

            if (round.Status != GameRoundStatusValue.ReviewingResults)
            {
                return new FinalizeGameRoundResult(
                    FinalizeGameRoundOutcome.NotInProgress,
                    null
                );
            }

            var modifierInputsById = input.ModifierResults.ToDictionary(x => x.ModifierResultId);
            foreach (var modifierResultId in modifierInputsById.Keys)
            {
                if (round.ModifierResults.All(x => x.Id != modifierResultId))
                {
                    return new FinalizeGameRoundResult(
                        FinalizeGameRoundOutcome.ModifierResultNotFound,
                        null
                    );
                }
            }

            foreach (var modifier in round.ModifierResults)
            {
                if (modifierInputsById.TryGetValue(modifier.Id, out var update))
                {
                    var normalizedOutcomeStatus = update.OutcomeStatus.Trim().ToLowerInvariant();
                    if (!AllowedModifierOutcomeStatuses.Contains(normalizedOutcomeStatus))
                    {
                        return new FinalizeGameRoundResult(
                            FinalizeGameRoundOutcome.InvalidStatus,
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
                else if (modifier.OutcomeStatus == GameRoundModifierOutcomeValue.Pending)
                {
                    modifier.OutcomeStatus = GameRoundModifierOutcomeValue.Cancelled;
                    modifier.ResolvedByUserId = resolvedByUserId;
                    modifier.ResolvedAtUtc = now;
                    modifier.UpdatedAtUtc = now;
                }
            }

            round.Status = normalizedStatus;
            round.FinishedAtUtc = now;
            round.ResolvedByUserId = resolvedByUserId;
            round.UpdatedAtUtc = now;
            round.KillsCount = input.KillsCount;
            round.BountyCount = input.BountyCount;
            round.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            ApplyAutomaticModifierScoring(round, resolvedByUserId, now);
            round.FinalScore = ComputeFinalScore(round, normalizedStatus);

            var activeGameModifiers = await _dbContext.GameModifierActivations
                .Where(x => x.GameId == round.GameId && x.ArchivedAtUtc == null)
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
                        x.TeamId,
                        TeamName = x.Team.Name,
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

        var participants = await _dbContext.GameRoundParticipants
            .AsNoTracking()
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new GameRoundParticipantSnapshot(x.UserId, x.DisplayNameSnapshot))
            .ToArrayAsync(cancellationToken);

        var modifiers = await _dbContext.GameRoundModifierResults
            .AsNoTracking()
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(
                x =>
                    new GameRoundModifierSnapshot(
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

        return new GameRoundDetails(
            round.Id,
            round.GameId,
            round.CellId,
            round.TeamId,
            round.TeamName,
            round.TeamSlotIndex,
            round.Status,
            round.StartedAtUtc,
            round.FinishedAtUtc,
            round.BaseScore,
            round.FinalScore,
            round.KillsCount,
            round.BountyCount,
            round.Notes,
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

        _dbContext.GameRoundModifierResults.AddRange(
            activeModifiers.Select(
                x =>
                    new GameRoundModifierResult
                    {
                        Id = Guid.NewGuid(),
                        RoundId = roundId,
                        GameModifierActivationId = x.Id,
                        ModifierId = x.ModifierId,
                        ModifierNameSnapshot = x.Name,
                        ModifierCategorySnapshot = x.Category,
                        ModifierMechanicTypeSnapshot = ResolveMechanicType(x.MetadataJson),
                        ModifierDescriptionSnapshot = x.Description,
                        ModifierScoringTypeSnapshot = x.ScoringType,
                        ModifierEffectSnapshotJson = SerializeEffectSnapshot(
                            ResolveEffectSnapshot(x.ScoringType, x.MetadataJson)
                        ),
                        OutcomeStatus = GameRoundModifierOutcomeValue.Pending,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    }
            )
        );
    }

    private static int ComputeFinalScore(GameRound round, string normalizedStatus)
    {
        if (normalizedStatus == GameRoundStatusValue.Cancelled)
        {
            return 0;
        }

        var modifierKillDelta = round.ModifierResults.Sum(x => x.KillDelta);
        var modifierScoreDelta = round.ModifierResults.Sum(x => x.ScoreDelta);
        var baseActionsCount = round.KillsCount + round.BountyCount + modifierKillDelta;

        return (baseActionsCount * round.BaseScore) + modifierScoreDelta;
    }

    private static void ApplyAutomaticModifierScoring(
        GameRound round,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc
    )
    {
        foreach (var modifierGroup in round.ModifierResults.GroupBy(x => x.ModifierId))
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
                    round,
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
                    round,
                    resolvedByUserId,
                    resolvedAtUtc,
                    scoreImpact,
                    formula
                );
            }
        }
    }

    private static void ApplySingleAutomaticModifierScoring(
        GameRoundModifierResult modifier,
        GameRound round,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        GameModifierScoreImpact scoreImpact,
        GameModifierScoreFormula formula
    )
    {
        var context = CreateFormulaContext(round, scoreImpact, activationCount: 1);

        if (round.KillsCount > 0 && (scoreImpact.PerKillBonus ?? 0) > 0)
        {
            CompleteAutomaticModifier(
                modifier,
                GameRoundModifierOutcomeValue.Completed,
                RoundScore(GameModifierScoreFormulaSyntaxValidator.EvaluateSuccess(formula, context)),
                resolvedByUserId,
                resolvedAtUtc,
                SerializeAutomaticResolution("round_kills", round, scoreImpact, formula, "success", 1)
            );
            return;
        }

        if (round.KillsCount == 0 && (scoreImpact.FailurePenaltyPoints ?? 0) > 0)
        {
            CompleteAutomaticModifier(
                modifier,
                GameRoundModifierOutcomeValue.Failed,
                -1 * (scoreImpact.FailurePenaltyPoints ?? 0),
                resolvedByUserId,
                resolvedAtUtc,
                SerializeAutomaticResolution("round_kills", round, scoreImpact, formula, "failure", 1)
            );
            return;
        }

        CompleteAutomaticModifier(
            modifier,
            GameRoundModifierOutcomeValue.Cancelled,
            0,
            resolvedByUserId,
            resolvedAtUtc,
            SerializeAutomaticResolution("round_kills", round, scoreImpact, formula, "none", 1)
        );
    }

    private static void ApplyAggregateAutomaticModifierScoring(
        IReadOnlyList<GameRoundModifierResult> modifiers,
        GameRound round,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        GameModifierScoreImpact scoreImpact,
        GameModifierScoreFormula formula
    )
    {
        var activationCount = modifiers.Count;
        var context = CreateFormulaContext(round, scoreImpact, activationCount);
        var outcomeStatus = GameRoundModifierOutcomeValue.Cancelled;
        var totalScoreDelta = 0;
        var effect = "none";

        if (round.KillsCount > 0)
        {
            outcomeStatus = GameRoundModifierOutcomeValue.Completed;
            totalScoreDelta = RoundScore(
                GameModifierScoreFormulaSyntaxValidator.EvaluateSuccess(formula, context)
            );
            effect = "success";
        }
        else if ((scoreImpact.FailurePenaltyPoints ?? 0) > 0
            || !string.IsNullOrWhiteSpace(formula.FailureExpression))
        {
            outcomeStatus = GameRoundModifierOutcomeValue.Failed;
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
                    round,
                    scoreImpact,
                    formula,
                    effect,
                    activationCount
                )
            );
        }
    }

    private static GameModifierScoreFormulaSyntaxValidator.ModifierScoreFormulaContext CreateFormulaContext(
        GameRound round,
        GameModifierScoreImpact scoreImpact,
        int activationCount
    ) =>
        new(
            round.KillsCount,
            round.BountyCount,
            round.BaseScore,
            round.BaseScore,
            scoreImpact.PerKillBonus ?? 0,
            scoreImpact.FailurePenaltyPoints ?? 0,
            activationCount,
            round.KillsCount + round.BountyCount
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
        GameRoundModifierResult modifier,
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
        GameRound round,
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
                round.KillsCount,
                round.BountyCount,
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
            GameRoundModifierOutcomeValue.Completed,
            GameRoundModifierOutcomeValue.Failed,
            GameRoundModifierOutcomeValue.Cancelled
        ],
        StringComparer.Ordinal
    );

    private sealed record ModifierMetadata(
        GameModifierEffect Effect,
        GameModifierActivationLimit? ActivationLimit
    );

}
