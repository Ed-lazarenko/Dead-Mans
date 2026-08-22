using backend.Application.Contracts;
using backend.Application.Features.GameRounds;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
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

}
