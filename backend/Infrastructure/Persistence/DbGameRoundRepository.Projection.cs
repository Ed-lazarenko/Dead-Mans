using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
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

}
