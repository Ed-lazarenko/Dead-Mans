using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Messaging;

namespace backend.Application.Features.GameModifiers;

public sealed partial class GameModifierService
{
    public async Task<EmergencyDisableGameModifierResult> EmergencyDisableAsync(
        Guid modifierId,
        Guid? disabledByUserId,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedReason = reason?.Trim();
        if (disabledByUserId is null)
        {
            return new EmergencyDisableGameModifierResult(
                EmergencyDisableGameModifierOutcome.UserNotResolved
            );
        }

        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 1000)
        {
            return new EmergencyDisableGameModifierResult(
                EmergencyDisableGameModifierOutcome.InvalidRequest
            );
        }

        var repositoryResult = await _repository.EmergencyDisableModifierAsync(
            new EmergencyDisableGameModifierInput(
                modifierId,
                disabledByUserId.Value,
                normalizedReason
            ),
            cancellationToken
        );
        var result = repositoryResult.Status switch
        {
            EmergencyDisableGameModifierRepositoryStatus.Disabled
                when repositoryResult.GameId is not null
                    && repositoryResult.Version.HasValue
                    && repositoryResult.ModifierId.HasValue =>
                new EmergencyDisableGameModifierResult(
                    EmergencyDisableGameModifierOutcome.Disabled,
                    new GameModifierAvailabilityChangedEvent(
                        repositoryResult.GameId,
                        repositoryResult.Version.Value,
                        repositoryResult.ModifierId.Value
                    )
                ),
            EmergencyDisableGameModifierRepositoryStatus.AlreadyDisabled =>
                new EmergencyDisableGameModifierResult(
                    EmergencyDisableGameModifierOutcome.AlreadyDisabled
                ),
            EmergencyDisableGameModifierRepositoryStatus.GameNotActive =>
                new EmergencyDisableGameModifierResult(
                    EmergencyDisableGameModifierOutcome.GameNotActive
                ),
            _ => new EmergencyDisableGameModifierResult(
                EmergencyDisableGameModifierOutcome.ModifierNotEnabled
            )
        };

        if (result.Outcome != EmergencyDisableGameModifierOutcome.Disabled || result.Event is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishModifierAvailabilityChangedAsync(
                result.Event,
                publishToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameModifierAvailabilityChangedPublishFailed,
            result.Event.ModifierId
        );

        return result;
    }

    public async Task<ActivateGameModifierResult> ActivateAsync(
        Guid modifierId,
        Guid? activatedByUserId,
        Guid? initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await _repository.ModifierIdExistsAsync(modifierId, cancellationToken))
        {
            return new ActivateGameModifierResult(ActivateGameModifierOutcome.NotFound);
        }

        if (activatedByUserId is null || initiatedByUserId is null)
        {
            return new ActivateGameModifierResult(ActivateGameModifierOutcome.UserNotResolved);
        }

        var activationResult = await _repository.ActivateModifierAsync(
            modifierId,
            activatedByUserId.Value,
            initiatedByUserId.Value,
            cancellationToken
        );

