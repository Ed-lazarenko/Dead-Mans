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
}
