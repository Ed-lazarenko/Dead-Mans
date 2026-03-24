using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Api.Contracts;
using backend.Api.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/modifiers")]
public sealed class ModifiersController : ControllerBase
{
    private readonly IModifiersService _modifiersService;

    public ModifiersController(IModifiersService modifiersService)
    {
        _modifiersService = modifiersService;
    }

    [HttpGet]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<ModifiersSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _modifiersService.GetSnapshotAsync(cancellationToken);
        return Ok(snapshot.ToDto());
    }

    [HttpPost("activate")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<ModifiersSnapshotDto>> Activate(
        [FromBody] ActivateModifierRequest request,
        CancellationToken cancellationToken
    )
    {
        var snapshot = await _modifiersService.ActivateAsync(request.ToCommand(), cancellationToken);
        return Ok(snapshot.ToDto());
    }
}
