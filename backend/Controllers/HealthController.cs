using backend.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public HealthController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public ActionResult<HealthStatusResponse> Get()
    {
        return Ok(new HealthStatusResponse("ok", _environment.EnvironmentName, DateTimeOffset.UtcNow));
    }
}
