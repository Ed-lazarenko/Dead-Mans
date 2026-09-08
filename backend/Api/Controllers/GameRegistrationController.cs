using ApiContracts = backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using AppContracts = backend.Application.Contracts;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/registration")]
[Authorize]
public sealed class GameRegistrationController : ControllerBase
{
    private readonly IGameRegistrationService _registrationService;

    public GameRegistrationController(IGameRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiContracts.GameRegistrationSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var snapshot = await _registrationService.GetRegistrationSnapshotAsync(
            userId.Value,
            cancellationToken
        );
        if (snapshot is null)
        {
            return NotFound(GameRegistrationErrorMapping.NotOpenResponse());
        }

        return Ok(snapshot.ToDto());
    }

    [HttpPost("teams")]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeam(
        [FromBody] ApiContracts.CreateRegistrationTeamRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.CreateTeamAsync(
            userId.Value,
            request.RecruitmentOpen,
            request.Name,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status201Created);
    }

    [HttpPatch("my-team/name")]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyTeamName(
        [FromBody] ApiContracts.UpdateRegistrationTeamNameRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.UpdateMyTeamNameAsync(
            userId.Value,
            request.Name,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("teams/{teamId:guid}/join")]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.JoinTeamAsync(userId.Value, teamId, cancellationToken);
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("teams/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveTeam(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.LeaveTeamAsync(userId.Value, cancellationToken);
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("my-team/disband-request")]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestMyTeamDisband(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.RequestMyTeamDisbandAsync(
            userId.Value,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("teams")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<ApiContracts.RegistrationTeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListTeams(CancellationToken cancellationToken)
    {
        var teams = await _registrationService.ListTeamsAsync(cancellationToken);
        if (teams is null)
        {
            return NotFound(GameRegistrationErrorMapping.NotOpenResponse());
        }

        return Ok(teams.Select(team => team.ToDto()).ToArray());
    }

    [HttpGet("admin")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.GameRegistrationAdminSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdminSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await _registrationService.GetAdminSnapshotAsync(cancellationToken);
        if (snapshot is null)
        {
            return NotFound(GameRegistrationErrorMapping.NotOpenResponse());
        }

        return Ok(snapshot.ToDto());
    }

    [HttpPost("admin/teams")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAdminTeam(
        [FromBody] ApiContracts.CreateAdminRegistrationTeamRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.CreateEmptyTeamAsync(
            adminId.Value,
            request.TeamSlotId,
            request.RecruitmentOpen,
            request.Name,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status201Created);
    }

    [HttpPatch("admin/teams/{teamId:guid}/name")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAdminTeamName(
        Guid teamId,
        [FromBody] ApiContracts.UpdateRegistrationTeamNameRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.UpdateTeamNameAsync(
            teamId,
            request.Name,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("admin/teams/{teamId:guid}/assign")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignPlayer(
        Guid teamId,
        [FromBody] ApiContracts.AssignRegistrationPlayerRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.AssignPlayerAsync(
            adminId.Value,
            teamId,
            request.UserId,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("admin/teams/{teamId:guid}/members/{userId:guid}/remove")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePlayerFromTeam(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.RemovePlayerFromTeamAsync(
            adminId.Value,
            teamId,
            userId,
            cancellationToken
        );
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("admin/teams/{teamId:guid}/invitations/{invitationId:guid}/cancel")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelTeamInvitation(
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.CancelTeamInvitationAsync(
            adminId.Value,
            teamId,
            invitationId,
            cancellationToken
        );
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("admin/teams/{teamId:guid}/move")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveTeam(
        Guid teamId,
        [FromBody] ApiContracts.MoveRegistrationTeamRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.MoveTeamToSlotAsync(
            adminId.Value,
            teamId,
            request.TargetTeamSlotId,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("teams/{teamId:guid}/confirm")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.ConfirmTeamAsync(
            adminId.Value,
            teamId,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("teams/{teamId:guid}/reject")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.RejectTeamAsync(
            adminId.Value,
            teamId,
            cancellationToken
        );
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("teams/{teamId:guid}/disband")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisbandConfirmedTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.DisbandConfirmedTeamAsync(
            adminId.Value,
            teamId,
            cancellationToken
        );
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("invitations")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ApiContracts.RegistrationInvitationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvitation(
        [FromBody] ApiContracts.CreateAdminInvitationRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var adminId = RequireUserId();
        if (adminId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.CreateAdminInvitationAsync(
            adminId.Value,
            request.TeamSlotId,
            request.InvitedUserId,
            request.TeamId,
            cancellationToken
        );
        if (result.Success && result.Value is not null)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value.ToDto());
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("my-team/invitations")]
    [ProducesResponseType(typeof(ApiContracts.RegistrationInvitationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePlayerInvitation(
        [FromBody] ApiContracts.CreatePlayerInvitationRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.CreatePlayerInvitationAsync(
            userId.Value,
            request.InvitedUserId,
            cancellationToken
        );
        if (result.Success && result.Value is not null)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value.ToDto());
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("my-team/invitations/{invitationId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelPlayerInvitation(
        Guid invitationId,
        CancellationToken cancellationToken
    )
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.CancelPlayerInvitationAsync(
            userId.Value,
            invitationId,
            cancellationToken
        );
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    [HttpPost("invitations/{invitationId:guid}/accept")]
    [ProducesResponseType(typeof(ApiContracts.RegistrationTeamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptInvitation(
        Guid invitationId,
        CancellationToken cancellationToken
    )
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.AcceptInvitationAsync(
            userId.Value,
            invitationId,
            cancellationToken
        );
        return ToTeamResult(result, StatusCodes.Status200OK);
    }

    [HttpPost("invitations/{invitationId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeclineInvitation(
        Guid invitationId,
        CancellationToken cancellationToken
    )
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var result = await _registrationService.DeclineInvitationAsync(
            userId.Value,
            invitationId,
            cancellationToken
        );
        if (result.Success)
        {
            return NoContent();
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }

    private Guid? RequireUserId() => HttpContext.TryGetUserId();

    private IActionResult ToTeamResult(
        AppContracts.GameRegistrationResult<AppContracts.RegistrationTeamDto> result,
        int successStatusCode
    )
    {
        if (result.Success && result.Value is not null)
        {
            return StatusCode(successStatusCode, result.Value.ToDto());
        }

        return GameRegistrationErrorMapping.ToActionResult(this, result.Error);
    }
}
