using backend.Application.Abstractions;
using backend.Application.Contracts;
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
    public async Task<ActionResult<ModifiersSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _modifiersService.GetSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }

    [HttpPost("activate")]
    public async Task<ActionResult<ModifiersSnapshotDto>> Activate(
        [FromBody] ActivateModifierRequest request,
        CancellationToken cancellationToken
    )
    {
        var snapshot = await _modifiersService.ActivateAsync(request, cancellationToken);
        return Ok(snapshot);
    }
}
