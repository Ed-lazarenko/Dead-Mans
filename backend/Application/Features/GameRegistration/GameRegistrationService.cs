using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameRegistration;

public sealed partial class GameRegistrationService : IGameRegistrationService
{
    private readonly IGameRegistrationReadStore _reads;
    private readonly IGameRegistrationPersistence _persistence;

    public GameRegistrationService(
        IGameRegistrationReadStore reads,
        IGameRegistrationPersistence persistence
    )
    {
        _reads = reads;
        _persistence = persistence;
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> CreateTeamAsync(
        Guid userId,
        bool recruitmentOpen,
        string? name = null,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = TeamNameValue.Normalize(name);
        if (!TeamNameValue.IsValid(normalizedName))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvalidTeamName);
        }

        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        if (await _reads.UserHasTeamMembershipAsync(game.GameId, userId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.UserAlreadyOnTeam);
        }

        var slot = await _reads.FindAvailablePublicSlotAsync(game.GameId, cancellationToken);
        if (slot is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NoAvailableSlot);
        }

        return await _persistence.PersistCreateTeamAsync(
            game.GameId,
            userId,
            slot.TeamSlotId,
            recruitmentOpen,
            normalizedName,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> UpdateMyTeamNameAsync(
        Guid userId,
        string? name,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = TeamNameValue.Normalize(name);
        if (!TeamNameValue.IsValid(normalizedName))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvalidTeamName);
        }

        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var teamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (!teamId.HasValue)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NotTeamMember);
        }

        var team = await _reads.GetTeamAdminActionSnapshotAsync(game.GameId, teamId.Value, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistUpdateTeamNameAsync(
            game.GameId,
            teamId.Value,
            normalizedName,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> JoinTeamAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        if (await _reads.UserHasTeamMembershipAsync(game.GameId, userId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.UserAlreadyOnTeam);
        }

        var team = await _reads.GetJoinableTeamAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming || !team.RecruitmentOpen)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistJoinTeamAsync(
            game.GameId,
            userId,
            teamId,
            game.MaxPlayersPerTeam,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> LeaveTeamAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var activeTeamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (activeTeamId.HasValue)
        {
            var team = await _reads.GetTeamAdminActionSnapshotAsync(
                game.GameId,
                activeTeamId.Value,
                cancellationToken
            );
            if (team?.Status == TeamStatusValue.Confirmed)
            {
                return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
            }
        }

        if (activeTeamId.HasValue
            && await _reads.TeamHasPendingInvitationAsync(game.GameId, activeTeamId.Value, cancellationToken))
        {
            return Fail<bool>(GameRegistrationErrorCode.PendingOutgoingInvitation);
        }

        return await _persistence.PersistLeaveTeamAsync(game.GameId, userId, cancellationToken);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> RequestMyTeamDisbandAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var activeTeamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (!activeTeamId.HasValue)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NotTeamMember);
        }

        var team = await _reads.GetTeamAdminActionSnapshotAsync(
            game.GameId,
            activeTeamId.Value,
            cancellationToken
        );
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistRequestTeamDisbandAsync(
            game.GameId,
            userId,
            activeTeamId.Value,
            cancellationToken
        );
    }

    private static GameRegistrationResult<T> Fail<T>(GameRegistrationErrorCode error) =>
        new(false, default, error);
}
