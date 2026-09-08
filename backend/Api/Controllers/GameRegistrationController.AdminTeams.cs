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

public sealed partial class GameRegistrationController
{
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
}
