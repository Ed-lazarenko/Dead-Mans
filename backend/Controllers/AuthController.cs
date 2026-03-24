using backend.Api.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("auth/twitch")]
public sealed class AuthController : ControllerBase
{
    private const string OAuthStateCookieName = "dm_twitch_oauth_state";

    private readonly ITwitchAuthService _twitchAuthService;
    private readonly TwitchAuthOptions _twitchAuthOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITwitchAuthService twitchAuthService,
        IOptions<TwitchAuthOptions> twitchAuthOptions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger
    )
    {
        _twitchAuthService = twitchAuthService;
        _twitchAuthOptions = twitchAuthOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var state = TwitchStateGenerator.Create();
        Response.Cookies.Delete(OAuthStateCookieName, BuildOAuthStateCookieOptions());
        Response.Cookies.Append(
            OAuthStateCookieName,
            state,
            BuildOAuthStateCookieOptions()
        );

        var twitchAuthorizeUrl = _twitchAuthService.BuildAuthorizeUrl(state);
        return Redirect(twitchAuthorizeUrl);
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect(BuildFrontendRedirect("error", NormalizeFrontendReason(error)));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Redirect(BuildFrontendRedirect("error", "missing_code"));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            return Redirect(BuildFrontendRedirect("error", "missing_state"));
        }

        if (!Request.Cookies.TryGetValue(OAuthStateCookieName, out var stateCookie))
        {
            return Redirect(BuildFrontendRedirect("error", "state_cookie_missing"));
        }

        Response.Cookies.Delete(OAuthStateCookieName, BuildOAuthStateCookieOptions());

        if (!string.Equals(state, stateCookie, StringComparison.Ordinal))
        {
            return Redirect(BuildFrontendRedirect("error", "state_mismatch"));
        }

        try
        {
            var authUser = await _twitchAuthService.AuthenticateAsync(code, cancellationToken);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, authUser.UserId.ToString()),
                new("twitch_user_id", authUser.TwitchUserId),
                new(ClaimTypes.Name, authUser.DisplayName)
            };
            claims.AddRange(authUser.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twitch authentication callback failed.");
            return Redirect(BuildFrontendRedirect("error", "authentication_failed"));
        }

        return Redirect(BuildFrontendRedirect("authenticated"));
    }

    private CookieOptions BuildOAuthStateCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
            Path = "/"
        };
    }

    private string BuildFrontendRedirect(string status, string? reason = null)
    {
        var query = new Dictionary<string, string?> { ["status"] = status };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            query["reason"] = reason;
        }

        return QueryHelpers.AddQueryString(_twitchAuthOptions.FrontendRedirectUri, query);
    }

    private static string NormalizeFrontendReason(string reason)
    {
        return reason switch
        {
            "access_denied" => reason,
            _ => "unknown"
        };
    }
}
