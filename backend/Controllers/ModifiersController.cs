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
    private readonly ILogger<ModifiersController> _logger;

    public ModifiersController(IModifiersService modifiersService, ILogger<ModifiersController> logger)
    {
        _modifiersService = modifiersService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<ModifiersSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _modifiersService.GetSnapshotAsync(cancellationToken);
            return Ok(snapshot.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Modifiers snapshot failed (configuration or domain rule).");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading modifiers.");
            throw;
        }
    }

    [HttpPost("activate")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<ModifiersSnapshotDto>> Activate(
        [FromBody] ActivateModifierRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Modifier activate requested. ModifierId: {ModifierId}.",
                request.ModifierId
            );
            var snapshot = await _modifiersService.ActivateAsync(request.ToCommand(), cancellationToken);
            return Ok(snapshot.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Modifier activate failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error activating modifier.");
            throw;
        }
    }
}
