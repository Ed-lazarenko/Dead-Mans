using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public sealed partial class GameRoundController
{
    [HttpGet("teams")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameRoundTeamOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEligibleTeams(CancellationToken cancellationToken)
    {
        var teams = await _service.GetEligibleTeamsAsync(cancellationToken);
        return Ok(teams.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var activeRound = await _service.GetActiveAsync(cancellationToken);
        if (activeRound is null)
        {
            return NoContent();
        }

        return Ok(activeRound.ToDto());
    }
}
