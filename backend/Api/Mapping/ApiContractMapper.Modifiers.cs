using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Api.Mapping;

public static partial class ApiContractMapper
{
    public static CreateGameModifierInput ToInput(this CreateGameModifierRequestDto request)
    {
        return new CreateGameModifierInput(
            request.Name,
            request.Description,
            request.Category,
            request.ActivationCost,
            request.ActivationLimit.ToModel(),
            (request.ConflictingModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            request.IconEmoji,
            request.ActivationCommand,
            request.NormalizedTags,
            request.BehaviorV2.ToModel(),
            request.ChangeNote
        );
    }

    public static UpdateGameModifierInput ToInput(this UpdateGameModifierRequestDto request)
    {
        return new UpdateGameModifierInput(
            request.Name,
            request.Description,
            request.Category,
            request.ActivationCost,
            request.ActivationLimit.ToModel(),
            (request.ConflictingModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            request.IconEmoji,
            request.ActivationCommand,
            request.NormalizedTags,
            request.BehaviorV2.ToModel(),
            request.ExpectedRevision,
            request.ChangeNote
        );
    }

    public static ModifierHistorySummaryDto ToDto(this ModifierHistorySummary item) => new(
        item.ModifierId.ToString(), item.CurrentRevision, item.Name, item.Category, item.IconEmoji,
        item.ActivationCost, item.IsArchived, item.CreatedAtUtc, item.ArchivedAtUtc,
        item.VersionCount, item.GamesCount, item.ActivationsCount);

    public static ModifierVersionSummaryDto ToDto(this ModifierVersionSummary item) => new(
        item.VersionId.ToString(), item.ModifierId.ToString(), item.Revision, item.Name,
        item.CreatedAtUtc, item.CreatedByUserId?.ToString(), item.CreatedByDisplayName,
        item.ChangeNote, item.ChangeType, item.CascadeSourceModifierId?.ToString(), item.ChangedFields);

    public static ModifierVersionDetailDto ToDto(this ModifierVersionDetail item) => new(
        item.VersionId.ToString(), item.ModifierId.ToString(), item.Revision, item.Name,
        item.Description, item.Category, item.IconEmoji, item.ActivationCommand,
        item.ActivationCost, item.ActivationLimit.ToDto(), item.NormalizedTags,
        item.BehaviorV2.ToDto(), item.Conflicts.Select(x => new ModifierConflictSnapshotDto(
            x.ModifierId.ToString(), x.Name)).ToArray(), item.CreatedAtUtc,
        item.CreatedByUserId?.ToString(), item.CreatedByDisplayName, item.ChangeNote,
        item.ChangeType, item.CascadeSourceModifierId?.ToString(), item.ChangedFields,
        item.IsCurrent, item.IsArchived);

    public static ModifierVersionGameSummaryDto ToDto(this ModifierVersionGameSummary item) => new(
        item.GameId.ToString(), item.GameTitle, item.GameStatus, item.StartedAtUtc,
        item.FinishedAtUtc, item.SuccessfulActivationsCount, item.CancelledActivationsCount,
        item.ResultsCount, item.IsEmergencyDisabled);

    public static GameModifierDraftPreviewDto ToDto(this GameModifierDraftPreview preview) => new(
        preview.Name,
        preview.Description,
        preview.IconEmoji,
        preview.ActivationCommand,
        preview.NormalizedTags.ToArray(),
        preview.BehaviorV2.ToDto(),
        new GameModifierDraftExampleDto(
            preview.Example.CardValue,
            preview.Example.KillsCount,
            preview.Example.BountyCount,
            preview.Example.ResolutionExample,
            preview.Example.PointsDelta,
            preview.Example.BonusKillsDelta,
            preview.Example.FinalScore
        )
    );

    public static GameModifierDefinitionDto ToDto(this GameModifierDefinition definition)
    {
        return new GameModifierDefinitionDto(
            definition.Id.ToString(),
            definition.Category,
            definition.Name,
            definition.Description,
            definition.ActivationCost,
            definition.ActivationLimit.ToDto(),
            definition.ConflictingModifierIds.Select(id => id.ToString()).ToArray(),
            definition.IconEmoji,
            definition.ActivationCommand,
            definition.IsLockedByActiveGame,
            definition.Revision,
            definition.NormalizedTags.ToArray(),
            definition.BehaviorV2.ToDto()
        );
    }

    private static ModifierBehaviorV2 ToModel(this GameModifierBehaviorV2Dto dto) => new(
        dto.SchemaVersion,
        dto.Kind switch
        {
            "rule" => ModifierBehaviorKind.Rule,
            "scoring" => ModifierBehaviorKind.Scoring,
            _ => (ModifierBehaviorKind)(-1)
        },
        dto.Phase switch
        {
            "preparation" => ModifierPhase.Preparation,
            "round" => ModifierPhase.Round,
            "result" => ModifierPhase.Result,
            _ => (ModifierPhase)(-1)
        },
        dto.Performer switch
        {
            "activeTeam" => ModifierPerformer.ActiveTeam,
            "mentor" => ModifierPerformer.Mentor,
            _ => (ModifierPerformer)(-1)
        },
        dto.RequiresHostMonitoring,
        dto.Rule,
        dto.StackingPolicy switch
        {
            "aggregateParameters" => ModifierStackingPolicy.AggregateParameters,
            "independentInstances" => ModifierStackingPolicy.IndependentInstances,
            _ => (ModifierStackingPolicy)(-1)
        },
        dto.Resolution.ToModel(),
        dto.Reward switch
        {
            "none" => ModifierRewardKind.None,
            "points" => ModifierRewardKind.Points,
            "bonusKills" => ModifierRewardKind.BonusKills,
            _ => (ModifierRewardKind)(-1)
        },
        dto.FormulaReference?.ToModel(),
        dto.DurationSecondsPerActivation
    );

    private static ModifierResolution ToModel(this GameModifierResolutionDto dto) => dto switch
    {
        GameModifierRuleStatusResolutionDto => new RuleStatusResolution(),
        GameModifierBooleanResolutionDto value => new BooleanResolution(value.InputLabel),
        GameModifierNonNegativeCountResolutionDto value => new NonNegativeCountResolution(
            value.InputLabel,
            value.MaximumKind,
            value.MaximumPerActivation
        ),
        GameModifierAutomaticRoundMetricResolutionDto value =>
            new AutomaticRoundMetricResolution(value.Metric),
        GameModifierPerActivationResolutionDto => new PerActivationResolution(),
        _ => throw new ArgumentOutOfRangeException(nameof(dto))
    };

    private static ModifierFormulaReference ToModel(this GameModifierFormulaReferenceV2Dto dto) =>
        new(dto.Code, dto.Version, dto.Parameters.ToModel());

    private static ModifierFormulaParameters ToModel(this GameModifierFormulaParametersDto dto) =>
        dto switch
        {
            GameModifierGrowingKillValueParametersDto value =>
                new GrowingKillValueParameters(
                    value.IncrementPointsPerKill,
                    value.ZeroKillPenaltyPoints
                ),
            GameModifierBonusKillOnConditionParametersDto value =>
                new BonusKillOnConditionParameters(value.SuccessBonusKills),
            GameModifierBonusKillsByCountParametersDto value =>
                new BonusKillsByCountParameters(value.BonusKillsPerUnit),
            GameModifierWindowKillBonusPointsParametersDto value =>
                new WindowKillBonusPointsParameters(value.BonusRate),
            GameModifierFixedPointsPerUnitParametersDto value =>
                new FixedPointsPerUnitParameters(value.PointsPerUnit),
            GameModifierCardPercentPerUnitParametersDto value =>
                new CardPercentPerUnitParameters(value.Rate),
            GameModifierBonusKillsPerUnitParametersDto value =>
                new BonusKillsPerUnitParameters(value.BonusKillsPerUnit),
            GameModifierKillValueIncreasePerUnitParametersDto value =>
                new KillValueIncreasePerUnitParameters(
                    value.IncrementPointsPerUnit,
                    value.ZeroCountPenaltyPoints
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

    private static GameModifierBehaviorV2Dto ToDto(this ModifierBehaviorV2 behavior) => new(
        behavior.SchemaVersion,
        behavior.Kind == ModifierBehaviorKind.Rule ? "rule" : "scoring",
        behavior.Phase switch
        {
            ModifierPhase.Preparation => "preparation",
            ModifierPhase.Round => "round",
            ModifierPhase.Result => "result",
            _ => throw new ArgumentOutOfRangeException(nameof(behavior))
        },
        behavior.Performer == ModifierPerformer.ActiveTeam ? "activeTeam" : "mentor",
        behavior.RequiresHostMonitoring,
        behavior.Rule,
        behavior.StackingPolicy == ModifierStackingPolicy.AggregateParameters
            ? "aggregateParameters"
            : "independentInstances",
        behavior.Resolution.ToDto(),
        behavior.Reward switch
        {
            ModifierRewardKind.None => "none",
            ModifierRewardKind.Points => "points",
            ModifierRewardKind.BonusKills => "bonusKills",
            _ => throw new ArgumentOutOfRangeException(nameof(behavior))
        },
        behavior.FormulaReference?.ToDto(),
        behavior.DurationSecondsPerActivation
    );

    private static GameModifierResolutionDto ToDto(this ModifierResolution resolution) =>
        resolution switch
        {
            RuleStatusResolution => new GameModifierRuleStatusResolutionDto(),
            BooleanResolution value => new GameModifierBooleanResolutionDto(value.InputLabel),
            NonNegativeCountResolution value => new GameModifierNonNegativeCountResolutionDto(
                value.InputLabel,
                value.MaximumKind,
                value.MaximumPerActivation
            ),
            AutomaticRoundMetricResolution value =>
                new GameModifierAutomaticRoundMetricResolutionDto(value.Metric),
            PerActivationResolution => new GameModifierPerActivationResolutionDto(),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution))
        };

    private static GameModifierFormulaReferenceV2Dto ToDto(
        this ModifierFormulaReference reference
    ) => new(reference.Code, reference.Version, reference.Parameters.ToDto());

    private static GameModifierFormulaParametersDto ToDto(
        this ModifierFormulaParameters parameters
    ) => parameters switch
    {
        GrowingKillValueParameters value => new GameModifierGrowingKillValueParametersDto(
            value.IncrementPointsPerKill,
            value.ZeroKillPenaltyPoints
        ),
        BonusKillOnConditionParameters value =>
            new GameModifierBonusKillOnConditionParametersDto(value.SuccessBonusKills),
        BonusKillsByCountParameters value =>
            new GameModifierBonusKillsByCountParametersDto(value.BonusKillsPerUnit),
        WindowKillBonusPointsParameters value =>
            new GameModifierWindowKillBonusPointsParametersDto(value.BonusRate),
        FixedPointsPerUnitParameters value =>
            new GameModifierFixedPointsPerUnitParametersDto(value.PointsPerUnit),
        CardPercentPerUnitParameters value =>
            new GameModifierCardPercentPerUnitParametersDto(value.Rate),
        BonusKillsPerUnitParameters value =>
            new GameModifierBonusKillsPerUnitParametersDto(value.BonusKillsPerUnit),
        KillValueIncreasePerUnitParameters value =>
            new GameModifierKillValueIncreasePerUnitParametersDto(
                value.IncrementPointsPerUnit,
                value.ZeroCountPenaltyPoints
            ),
        _ => throw new ArgumentOutOfRangeException(nameof(parameters))
    };

    private static GameModifierActivationLimit ToModel(this GameModifierActivationLimitDto? dto)
    {
        if (dto is null)
        {
            return new GameModifierActivationLimit(null);
        }

        return new GameModifierActivationLimit(dto.Count);
    }

    private static GameModifierActivationLimitDto ToDto(this GameModifierActivationLimit model)
    {
        return new GameModifierActivationLimitDto(model.Count);
    }

    public static GameModifierActivationDto ToDto(this GameModifierActivation activation)
    {
        return new GameModifierActivationDto(
            activation.ActivationId.ToString(),
            activation.RoundId.ToString(),
            activation.RoundVersion,
            activation.ModifierId.ToString(),
            activation.ModifierName,
            activation.ActivatedByUserId,
            activation.ActivatedByDisplayName,
            activation.ActivationCost,
            activation.ActivatedAtUtc
        );
    }

    public static GameModifierAvailabilityDto ToDto(this GameModifierAvailability availability)
    {
        return new GameModifierAvailabilityDto(
            availability.Modifier.ToDto(),
            availability.IsActive,
            availability.CanActivate,
            availability.BlockedReason,
            availability.ActivationsCount,
            availability.Limit,
            availability.IsEmergencyDisabled,
            availability.EmergencyDisabledAtUtc
        );
    }

    public static GameModifierStateDto ToDto(this GameModifierState state)
    {
        return new GameModifierStateDto(
            state.GameId.ToString(),
            state.AvailableQuizPoints,
            state.EarnedQuizPoints,
            state.SpentQuizPoints,
            state.IsOrderingOpen,
            state.ActiveModifiers.Select(x => x.ToDto()).ToArray(),
            state.AvailableModifiers.Select(x => x.ToDto()).ToArray()
        );
    }

    public static GameModifierAdminPlayerDto ToDto(this GameModifierAdminPlayer player)
    {
        return new GameModifierAdminPlayerDto(
            player.UserId.ToString(),
            player.Login,
            player.DisplayName,
            player.AvailableQuizPoints,
            player.EarnedQuizPoints,
            player.SpentQuizPoints
        );
    }

    public static GameModifierAdminPlayersSummaryDto ToDto(this GameModifierAdminPlayersSummary summary)
    {
        return new GameModifierAdminPlayersSummaryDto(
            summary.PlayersCount,
            summary.TotalAvailableQuizPoints,
            summary.TotalEarnedQuizPoints,
            summary.TotalSpentQuizPoints
        );
    }

    public static GameModifierAdminPlayersResultDto ToDto(this GameModifierAdminPlayersResult result)
    {
        return new GameModifierAdminPlayersResultDto(
            result.Summary.ToDto(),
            result.Players.Select(x => x.ToDto()).ToArray()
        );
    }

    public static GameModifierActivatedEventDto ToDto(this GameModifierActivatedEvent @event)
    {
        return new GameModifierActivatedEventDto(@event.GameId, @event.Version, @event.Activation.ToDto());
    }

    public static GameModifierActivationCancelledEventDto ToDto(
        this GameModifierActivationCancelledEvent @event
    )
    {
        return new GameModifierActivationCancelledEventDto(
            @event.GameId,
            @event.Version,
            @event.ActivationId.ToString()
        );
    }

    public static GameModifierAvailabilityChangedEventDto ToDto(
        this GameModifierAvailabilityChangedEvent @event
    )
    {
        return new GameModifierAvailabilityChangedEventDto(
            @event.GameId,
            @event.Version,
            @event.ModifierId.ToString()
        );
    }

    public static GameUserNotificationCreatedEventDto ToDto(
        this GameUserNotificationCreatedEvent @event
    )
    {
        return new GameUserNotificationCreatedEventDto(@event.Notification.ToDto());
    }

}