        var result = activationResult.Status switch
        {
            ActivateGameModifierRepositoryStatus.Activated
                when activationResult.GameId is not null
                    && activationResult.Version.HasValue
                    && activationResult.Activation is not null =>
                new ActivateGameModifierResult(
                    ActivateGameModifierOutcome.Activated,
                    new GameModifierActivatedEvent(
                        activationResult.GameId,
                        activationResult.Version.Value,
                        activationResult.Activation
                    )
                ),
            ActivateGameModifierRepositoryStatus.NotFound =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.NotFound),
            ActivateGameModifierRepositoryStatus.GameNotActive => new ActivateGameModifierResult(
                ActivateGameModifierOutcome.GameNotActive
            ),
            ActivateGameModifierRepositoryStatus.ModifierNotEnabled => new ActivateGameModifierResult(
                ActivateGameModifierOutcome.ModifierNotEnabled
            ),
            ActivateGameModifierRepositoryStatus.ModifierConflictActive =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.ModifierConflictActive),
            ActivateGameModifierRepositoryStatus.ModifierLimitReached => new ActivateGameModifierResult(
                ActivateGameModifierOutcome.ModifierLimitReached
            ),
            ActivateGameModifierRepositoryStatus.ModifierOrderingClosed =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.ModifierOrderingClosed),
            ActivateGameModifierRepositoryStatus.ActiveTeamMember =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.ActiveTeamMember),
            ActivateGameModifierRepositoryStatus.InsufficientQuizPoints =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.InsufficientQuizPoints),
            ActivateGameModifierRepositoryStatus.EmergencyDisabled =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.EmergencyDisabled),
            ActivateGameModifierRepositoryStatus.VersionBindingMissing =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.VersionBindingMissing),
            _ => new ActivateGameModifierResult(ActivateGameModifierOutcome.GameNotActive)
        };

        if (result.Outcome != ActivateGameModifierOutcome.Activated || result.Event is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishModifierActivatedAsync(result.Event, publishToken),
            _logger,
            AppMessages.Logs.RealtimeGameModifierActivatedPublishFailed,
            result.Event.Activation.ModifierId
        );

        return result;
    }

    public async Task<CancelGameModifierActivationResult> CancelActivationAsync(
        Guid activationId,
        Guid? cancelledByUserId,
        int expectedRoundVersion,
        bool isAdmin,
        string? reason = null,
        string? cancelledByDisplayName = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!cancelledByUserId.HasValue)
        {
            return new CancelGameModifierActivationResult(
                CancelGameModifierActivationOutcome.UserNotResolved
            );
        }

        var cancellationResult = await _repository.CancelActivationAsync(
            new CancelGameModifierActivationRepositoryInput(
                activationId,
                cancelledByUserId.Value,
                expectedRoundVersion,
                isAdmin,
                reason
            ),
            cancellationToken
        );

        var result = cancellationResult.Status switch
        {
            CancelGameModifierActivationRepositoryStatus.Cancelled
                when cancellationResult.GameId is not null
                    && cancellationResult.Version.HasValue
                    && cancellationResult.ActivationId.HasValue
                    && cancellationResult.StateChanged =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.Cancelled,
                    new GameModifierActivationCancelledEvent(
                        cancellationResult.GameId,
                        cancellationResult.Version.Value,
                        cancellationResult.ActivationId.Value
                    )
                ),
            CancelGameModifierActivationRepositoryStatus.Cancelled =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.Cancelled
                ),
            CancelGameModifierActivationRepositoryStatus.GameNotActive =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.GameNotActive
                ),
            CancelGameModifierActivationRepositoryStatus.Forbidden =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.Forbidden
                ),
            CancelGameModifierActivationRepositoryStatus.InvalidRoundState =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.InvalidRoundState
                ),
            CancelGameModifierActivationRepositoryStatus.StaleVersion =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.StaleVersion
                ),
            CancelGameModifierActivationRepositoryStatus.ReasonRequired =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.ReasonRequired
                ),
            _ => new CancelGameModifierActivationResult(
                CancelGameModifierActivationOutcome.ActivationNotFound
            )
        };

        if (result.Outcome != CancelGameModifierActivationOutcome.Cancelled)
        {
            return result;
        }

        if (result.Event is not null)
        {
            await RealtimePublishGuard.TryPublishAsync(
                publishToken => _eventsPublisher.PublishModifierActivationCancelledAsync(
                    result.Event,
                    publishToken
                ),
                _logger,
                AppMessages.Logs.RealtimeGameModifierCancelledPublishFailed,
                result.Event.ActivationId
            );
        }

        if (cancellationResult.StateChanged
            && cancellationResult.ActivatedByUserId.HasValue
            && cancellationResult.ActivationId.HasValue
            && Guid.TryParse(cancellationResult.GameId, out var notificationGameId)
            && !string.IsNullOrWhiteSpace(cancellationResult.ModifierName)
            && cancellationResult.RefundedQuizPoints.HasValue)
        {
            await _notificationService.NotifyModifierCancelledAsync(
                cancellationResult.ActivatedByUserId.Value,
                notificationGameId,
                cancellationResult.ActivationId.Value,
                cancellationResult.ModifierName,
                string.IsNullOrWhiteSpace(cancelledByDisplayName)
                    ? "Administrator"
                    : cancelledByDisplayName,
                cancellationResult.RefundedQuizPoints.Value,
                cancellationToken
            );
        }

        return result;
    }
}
