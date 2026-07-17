using backend.Application.Contracts;
using backend.Api.Http;
using backend.Messaging;
using Microsoft.AspNetCore.Http;

namespace backend.Api.Errors;

public static class DomainErrorHttpPolicy
{
#pragma warning disable CS8524
    public static ApiErrorDescriptor FromRegistration(GameRegistrationErrorCode error) =>
        error switch
        {
            GameRegistrationErrorCode.None => new(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.GameRegistrationOperationFailed,
                AppMessages.ErrorCodes.GameRegistrationOperationFailed
            ),
            GameRegistrationErrorCode.GameNotInReady => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameRegistrationNotOpen,
                AppMessages.ErrorCodes.GameRegistrationNotOpen
            ),
            GameRegistrationErrorCode.NoAvailableSlot => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationNoSlots,
                AppMessages.ErrorCodes.GameRegistrationNoSlots
            ),
            GameRegistrationErrorCode.UserAlreadyOnTeam => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationAlreadyOnTeam,
                AppMessages.ErrorCodes.GameRegistrationAlreadyOnTeam
            ),
            GameRegistrationErrorCode.TeamNotFound => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameRegistrationTeamNotFound,
                AppMessages.ErrorCodes.GameRegistrationTeamNotFound
            ),
            GameRegistrationErrorCode.TeamNotJoinable
                or GameRegistrationErrorCode.TeamFull
                or GameRegistrationErrorCode.TargetTeamSameAsSource => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationTeamNotJoinable,
                AppMessages.ErrorCodes.GameRegistrationTeamNotJoinable
            ),
            GameRegistrationErrorCode.NotTeamMember => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameRegistrationNotTeamMember,
                AppMessages.ErrorCodes.GameRegistrationNotTeamMember
            ),
            GameRegistrationErrorCode.InvitationNotFound
                or GameRegistrationErrorCode.InvitationNotPending => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameRegistrationInvitationInvalid,
                AppMessages.ErrorCodes.GameRegistrationInvitationInvalid
            ),
            GameRegistrationErrorCode.UserNotFound => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.UserMissingOrInactive,
                AppMessages.ErrorCodes.GameRegistrationUserNotFound
            ),
            GameRegistrationErrorCode.SlotNotFound => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameRegistrationSlotNotFound,
                AppMessages.ErrorCodes.GameRegistrationSlotNotFound
            ),
            GameRegistrationErrorCode.SlotNotAvailable => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationSlotNotAvailable,
                AppMessages.ErrorCodes.GameRegistrationSlotNotAvailable
            ),
            GameRegistrationErrorCode.PendingInvitationExists => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationPendingInvitationExists,
                AppMessages.ErrorCodes.GameRegistrationPendingInvitation
            ),
            GameRegistrationErrorCode.PendingOutgoingInvitation => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationPendingOutgoingInvitation,
                AppMessages.ErrorCodes.GameRegistrationPendingOutgoingInvitation
            ),
            GameRegistrationErrorCode.TeamInviteNotAllowed => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationTeamInviteNotAllowed,
                AppMessages.ErrorCodes.GameRegistrationTeamInviteNotAllowed
            ),
            GameRegistrationErrorCode.TeamActiveInGame => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationTeamActiveInGame,
                AppMessages.ErrorCodes.GameRegistrationTeamActiveInGame
            ),
            GameRegistrationErrorCode.OperationFailed => new(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.GameRegistrationOperationFailed,
                AppMessages.ErrorCodes.GameRegistrationOperationFailed
            )
        };

    public static ApiErrorDescriptor FromLifecycle(GameLifecycleErrorCode error) =>
        error switch
        {
            GameLifecycleErrorCode.None => new(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnableToLoadCurrentGame,
                AppMessages.ErrorCodes.GameLifecycleOperationFailed
            ),
            GameLifecycleErrorCode.DraftNotFound => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.NoDraftGameForSetup,
                AppMessages.ErrorCodes.GameLifecycleDraftNotFound
            ),
            GameLifecycleErrorCode.GameNotReady => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameNotReadyForStart,
                AppMessages.ErrorCodes.GameLifecycleGameNotReady
            ),
            GameLifecycleErrorCode.GameNotActive => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameNotActiveForFinish,
                AppMessages.ErrorCodes.GameLifecycleGameNotActive
            ),
            GameLifecycleErrorCode.ReadyGameAlreadyExists => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.ReadyGameAlreadyExists,
                AppMessages.ErrorCodes.GameLifecycleReadyAlreadyExists
            ),
            GameLifecycleErrorCode.ActiveGameAlreadyExists => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.ActiveGameAlreadyExists,
                AppMessages.ErrorCodes.GameLifecycleActiveAlreadyExists
            ),
            GameLifecycleErrorCode.NoParticipationSlots => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationSlotsRequired,
                AppMessages.ErrorCodes.GameLifecycleRegistrationSlotsRequired
            ),
            GameLifecycleErrorCode.InvalidTeamSizeLimits => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameRegistrationInvalidTeamSizeLimits,
                AppMessages.ErrorCodes.GameLifecycleInvalidTeamSizeLimits
            ),
            GameLifecycleErrorCode.NoConfirmedTeams => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameLifecycleNoConfirmedTeams,
                AppMessages.ErrorCodes.GameLifecycleNoConfirmedTeams
            ),
            GameLifecycleErrorCode.UnconfirmedTeams => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameLifecycleUnconfirmedTeams,
                AppMessages.ErrorCodes.GameLifecycleUnconfirmedTeams
            ),
            GameLifecycleErrorCode.PendingInvitations => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameLifecyclePendingInvitations,
                AppMessages.ErrorCodes.GameLifecyclePendingInvitations
            ),
            GameLifecycleErrorCode.PendingDisbandRequests => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameLifecyclePendingDisbandRequests,
                AppMessages.ErrorCodes.GameLifecyclePendingDisbandRequests
            ),
            GameLifecycleErrorCode.InvalidConfirmedTeamRoster => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameLifecycleInvalidConfirmedTeamRoster,
                AppMessages.ErrorCodes.GameLifecycleInvalidConfirmedTeamRoster
            ),
            GameLifecycleErrorCode.DraftDeleteNotAllowed => new(
                StatusCodes.Status409Conflict,
                AppMessages.Client.DraftGameDeleteNotAllowed,
                AppMessages.ErrorCodes.GameLifecycleDraftDeleteNotAllowed
            ),
            GameLifecycleErrorCode.GameNotFound => new(
                StatusCodes.Status404NotFound,
                AppMessages.Client.GameLifecycleGameNotFound,
                AppMessages.ErrorCodes.GameLifecycleGameNotFound
            )
        };
#pragma warning restore CS8524
}
