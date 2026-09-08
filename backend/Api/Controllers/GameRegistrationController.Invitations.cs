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
}
