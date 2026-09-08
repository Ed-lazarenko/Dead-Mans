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
public sealed partial class GameRegistrationController : ControllerBase
{
    private readonly IGameRegistrationService _registrationService;

    public GameRegistrationController(IGameRegistrationService registrationService)
    {
        _registrationService = registrationService;
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
