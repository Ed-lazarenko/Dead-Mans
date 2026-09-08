using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/modifiers")]
[Authorize]
public sealed partial class GameModifierController : ControllerBase
{
    private readonly IGameModifierService _gameModifierService;

    public GameModifierController(IGameModifierService gameModifierService)
    {
        _gameModifierService = gameModifierService;
    }
}
